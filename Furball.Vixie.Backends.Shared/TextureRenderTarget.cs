using System.Numerics;

namespace Furball.Vixie.Backends.Shared {
    public abstract class TextureRenderTarget {
        public abstract Vector2 Size { get; protected set; }

        public abstract void Bind();
        public abstract void Unbind();
        public abstract Texture GetTexture();
    }
}
