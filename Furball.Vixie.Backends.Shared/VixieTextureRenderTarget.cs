using System;
using Silk.NET.Maths;

namespace Furball.Vixie.Backends.Shared; 

public abstract class VixieTextureRenderTarget : IDisposable {
    public abstract Vector2D<int> Size { get; protected set; }

    public abstract void    Bind();
    public abstract void    Unbind();
    public abstract VixieTexture GetTexture();

    public virtual void Dispose() {}
}