using System.Drawing;
using System.Numerics;
using FontStashSharp.Interfaces;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;

namespace Furball.Vixie.Backends.Shared.FontStashSharp {
    public class VixieFontStashRenderer : IFontStashRenderer {
        private IQuadRenderer _renderer;
        private IGraphicsBackend  _backend;

        public VixieFontStashRenderer(IGraphicsBackend backend, IQuadRenderer renderer) {
            this._backend       = backend;
            this._renderer      = renderer;
            this.TextureManager = new VixieTexture2dManager(backend);
        }

        public void Draw(object texture, Vector2 pos, Rectangle? src, System.Drawing.Color color, float rotation, Vector2 scale, float depth) {
            var tex = (Texture)texture;

            // Rectangle rect = src.Value;
            // rect.Y = tex.Height - rect.Y - rect.Height;

            this._renderer.Draw(tex, pos, scale, rotation, new Color(color.R, color.G, color.B, color.A), src.Value);
        }
        public ITexture2DManager TextureManager { get; }
    }
}
