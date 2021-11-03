using System.Drawing;
using System.Numerics;
using FontStashSharp.Interfaces;
using Furball.Vixie.Gl;

namespace Furball.Vixie.FontStashSharp {
    public class VixieTexture2dManager : ITexture2DManager {
        public object CreateTexture(int width, int height) {
            return new Texture((uint) width, (uint) height);
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
