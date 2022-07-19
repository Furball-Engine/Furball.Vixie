using System;
using System.Drawing;
using System.Numerics;

namespace Furball.Vixie.Backends.Shared; 

public abstract class Texture : IDisposable {
    public abstract Vector2 Size   { get; protected set; }
    public          int     Width  => (int)this.Size.X;
    public          int     Height => (int)this.Size.Y;

    public abstract Texture SetData<pDataType>(int level, pDataType[] data) where pDataType : unmanaged;
    public abstract Texture SetData<pDataType>(int level, Rectangle rect, pDataType[] data) where pDataType : unmanaged;
    public virtual  void    Dispose() {}
}