using System;
using System.Drawing;
using System.Numerics;
using FontStashSharp;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.FontStashSharp;
using Color = Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie; 

public class Renderer {
    private IQuadRenderer          _quadRenderer;
    private VixieFontStashRenderer _fssRenderer;
    
    public Renderer() {
        this.Recreate();
        
        Global.TRACKED_RENDERERS.Add(new WeakReference<Renderer>(this));
    }

    internal void InternalDispose() {
        this._quadRenderer.Dispose();
    }

    private bool _isDisposed;
    public void Dispose() {
        if (this._isDisposed)
            return;

        this._isDisposed = true;
        
        this._quadRenderer.Dispose();
    }

    internal void Recreate() {
        this._quadRenderer = GraphicsBackend.Current.CreateTextureRenderer();
        this._fssRenderer  = new VixieFontStashRenderer(this._quadRenderer);
    }

    public bool IsBegun => this._quadRenderer.IsBegun;
    /// <summary>
    /// Begins the Renderer, used for initializing things
    /// </summary>
    public void Begin() {
        this._quadRenderer.Begin();
    }
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
    public void Draw(VixieTexture vixieTexture,               Vector2 position, Vector2 scale, float rotation, Color colorOverride,
                     TextureFlip  texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        this._quadRenderer.Draw(vixieTexture, position, scale, rotation, colorOverride, texFlip, rotOrigin);
    }
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
    public void Draw(VixieTexture vixieTexture, Vector2     position,                   Vector2 scale, float rotation, Color colorOverride,
                     Rectangle    sourceRect,   TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        this._quadRenderer.Draw(vixieTexture, position, scale, rotation, colorOverride, sourceRect, texFlip, rotOrigin);
    }
    public void Draw(VixieTexture vixieTexture,            Vector2 position, float rotation = 0,
                     TextureFlip  flip = TextureFlip.None, Vector2 rotOrigin = default) {
        this._quadRenderer.Draw(vixieTexture, position, rotation, flip, rotOrigin);
    }
    public void Draw(VixieTexture vixieTexture,            Vector2 position, Vector2 scale, float rotation = 0,
                     TextureFlip  flip = TextureFlip.None, Vector2 rotOrigin = default) {
        this._quadRenderer.Draw(vixieTexture, position, scale, rotation, flip, rotOrigin);
    }
    public void Draw(VixieTexture vixieTexture, Vector2     position,                   Vector2 scale, Color colorOverride,
                     float        rotation = 0, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        this._quadRenderer.Draw(vixieTexture, position, scale, colorOverride, rotation, texFlip, rotOrigin);
    }

    /// <summary>
    /// Batches Text to the Screen
    /// </summary>
    /// <param name="font">Font to Use</param>
    /// <param name="text">Text to Write</param>
    /// <param name="position">Where to Draw</param>
    /// <param name="color">What color to draw</param>
    /// <param name="rotation">Rotation of the text</param>
    /// <param name="scale">Scale of the text, leave null to draw at standard scale</param>
    public void DrawString(DynamicSpriteFont font,         string  text, Vector2 position, Color color, float rotation = 0f,
                           Vector2?          scale = null, Vector2 origin = default) {
        font.DrawText(this._fssRenderer, text, position, System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B), scale, rotation, origin);
    }
    /// <summary>
    /// Batches Text to the Screen
    /// </summary>
    /// <param name="font">Font to Use</param>
    /// <param name="text">Text to Write</param>
    /// <param name="position">Where to Draw</param>
    /// <param name="color">What color to draw</param>
    /// <param name="rotation">Rotation of the text</param>
    /// <param name="scale">Scale of the text, leave null to draw at standard scale</param>
    public void DrawString(DynamicSpriteFont font,          string   text,         Vector2 position, System.Drawing.Color color,
                           float             rotation = 0f, Vector2? scale = null, Vector2 origin = default) {
        font.DrawText(this._fssRenderer, text, position, System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B), scale, rotation, origin);
    }
    /// <summary>
    /// Batches Colorful text to the Screen
    /// </summary>
    /// <param name="font">Font to Use</param>
    /// <param name="text">Text to Write</param>
    /// <param name="position">Where to Draw</param>
    /// <param name="colors">What colors to use</param>
    /// <param name="rotation">Rotation of the text</param>
    /// <param name="scale">Scale of the text, leave null to draw at standard scale</param>
    public void DrawString(DynamicSpriteFont font,          string   text,         Vector2 position, System.Drawing.Color[] colors,
                           float             rotation = 0f, Vector2? scale = null, Vector2 origin = default) {
        font.DrawText(this._fssRenderer, text, position, colors, scale.Value, rotation, origin);
    }

    /// <summary>
    /// Ends the Rendering, use this to finish drawing or do something at the very end
    /// </summary>
    public void End() {
        this._quadRenderer.End();
    }
}