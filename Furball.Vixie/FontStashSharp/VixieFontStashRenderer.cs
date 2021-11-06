using System.Drawing;
using System.Numerics;
using FontStashSharp.Interfaces;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Graphics.Renderers.OpenGL;

namespace Furball.Vixie.FontStashSharp {
    public class VixieFontStashRenderer : IFontStashRenderer {
        private BatchedRenderer _renderer;

        public VixieFontStashRenderer(BatchedRenderer renderer) {
            this._renderer      = renderer;
            this.TextureManager = new VixieTexture2dManager();
        }

        public void Draw(object texture, Vector2 pos, Rectangle? src, Color color, float rotation, Vector2 origin, Vector2 scale, float depth) {
            this._renderer.Draw(texture as Texture, pos - origin, Vector2.Zero, scale, rotation, color, src, SpriteEffects.FlipVertical);
        }

        public ITexture2DManager TextureManager { get; }
    }
}
