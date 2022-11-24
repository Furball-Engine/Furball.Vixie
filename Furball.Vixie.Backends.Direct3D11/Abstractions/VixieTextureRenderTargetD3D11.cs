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

    public unsafe VixieTextureRenderTargetD3D11(Direct3D11Backend backend, uint width, uint height) {
        this._backend = backend;
        this._backend.CheckThread();

        Texture2DDesc renderTargetTextureDesc = new() {
            Width     = width,
            Height    = height,
            MipLevels = 1,
            ArraySize = 1,
            Format    = Format.FormatR8G8B8A8Unorm,
            BindFlags = (uint)(BindFlag.ShaderResource | BindFlag.RenderTarget),
            Usage     = Usage.Default,
            SampleDesc = new SampleDesc {
                Count   = 1,
                Quality = 0
            },
        };

        this._texDesc = renderTargetTextureDesc;

        ComPtr<ID3D11Texture2D> renderTargetTexture = null;
        backend.Device.CreateTexture2D(renderTargetTextureDesc, null, ref renderTargetTexture);

        RenderTargetViewDesc renderTargetDesc = new RenderTargetViewDesc {
            Format        = renderTargetTextureDesc.Format,
            ViewDimension = RtvDimension.Texture2D,
        };

        renderTargetDesc.Anonymous.Texture2D = new Tex2DRtv {
            MipSlice = 0
        };

        ComPtr<ID3D11RenderTargetView> renderTarget = null;
        backend.Device.CreateRenderTargetView(renderTargetTexture, renderTargetDesc, ref renderTarget);

        ShaderResourceViewDesc shaderResourceViewDesc = new() {
            Format        = renderTargetDesc.Format,
            ViewDimension = D3DSrvDimension.D3DSrvDimensionTexture2D,
        };

        shaderResourceViewDesc.Anonymous.Texture2D = new Tex2DSrv {
            MostDetailedMip = 0,
            MipLevels       = 1
        };

        ComPtr<ID3D11ShaderResourceView> shaderResourceView = null;
        backend.Device.CreateShaderResourceView(renderTargetTexture, shaderResourceViewDesc, ref shaderResourceView);

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

    public override unsafe void Bind() {
        this._backend.CheckThread();
        this._backend.DeviceContext.OMSetRenderTargets(0, this._renderTarget, (ID3D11DepthStencilView*)null);
        this._backend.CurrentlyBoundTarget = this._renderTarget;
        this._backend.ResetBlendState();

        uint viewportCount = 0;
        this._backend.DeviceContext.RSGetViewports(ref viewportCount, null);
        
        if (viewportCount == 0)
            throw new Exception("No viewports?");
        
        Viewport* viewports = stackalloc Viewport[(int)viewportCount];
        this._backend.DeviceContext.RSGetViewports(ref viewportCount, viewports);

        this._startingViewport = viewports[0];

        Viewport newViewport = new(
            0,
            0,
            this.Size.X,
            this.Size.Y,
            this._startingViewport.MinDepth,
            this._startingViewport.MaxDepth
        );
        this._backend.DeviceContext.RSSetViewports(1, newViewport);
        this._backend.SetProjectionMatrix(this.Size.X, this.Size.Y, true);
    }

    public override void Unbind() {
        this._backend.CheckThread();
        this._backend.SetDefaultRenderTarget();

        this._backend.DeviceContext.RSSetViewports(1, this._startingViewport);
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