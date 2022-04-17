using System;
using System.Drawing;
using System.Numerics;
using FontStashSharp;

namespace Furball.Vixie.Backends.Shared.Renderers {
    public interface IQuadRenderer : IDisposable {
        public bool IsBegun { get; set; }
        /// <summary>
        /// Begins the Renderer, used for initializing things
        /// </summary>
        void Begin();
        /// <summary>
        /// Draws a Texture
        /// </summary>
        /// <param name="texture">Texture to Draw</param>
        /// <param name="position">Where to Draw</param>
        /// <param name="scale">How much to scale it up, Leave null to draw at standard scale</param>
        /// <param name="rotation">Rotation in Radians, leave 0 to not rotate</param>
        /// <param name="colorOverride">Color Tint, leave null to not tint</param>
        /// <param name="texFlip">Horizontally/Vertically flip the Drawn Texture</param>
        /// <param name="rotOrigin">origin of rotation, by default the top left</param>
        void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default);
        /// <summary>
        /// Draws a Texture
        /// </summary>
        /// <param name="texture">Texture to Draw</param>
        /// <param name="position">Where to Draw</param>
        /// <param name="scale">How much to scale it up, Leave null to draw at standard scale</param>
        /// <param name="rotation">Rotation in Radians, leave 0 to not rotate</param>
        /// <param name="colorOverride">Color Tint, leave null to not tint</param>
        /// <param name="sourceRect">What part of the texture to draw? Leave null to draw whole texture</param>
        /// <param name="texFlip">Horizontally/Vertically flip the Drawn Texture</param>
        /// <param name="rotOrigin">origin of rotation, by default the top left</param>
        void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default);
        void Draw(Texture texture, Vector2 position, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default);
        void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default);
        void Draw(Texture texture, Vector2 position, Vector2 scale, Color colorOverride, float rotation = 0, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default);

        /// <summary>
        /// Batches Text to the Screen
        /// </summary>
        /// <param name="font">Font to Use</param>
        /// <param name="text">Text to Write</param>
        /// <param name="position">Where to Draw</param>
        /// <param name="color">What color to draw</param>
        /// <param name="rotation">Rotation of the text</param>
        /// <param name="scale">Scale of the text, leave null to draw at standard scale</param>
        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation = 0f, Vector2? scale = null);
        /// <summary>
        /// Batches Text to the Screen
        /// </summary>
        /// <param name="font">Font to Use</param>
        /// <param name="text">Text to Write</param>
        /// <param name="position">Where to Draw</param>
        /// <param name="color">What color to draw</param>
        /// <param name="rotation">Rotation of the text</param>
        /// <param name="scale">Scale of the text, leave null to draw at standard scale</param>
        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color color, float rotation = 0f, Vector2? scale = null);
        /// <summary>
        /// Batches Colorful text to the Screen
        /// </summary>
        /// <param name="font">Font to Use</param>
        /// <param name="text">Text to Write</param>
        /// <param name="position">Where to Draw</param>
        /// <param name="colors">What colors to use</param>
        /// <param name="rotation">Rotation of the text</param>
        /// <param name="scale">Scale of the text, leave null to draw at standard scale</param>
        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color[] colors, float rotation = 0f, Vector2? scale = null);

        /// <summary>
        /// Ends the Rendering, use this to finish drawing or do something at the very end
        /// </summary>
        void End();
    }
}
