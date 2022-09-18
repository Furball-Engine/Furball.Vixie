using Furball.Vixie.Backends.Shared;
using Silk.NET.Maths;
using Vortice.Direct3D9;

namespace Furball.Vixie.Backends.Direct3D9.Abstractions;

public class RenderTargetD3D9 : VixieTextureRenderTarget {
    private readonly Direct3D9Backend  _backend;
    private readonly IDirect3DDevice9  _device;
    private readonly IDirect3DSurface9 _renderTarget;
    private readonly IDirect3DTexture9 _targetTexture;
    private readonly IDirect3DSurface9 _targetSurface;

    private uint _targetWidth, _targetHeight;

    public RenderTargetD3D9(Direct3D9Backend backend, IDirect3DDevice9 device, uint width, uint height) {
        this._device        = device;
        this._backend       = backend;
        this._renderTarget  = this._device.CreateRenderTarget((int)width, (int)height, Format.A8B8G8R8, MultisampleType.None, 0, false);
        this._targetTexture = this._device.CreateTexture((int)width, (int)height, 0, Usage.None, Format.A8B8G8R8, Pool.Managed);
        this._targetSurface = this._targetTexture.GetSurfaceLevel(0);

        this._targetWidth  = width;
        this._targetHeight = height;
    }
    
    public override Vector2D<int> Size {
        get {
            return new Vector2D<int>((int)this._targetWidth, (int)this._targetHeight);
        }
        protected set {

        }
    }

    public override void Bind() {
        this._device.SetRenderTarget(0, this._renderTarget);
    }

    public override void Unbind() {
        this._backend.ResetRendertarget();
    }

    public override VixieTexture GetTexture() {
        this._device.GetRenderTargetData(this._renderTarget, this._targetSurface);

        return new TextureD3D9(this._device, (int)this._targetWidth, (int)this._targetHeight, this._targetTexture);
    }
}