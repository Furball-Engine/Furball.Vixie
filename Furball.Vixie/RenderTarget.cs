#nullable enable
using System;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers;
using Silk.NET.Maths;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle = System.Drawing.Rectangle;

namespace Furball.Vixie; 

public class RenderTarget : IDisposable {
    private VixieTextureRenderTarget _target;
    private VixieTexture             _texture;
    
    public Vector2D<int> Size;
    
    private Rgba32[]? _dataCache;

    public RenderTarget(uint width, uint height, TextureParameters parameters = default) {
        this._target  = GraphicsBackend.Current.CreateRenderTarget(width, height);
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
        this._target.Dispose();
    }
    
    internal void SaveDataToCpu() {
        this._dataCache = this._texture.GetData();
    }

    internal void LoadDataFromCpuToNewTexture() {
        if (this._dataCache == null)
            throw new InvalidOperationException("Texture data was not saved before the backend switch!");
        
        VixieTextureRenderTarget newTex = GraphicsBackend.Current.CreateRenderTarget((uint)this.Size.X, (uint)this.Size.Y);
        
        newTex.GetTexture().SetData<Rgba32>(this._dataCache);
        
        this._target  = newTex;
        this._texture = this._target.GetTexture();

        this._dataCache = null;
    }
    
    public static implicit operator Texture(RenderTarget target) => new(target._texture);
    public static implicit operator VixieTexture(RenderTarget target) => target._texture;
    public void DisposeInternal() {
        this._target.Dispose();
    }
}