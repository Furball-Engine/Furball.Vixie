using System.Drawing;
using System.Numerics;
using FontStashSharp.Interfaces;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.Graphics.Backends.OpenGL.Abstractions;
using Furball.Vixie.Graphics.Renderers;
using Color=System.Drawing.Color;

namespace Furball.Vixie.FontStashSharp {
    public class VixieFontStashRenderer : IFontStashRenderer {
        private IQuadRenderer _renderer;
        private GraphicsBackend  _backend;

        public VixieFontStashRenderer(GraphicsBackend backend, IQuadRenderer renderer) {
            this._backend       = backend;
            this._renderer      = renderer;
            this.TextureManager = new VixieTexture2dManager(backend);
        }

        public void Draw(object texture, Vector2 pos, Rectangle? src, Color color, float rotation, Vector2 origin, Vector2 scale, float depth) {
            Texture actualTextureGl = texture as Texture;

            Rectangle rect = src.Value;
            rect.Y = actualTextureGl.Height - rect.Y - rect.Height;

            this._renderer.Draw(actualTextureGl, pos, scale, rotation, new Graphics.Color(color.R, color.G, color.B, color.A), rect, TextureFlip.None, origin * scale);
        }

        public ITexture2DManager TextureManager { get; }
    }
}
