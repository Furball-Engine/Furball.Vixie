using System.IO;
using Furball.Vixie.Graphics;

namespace Furball.Vixie {
    public class Textures {
        public static Texture Create(byte[] imageData, bool qoi = false) {
            return GraphicsBackend.Current.CreateTexture(imageData, qoi);
        }

        public static Texture Create() {
            return GraphicsBackend.Current.CreateWhitePixelTexture();
        }

        public static Texture Create(Stream stream) {
            return GraphicsBackend.Current.CreateTexture(stream);
        }

        public static Texture Create(uint width, uint height) {
            return GraphicsBackend.Current.CreateTexture(width, height);
        }

        public static Texture Create(string filepath) {
            return GraphicsBackend.Current.CreateTexture(filepath);
        }
    }
}
