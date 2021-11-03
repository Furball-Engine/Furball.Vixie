using System.Drawing;
using System.Numerics;
using FontStashSharp.Interfaces;
using Furball.Vixie.Gl;
using Furball.Vixie.Graphics;

namespace Furball.Vixie.FontStashSharp {
    public class VixieFontStashRenderer : IFontStashRenderer {
        private BatchedRenderer _renderer;

        public VixieFontStashRenderer(BatchedRenderer renderer) {
            this._renderer      = renderer;
            this.TextureManager = new VixieTexture2dManager();
        }

        public void Draw(object texture, Vector2 pos, Rectangle? src, Color color, float rotation, Vector2 origin, Vector2 scale, float depth) {
            //TODO(Eevee): color tint, source rectangle, proper rotation
            this._renderer.Draw(texture as Texture, pos, Vector2.Zero, scale, rotation);
        }

        public ITexture2DManager TextureManager { get; }
    }
}
