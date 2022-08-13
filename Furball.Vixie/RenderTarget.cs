using System;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers;
using Silk.NET.Maths;

namespace Furball.Vixie; 

public class RenderTarget : IDisposable {
    private readonly VixieTextureRenderTarget _target;

    public Vector2D<int> Size;
    
    public RenderTarget(uint width, uint height, TextureParameters parameters = default) {
        this._target = GraphicsBackend.Current.CreateRenderTarget(width, height);

        Global.TRACKED_RENDER_TARGETS.Add(new WeakReference<RenderTarget>(this));
    }

    public void Bind() {
        this._target.Bind();
    }

    public void Unbind() {
        this._target.Unbind();
    }

    ~RenderTarget() {
        DisposeQueue.Enqueue(this);
    }
    
    private bool _isDisposed = false;
    public void Dispose() {
        if (this._isDisposed)
            return;

        this._isDisposed = true;
        this._target.Dispose();
    }
    
    public static implicit operator Texture(RenderTarget target) => new(target._target.GetTexture());
    public static implicit operator VixieTexture(RenderTarget target) => target._target.GetTexture();
}