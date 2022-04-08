using System.Drawing;
using System.Numerics;
using FontStashSharp.Interfaces;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Backends;

namespace Furball.Vixie.FontStashSharp {
    public class VixieTexture2dManager : ITexture2DManager {
        private IGraphicsBackend _backend;

        public VixieTexture2dManager(IGraphicsBackend backend) {
            this._backend = backend;
        }

        public object CreateTexture(int width, int height) {
            return this._backend.CreateTexture((uint) width, (uint) height);
        }

        public Point GetTextureSize(object texture) {
            // ReSharper disable once PossibleNullReferenceException
            Vector2 size = (texture as Texture).Size;

            return new Point((int) size.X, (int) size.Y);
        }

        public void SetTextureData(object texture, Rectangle bounds, byte[] data) {
            // ReSharper disable once PossibleNullReferenceException
            (texture as Texture).SetData(0, bounds, data);
        }
    }
}
