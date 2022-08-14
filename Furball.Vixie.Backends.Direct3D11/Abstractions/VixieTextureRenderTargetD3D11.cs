using System;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers;
using Silk.NET.Maths;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Furball.Vixie.Backends.Direct3D11.Abstractions; 

internal sealed class VixieTextureRenderTargetD3D11 : VixieTextureRenderTarget {
    public override Vector2D<int> Size { get; protected set; }

    private Direct3D11Backend        _backend;
    private ID3D11Device             _device;
    private ID3D11DeviceContext      _deviceContext;
    private ID3D11Texture2D          _renderTargetTexture;
    private ID3D11RenderTargetView   _renderTarget;
    private ID3D11ShaderResourceView _shaderResourceView;

    private readonly Texture2DDescription _texDesc;

    private readonly VixieTexture _vixieTexture;
    
    private Viewport[] _viewports;

    public VixieTextureRenderTargetD3D11(Direct3D11Backend backend, uint width, uint height) {
        this._backend = backend;
        this._backend.CheckThread();
        this._deviceContext = backend.GetDeviceContext();
        this._device        = backend.GetDevice();

        Texture2DDescription renderTargetTextureDescription = new Texture2DDescription {
            Width     = (int)width,
            Height    = (int)height,
            MipLevels = 1,
            ArraySize = 1,
            Format    = Format.R8G8B8A8_UNorm,
            BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
            Usage     = ResourceUsage.Default,
            SampleDescription = new SampleDescription {
                Count = 1, Quality = 0
            },
        };

        this._texDesc = renderTargetTextureDescription;

        ID3D11Texture2D renderTargetTexture = this._device.CreateTexture2D(renderTargetTextureDescription);

        RenderTargetViewDescription renderTargetDescription = new RenderTargetViewDescription {
            Format        = renderTargetTextureDescription.Format,
            ViewDimension = RenderTargetViewDimension.Texture2D,
        };

        renderTargetDescription.Texture2D.MipSlice = 0;

        ID3D11RenderTargetView renderTarget = this._device.CreateRenderTargetView(renderTargetTexture, renderTargetDescription);

        ShaderResourceViewDescription shaderResourceViewDescription = new ShaderResourceViewDescription {
            Format        = renderTargetDescription.Format,
            ViewDimension = ShaderResourceViewDimension.Texture2D,
        };

        shaderResourceViewDescription.Texture2D.MostDetailedMip = 0;
        shaderResourceViewDescription.Texture2D.MipLevels       = 1;

        ID3D11ShaderResourceView shaderResourceView = this._device.CreateShaderResourceView(renderTargetTexture, shaderResourceViewDescription);

        this._renderTargetTexture = renderTargetTexture;
        this._renderTarget        = renderTarget;
        this._shaderResourceView  = shaderResourceView;
        this.Size                 = new Vector2D<int>((int)width, (int)height);
        this._viewports           = new Viewport[16];

        this._vixieTexture = new VixieTextureD3D11(this._backend, this._renderTargetTexture, this._shaderResourceView,
            this.Size, this._texDesc);
    }

    ~VixieTextureRenderTargetD3D11() {
        DisposeQueue.Enqueue(this);
    }

    public override void Bind() {
        this._backend.CheckThread();
        this._deviceContext.OMSetRenderTargets(this._renderTarget);
        this._backend.CurrentlyBoundTarget = this._renderTarget;
        this._backend.ResetBlendState();

        this._deviceContext.RSGetViewports(this._viewports);
        this._deviceContext.RSSetViewport(new Viewport(0, 0, this.Size.X, this.Size.Y, 0, 1));
    }

    public override void Unbind() {
        this._backend.CheckThread();
        this._backend.SetDefaultRenderTarget();
        this._deviceContext.RSSetViewports(this._viewports);
    }

    public override VixieTexture GetTexture() => this._vixieTexture;

    private bool _isDisposed = false;
    public override void Dispose() {
        this._backend.CheckThread();
        if (this._isDisposed)
            return;

        this._isDisposed = true;

        try {
            this._renderTarget?.Dispose();
        } catch(NullReferenceException) { /* Apperantly thing?.Dispose can still throw a NullRefException? */ }
    }
}