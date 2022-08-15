#nullable enable
using System;
using System.IO;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers;
using Silk.NET.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle = System.Drawing.Rectangle;

namespace Furball.Vixie; 

public class Texture : IDisposable {
    public string Name = "";

    private VixieTexture _texture;

    public Vector2D<int> Size {
        get;
    }

    public int Width {
        get => this.Size.X;
    }
    public int Height {
        get => this.Size.Y;
    }

    public TextureFilterType FilterType {
        get => this._texture.FilterType;
        set => this._texture.FilterType = value;
    }
    
    private Rgba32[]?         _dataCache = null;
    private bool              _mipmapCache;
    private TextureFilterType _filterTypeCache;

    public static Texture CreateTextureFromByteArray(byte[] imageData, TextureParameters parameters = default) {
        VixieTexture tex = GraphicsBackend.Current.CreateTextureFromByteArray(imageData, parameters);

        Texture managedTex = new(tex);
        
        Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(managedTex));
        
        return managedTex;
    }

    public static Texture CreateWhitePixelTexture() {
        VixieTexture tex = GraphicsBackend.Current.CreateWhitePixelTexture();

        Texture managedTex = new(tex);

        Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(managedTex));

        return managedTex;
    }

    public static Texture CreateTextureFromStream(Stream stream, TextureParameters parameters = default) {
        VixieTexture tex = GraphicsBackend.Current.CreateTextureFromStream(stream, parameters);

        Texture managedTex = new(tex);

        Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(managedTex));

        return managedTex;
    }

    public static Texture CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default) {
        VixieTexture tex = GraphicsBackend.Current.CreateEmptyTexture(width, height, parameters);

        Texture managedTex = new(tex);

        Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(managedTex));

        return managedTex;
    }

    internal Texture(VixieTexture texture) {
        this._texture = texture;

        this.Size = this._texture.Size;
    }

    public void SetData(Image<Rgba32> image) {
        if (image.Width != this.Width || this.Height != image.Height)
            throw new InvalidImageContentException($"That image does not have the right size! Expected: {Width}x{Height} Got: {image.Width}x{image.Height}");
        
        image.ProcessPixelRows(
        x => {
            for (int i = 0; i < x.Height; i++) {
                Span<Rgba32> span = x.GetRowSpan(i);
                
                this.SetData<Rgba32>(span, new Rectangle(0, i, x.Width, 1));
            }
        });
    }
    
    public void SetData(Image image) {
        this.SetData(image.CloneAs<Rgba32>());
    }

    public void SetData <T>(T[] data) where T : unmanaged {
        this._texture.SetData<T>(data);
    }
    
    public void SetData<T>(ReadOnlySpan<T> data) where T : unmanaged {
        this._texture.SetData(data);
    }
    
    public void SetData<T>(ReadOnlySpan<T> arr, Rectangle? rect = null) where T : unmanaged {
        rect ??= new Rectangle(0, 0, this.Size.X, this.Size.Y);
        
        this._texture.SetData(arr, rect.Value);
    }
    
    public Rgba32[] GetData() {
        return this._texture.GetData();
    }

    private bool _isDisposed;
    public void Dispose() {
        if (this._isDisposed)
            return;
        
        this._isDisposed = true;
        this._texture.Dispose();
    }

    ~Texture() {
        DisposeQueue.Enqueue(this);
    }

    internal void DisposeInternal() {
        this._texture.Dispose();
    }
    
    internal void SaveDataToCpu() {
        this._dataCache       = this._texture.GetData();
        this._mipmapCache     = this._texture.Mipmaps;
        this._filterTypeCache = this._texture.FilterType;
    }

    internal void LoadDataFromCpuToNewTexture() {
        if (this._dataCache == null)
            throw new InvalidOperationException("Texture data was not saved before the backend switch!");
        
        VixieTexture newTex = GraphicsBackend.Current.CreateEmptyTexture((uint)this.Size.X, (uint)this.Size.Y,
                                                      new TextureParameters(this._mipmapCache, this._filterTypeCache));
        
        newTex.SetData<Rgba32>(this._dataCache);
        
        this._texture = newTex;

        this._dataCache = null;
    }

    public static implicit operator VixieTexture(Texture tex) => tex._texture;
}