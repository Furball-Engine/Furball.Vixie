using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;

namespace Furball.Vixie; 

public static class RendererExtensions {
    public static unsafe void AllocateUnrotatedTexturedQuad(this IRenderer renderer, VixieTexture tex, Vector2 position, Vector2 scale) {
        Vector2 size = new(tex.Width * scale.X, tex.Height * scale.Y);
        
        MappedData mappedData = renderer.Reserve(4, 6);
        mappedData.VertexPtr[0] = new Vertex {
            Position          = position,
            Color             = Color.White,
            TexId             = renderer.GetTextureId(tex),
            TextureCoordinate = new Vector2(0, 0)
        };
        mappedData.VertexPtr[1] = new Vertex {
            Position = position + size with {
                Y = 0
            },
            Color             = Color.White,
            TexId             = renderer.GetTextureId(tex),
            TextureCoordinate = new Vector2(1, 0)
        };
        mappedData.VertexPtr[2] = new Vertex {
            Position          = position + size,
            Color             = Color.White,
            TexId             = renderer.GetTextureId(tex),
            TextureCoordinate = new Vector2(1, 1)
        };
        mappedData.VertexPtr[3] = new Vertex {
            Position = position + size with {
                X = 0
            },
            Color             = Color.White,
            TexId             = renderer.GetTextureId(tex),
            TextureCoordinate = new Vector2(0, 1)
        };
        mappedData.IndexPtr[0] = (ushort)(3 + mappedData.IndexOffset);
        mappedData.IndexPtr[1] = (ushort)(2 + mappedData.IndexOffset);
        mappedData.IndexPtr[2] = (ushort)(0 + mappedData.IndexOffset);
        mappedData.IndexPtr[3] = (ushort)(1 + mappedData.IndexOffset);
        mappedData.IndexPtr[4] = (ushort)(2 + mappedData.IndexOffset);
        mappedData.IndexPtr[5] = (ushort)(0 + mappedData.IndexOffset);
    }
}