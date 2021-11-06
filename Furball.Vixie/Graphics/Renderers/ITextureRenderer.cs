using System.Drawing;
using System.Numerics;

namespace Furball.Vixie.Graphics.Renderers {
    public interface ITextureRenderer {
        /// <summary>
        /// Begins the Renderer, used for initializing things
        /// </summary>
        void Begin();
        /// <summary>
        /// Draws a Texture
        /// </summary>
        /// <param name="texture">Texture to Draw</param>
        /// <param name="position">Where to Draw</param>
        /// <param name="size">How big to draw, leave null to get Texture Size</param>
        /// <param name="scale">How much to scale it up, Leave null to draw at standard scale</param>
        /// <param name="rotation">Rotation in Radians, leave 0 to not rotate</param>
        /// <param name="colorOverride">Color Tint, leave null to not tint</param>
        /// <param name="sourceRect">What part of the texture to draw? Leave null to draw whole texture</param>
        /// <param name="effects">Horizontally/Vertically flip the Drawn Texture</param>
        void Draw(Texture texture, Vector2 position, Vector2? size = null, Vector2? scale = null, float rotation = 0f, Color? colorOverride = null, Rectangle? sourceRect = null, SpriteEffects effects = SpriteEffects.None);
        /// <summary>
        /// Ends the Rendering, use this to finish drawing or do something at the very end
        /// </summary>
        void End();
    }
}
