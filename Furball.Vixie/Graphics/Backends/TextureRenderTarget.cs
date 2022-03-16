using System.Numerics;

namespace Furball.Vixie.Graphics.Backends {
    public abstract class TextureRenderTarget {
        public abstract Vector2 Size { get; protected set; }

        public static TextureRenderTarget Create(uint width, uint height) {
            return GraphicsBackend.Current.CreateRenderTarget(width, height);
        }

        public abstract void Bind();
        public abstract void Unbind();
        public abstract Texture GetTexture();
    }
}
