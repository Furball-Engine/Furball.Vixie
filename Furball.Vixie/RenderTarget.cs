#nullable enable
using System;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Helpers;
using Silk.NET.Maths;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle=System.Drawing.Rectangle;

namespace Furball.Vixie; 

public class RenderTarget : IDisposable {
    private VixieTextureRenderTarget _target;
    private VixieTexture             _texture;
    
    public Vector2D<int> Size;
    
    private Rgba32[]? _dataCache;

    public RenderTarget(GraphicsBackend backend, uint width, uint height) {
        this._target  = backend.CreateRenderTarget(width, height);
        this._texture = this._target.GetTexture();
        
        Global.TrackedRenderTargets.Add(new WeakReference<RenderTarget>(this));
        
        this.Size = new Vector2D<int>((int)width, (int)height);
    }

    public void Bind() {
        this._target.Bind();
    }

    public void Unbind() {
        this._target.Unbind();
    }

    public Rgba32[] GetData() => this._texture.GetData();

    public void SetData<pT>(pT[] arr, Rectangle? rect = null) where pT : unmanaged {
        rect ??= new Rectangle(0, 0, this.Size.X, this.Size.Y);
        
        this._texture.SetData<pT>(arr, rect.Value);
    }
    
    ~RenderTarget() {
        DisposeQueue.Enqueue(this);
    }
    
    private bool _isDisposed = false;
    public void Dispose() {
        if (this._isDisposed)
            return;

        this._isDisposed = true;
    }
    
    public static implicit operator Texture(RenderTarget      target) => new Texture(target._texture);
    public static implicit operator VixieTexture(RenderTarget target) => target._texture;
}