using System;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Maths;

namespace Furball.Vixie.Backends.Direct3D11.Abstractions;

internal sealed class VixieTextureRenderTargetD3D11 : VixieTextureRenderTarget {
    public override Vector2D<int> Size { get; protected set; }

    private readonly Direct3D11Backend                _backend;
    private readonly ComPtr<ID3D11Texture2D>          _renderTargetTexture;
    private readonly ComPtr<ID3D11RenderTargetView>   _renderTarget;
    private readonly ComPtr<ID3D11ShaderResourceView> _shaderResourceView;

    private readonly Texture2DDesc _texDesc;

    private readonly VixieTexture _vixieTexture;

    public VixieTextureRenderTargetD3D11(Direct3D11Backend backend, uint width, uint height) {
        this._backend = backend;
        this._backend.CheckThread();

        Texture2DDesc renderTargetTextureDesc = new() {
            Width     = width,
            Height    = height,
            MipLevels = 1,
            ArraySize = 1,
            Format    = Format.R8G8B8A8_UNorm,
            BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
            Usage     = ResourceUsage.Default,
            SampleDesc = new SampleDesc {
                Count   = 1,
                Quality = 0
            },
        };

        this._texDesc = renderTargetTextureDesc;

        ID3D11Texture2D renderTargetTexture = backend.Device.CreateTexture2D(renderTargetTextureDesc);

        RenderTargetViewDesc renderTargetDesc = new() {
            Format        = renderTargetTextureDesc.Format,
            ViewDimension = RenderTargetViewDimension.Texture2D,
        };

        renderTargetDesc.Texture2D.MipSlice = 0;

        ID3D11RenderTargetView renderTarget =
            backend.Device.CreateRenderTargetView(renderTargetTexture, renderTargetDesc);

        ShaderResourceViewDesc shaderResourceViewDesc = new() {
            Format        = renderTargetDesc.Format,
            ViewDimension = ShaderResourceViewDimension.Texture2D,
        };

        shaderResourceViewDesc.Texture2D.MostDetailedMip = 0;
        shaderResourceViewDesc.Texture2D.MipLevels       = 1;

        ID3D11ShaderResourceView shaderResourceView =
            backend.Device.CreateShaderResourceView(renderTargetTexture, shaderResourceViewDesc);

        this._renderTargetTexture = renderTargetTexture;
        this._renderTarget        = renderTarget;
        this._shaderResourceView  = shaderResourceView;
        this.Size                 = new Vector2D<int>((int)width, (int)height);

        this._vixieTexture = new VixieTextureD3D11(
            this._backend,
            this._renderTargetTexture,
            this._shaderResourceView,
            this.Size,
            this._texDesc
        );
    }

    ~VixieTextureRenderTargetD3D11() {
        DisposeQueue.Enqueue(this);
    }

    public override void Bind() {
        this._backend.CheckThread();
        this._backend.DeviceContext.OMSetRenderTargets(this._renderTarget);
        this._backend.CurrentlyBoundTarget = this._renderTarget;
        this._backend.ResetBlendState();

        this._startingViewport = this._backend.DeviceContext.RSGetViewport();

        Viewport newViewport = new(
            0,
            0,
            this.Size.X,
            this.Size.Y,
            this._startingViewport.MinDepth,
            this._startingViewport.MaxDepth
        );
        this._backend.DeviceContext.RSSetViewport(newViewport);
        this._backend.SetProjectionMatrix(this.Size.X, this.Size.Y, true);
    }

    public override void Unbind() {
        this._backend.CheckThread();
        this._backend.SetDefaultRenderTarget();

        this._backend.DeviceContext.RSSetViewport(this._startingViewport);
        this._backend.SetProjectionMatrix((int)this._startingViewport.Width, (int)this._startingViewport.Height, false);
    }

    public override VixieTexture GetTexture() => this._vixieTexture;

    private bool     _isDisposed = false;
    private Viewport _startingViewport;
    public override void Dispose() {
        this._backend.CheckThread();
        if (this._isDisposed)
            return;

        this._isDisposed = true;

        try {
            this._renderTarget.Dispose();
        }
        catch (NullReferenceException) { /* Apperantly thing?.Dispose can still throw a NullRefException? */
        }
    }
}