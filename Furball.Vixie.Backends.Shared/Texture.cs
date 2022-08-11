using System;
using System.Drawing;
using System.Numerics;

namespace Furball.Vixie.Backends.Shared; 

public abstract class Texture : IDisposable {
    protected Vector2 _size;

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

    public virtual  void    Dispose() {}
}