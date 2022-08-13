using System.Drawing;
using System.Numerics;
using FontStashSharp.Interfaces;
using Furball.Vixie.Backends.Shared.Renderers;
using Color = Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie.FontStashSharp; 

public class VixieFontStashRenderer : IFontStashRenderer {
    internal IQuadRenderer Renderer;

    public VixieFontStashRenderer(IQuadRenderer renderer) {
        this.Renderer      = renderer;
        this.TextureManager = new VixieTexture2dManager();
    }

    public void Draw(object texture, Vector2 pos, Rectangle? src, System.Drawing.Color color, float rotation, Vector2 scale, float depth) {
        Texture tex = (Texture)texture;

        this.Renderer.Draw(tex, pos, scale, rotation, new Color(color.R, color.G, color.B, color.A), src.Value);
    }
    public ITexture2DManager TextureManager { get; }
}