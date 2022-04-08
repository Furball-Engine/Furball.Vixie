using Furball.Vixie.Graphics;

namespace Furball.Vixie {
    public class TextureRenderTargets {
        public static TextureRenderTarget Create(uint width, uint height) {
            return GraphicsBackend.Current.CreateRenderTarget(width, height);
        }
    }
}
