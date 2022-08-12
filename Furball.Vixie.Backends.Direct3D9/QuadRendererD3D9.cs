using System.Drawing;
using System.Numerics;
using FontStashSharp;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Color = Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie.Backends.Direct3D9; 

public class QuadRendererD3D9 : IQuadRenderer {
    public QuadRendererD3D9() {
        
    }
    
    public void Dispose() {
        throw new System.NotImplementedException();
    }
    public bool IsBegun {
        get;
        set;
    }
    public void Begin() {
        throw new System.NotImplementedException();
    }
    public void Draw(Texture     texture,                    Vector2 position, Vector2 scale, float rotation, Color colorOverride,
                     TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        throw new System.NotImplementedException();
    }
    public void Draw(Texture     texture,                    Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect,
                     TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        throw new System.NotImplementedException();
    }
    public void Draw(Texture texture, Vector2 position, float rotation = 0, TextureFlip flip = TextureFlip.None,
                     Vector2 rotOrigin = default) {
        throw new System.NotImplementedException();
    }
    public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation = 0, TextureFlip flip = TextureFlip.None,
                     Vector2 rotOrigin = default) {
        throw new System.NotImplementedException();
    }
    public void Draw(Texture     texture,                    Vector2 position, Vector2 scale, Color colorOverride, float rotation = 0,
                     TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        throw new System.NotImplementedException();
    }
    public void DrawString(DynamicSpriteFont font,         string  text, Vector2 position, Color color, float rotation = 0,
                           Vector2?          scale = null, Vector2 origin = default) {
        throw new System.NotImplementedException();
    }
    public void DrawString(DynamicSpriteFont font,         string  text, Vector2 position, System.Drawing.Color color, float rotation = 0,
                           Vector2?          scale = null, Vector2 origin = default) {
        throw new System.NotImplementedException();
    }
    public void DrawString(DynamicSpriteFont font,         string  text, Vector2 position, System.Drawing.Color[] colors, float rotation = 0,
                           Vector2?          scale = null, Vector2 origin = default) {
        throw new System.NotImplementedException();
    }
    public void End() {
        throw new System.NotImplementedException();
    }
}