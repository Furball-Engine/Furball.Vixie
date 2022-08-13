#nullable enable
using System;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers;
using Silk.NET.Maths;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle = System.Drawing.Rectangle;

namespace Furball.Vixie; 

public class Texture : IDisposable {
    public string Name = "";

    private VixieTexture _texture;
    
    public Vector2D<int> Size;

    public TextureFilterType FilterType {
        get => this._texture.FilterType;
        set => this._texture.FilterType = value;
    }
    
    private Rgba32[]?         _dataCache = null;
    private bool              _mipmapCache;
    private TextureFilterType _filterTypeCache;
    
    public Texture(VixieTexture texture) {
        this._texture = texture;

        this.Size = this._texture.Size;
    }

    public void SetData<T>(T[] arr, Rectangle? rect = null) where T : unmanaged {
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
    
    internal void SaveDataToCpu() {
        this._dataCache       = this._texture.GetData();
        this._mipmapCache     = this._texture.Mipmaps;
        this._filterTypeCache = this._texture.FilterType;
    }

    internal void SaveDataToNewTexture() {
        if (this._dataCache == null)
            throw new InvalidOperationException("Texture data was not saved before the backend switch!");
        
        Texture newTex = Resources.CreateEmptyTexture((uint)this.Size.X, (uint)this.Size.Y,
                                                      new TextureParameters(this._mipmapCache, this._filterTypeCache));
        
        newTex.SetData(this._dataCache);
    }

    public static implicit operator VixieTexture(Texture tex) => tex._texture;
}