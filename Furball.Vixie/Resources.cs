using System.IO;
using Furball.Vixie.Backends.Shared;

namespace Furball.Vixie {
    public class Resources {
        public static TextureRenderTarget CreateTextureRenderTarget(uint width, uint height) {
            return GraphicsBackend.Current.CreateRenderTarget(width, height);
        }
        
        public static Texture CreateTexture(byte[] imageData, bool qoi = false) {
            return GraphicsBackend.Current.CreateTexture(imageData, qoi);
        }

        public static Texture CreateTexture() {
            return GraphicsBackend.Current.CreateWhitePixelTexture();
        }

        public static Texture CreateTexture(Stream stream) {
            return GraphicsBackend.Current.CreateTexture(stream);
        }

        public static Texture CreateTexture(uint width, uint height) {
            return GraphicsBackend.Current.CreateTexture(width, height);
        }

        public static Texture CreateTexture(string filepath) {
            return GraphicsBackend.Current.CreateTexture(filepath);
        }
    }
}
