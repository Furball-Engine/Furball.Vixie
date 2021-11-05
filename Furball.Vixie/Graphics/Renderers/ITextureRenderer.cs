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
        /// <param name="size">How big to draw</param>
        /// <param name="scale">How much to scale it up</param>
        /// <param name="rotation">Rotation in Radians</param>
        /// <param name="colorOverride">Color Tint</param>
        void Draw(Texture texture, Vector2 position, Vector2? size = null, Vector2? scale = null, float rotation = 0f, Color? colorOverride = null, Rectangle? sourceRect = null);
        /// <summary>
        /// Ends the Rendering, use this to finish drawing or do something at the very end
        /// </summary>
        void End();
    }
}
