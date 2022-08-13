using System;
using System.Drawing;
using System.Numerics;

namespace Furball.Vixie.Backends.Shared.Renderers; 

public interface IQuadRenderer : IDisposable {
    public bool IsBegun { get; set; }
    /// <summary>
    /// Begins the Renderer, used for initializing things
    /// </summary>
    void Begin();
    /// <summary>
    /// Draws a Texture
    /// </summary>
    /// <param name="vixieTexture">Texture to Draw</param>
    /// <param name="position">Where to Draw</param>
    /// <param name="scale">How much to scale it up, Leave null to draw at standard scale</param>
    /// <param name="rotation">Rotation in Radians, leave 0 to not rotate</param>
    /// <param name="colorOverride">Color Tint, leave null to not tint</param>
    /// <param name="texFlip">Horizontally/Vertically flip the Drawn Texture</param>
    /// <param name="rotOrigin">origin of rotation, by default the top left</param>
    void Draw(VixieTexture vixieTexture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default);
    /// <summary>
    /// Draws a Texture
    /// </summary>
    /// <param name="vixieTexture">Texture to Draw</param>
    /// <param name="position">Where to Draw</param>
    /// <param name="scale">How much to scale it up, Leave null to draw at standard scale</param>
    /// <param name="rotation">Rotation in Radians, leave 0 to not rotate</param>
    /// <param name="colorOverride">Color Tint, leave null to not tint</param>
    /// <param name="sourceRect">What part of the texture to draw? Leave null to draw whole texture</param>
    /// <param name="texFlip">Horizontally/Vertically flip the Drawn Texture</param>
    /// <param name="rotOrigin">origin of rotation, by default the top left</param>
    void Draw(VixieTexture vixieTexture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default);
    void Draw(VixieTexture vixieTexture, Vector2 position, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default);
    void Draw(VixieTexture vixieTexture, Vector2 position, Vector2 scale, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default);
    void Draw(VixieTexture vixieTexture, Vector2 position, Vector2 scale, Color colorOverride, float rotation = 0, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default);
    /// <summary>
    /// Ends the Rendering, use this to finish drawing or do something at the very end
    /// </summary>
    void End();
}