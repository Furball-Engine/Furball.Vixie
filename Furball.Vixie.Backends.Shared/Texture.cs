using System;
using System.Drawing;
using System.Numerics;
using SixLabors.ImageSharp.PixelFormats;

namespace Furball.Vixie.Backends.Shared; 

public abstract class Texture : IDisposable {
    protected Vector2 _size;
    public    bool    Useless;

    protected int MipMapCount(int width, int height)
        => (int)(Math.Floor(Math.Log(Math.Max(width, height), 2) / 2d) + 1);
    
    public Vector2 Size => this._size;
    public          int     Width  => (int)this.Size.X;
    public          int     Height => (int)this.Size.Y;

    public abstract TextureFilterType FilterType { get; set; }

    /// <summary>
    /// Sets the data of the whole texture at once
    /// </summary>
    /// <param name="data">The data</param>
    /// <typeparam name="T">The type of data</typeparam>
    /// <returns></returns>
    public abstract Texture SetData<T>(T[] data) where T : unmanaged;
    public abstract Texture SetData<T>(T[] data, Rectangle rect) where T : unmanaged;
    /// <summary>
    /// Copies the data of the texture into CPU memory
    /// </summary>
    /// <returns>The raw pixels of the texture</returns>
    public abstract Rgba32[] GetData();
    
    public virtual  void    Dispose() {}
}