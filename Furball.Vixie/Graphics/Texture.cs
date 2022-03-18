using System.Drawing;
using System.IO;
using System.Numerics;
using Furball.Vixie.Graphics.Backends;

namespace Furball.Vixie.Graphics {
    public abstract class Texture {
        public abstract Vector2 Size { get; protected set; }
        public int Width => (int)Size.X;
        public int Height => (int)Size.Y;

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

        public abstract Texture SetData<pDataType>(int level, pDataType[] data) where pDataType : unmanaged;
        public abstract Texture SetData<pDataType>(int level, Rectangle rect, pDataType[] data) where pDataType : unmanaged;
    }
}
