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
    
    public abstract Texture SetData<pDataType>(int level, pDataType[] data) where pDataType : unmanaged;
    public abstract Texture SetData<pDataType>(int level, Rectangle rect, pDataType[] data) where pDataType : unmanaged;
    public virtual  void    Dispose() {}
}