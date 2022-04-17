using System;
using System.Numerics;

namespace Furball.Vixie.Backends.Shared {
    public abstract class TextureRenderTarget : IDisposable {
        public abstract Vector2 Size { get; protected set; }

        public abstract void Bind();
        public abstract void Unbind();
        public abstract Texture GetTexture();

        public virtual void Dispose() {}
    }
}
