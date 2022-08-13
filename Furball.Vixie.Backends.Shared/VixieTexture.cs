using System;
using Silk.NET.Maths;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle = System.Drawing.Rectangle;

namespace Furball.Vixie.Backends.Shared; 

public abstract class VixieTexture : IDisposable {
    protected Vector2D<int> _size;
    public    bool          Useless;

    protected int MipMapCount(int width, int height)
        => (int)(Math.Floor(Math.Log(Math.Max(width, height), 2) / 2d) + 1);

    public Vector2D<int> Size => this._size;
    
    public int Width  => this.Size.X;
    public int Height => this.Size.Y;

    public abstract TextureFilterType FilterType { get; set; }

    public abstract bool Mipmaps { get; }
    
    /// <summary>
    /// Sets the data of the whole texture at once
    /// </summary>
    /// <param name="data">The data</param>
    /// <typeparam name="T">The type of data</typeparam>
    /// <returns></returns>
    public abstract VixieTexture SetData<T>(T[] data) where T : unmanaged;
    public abstract VixieTexture SetData<T>(T[] data, Rectangle rect) where T : unmanaged;
    /// <summary>
    /// Copies the data of the texture into CPU memory
    /// </summary>
    /// <returns>The raw pixels of the texture</returns>
    public abstract Rgba32[] GetData();
    
    public virtual  void    Dispose() {}
}