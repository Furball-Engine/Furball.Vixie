using System.Numerics;
using FontStashSharp.Interfaces;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;

namespace Furball.Vixie.FontStashSharp; 

public class VixieFontStashRenderer : IFontStashRenderer2 {
    internal readonly VixieRenderer VixieRenderer;
    public VixieFontStashRenderer(GraphicsBackend backend, VixieRenderer vixieRenderer) {
        this.VixieRenderer      = vixieRenderer;
        this.TextureManager = new VixieTexture2dManager(backend);
    }
    
    public unsafe void DrawQuad(object                         texture,    ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight,
                                ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight) {
        MappedData map = this.VixieRenderer.Reserve(4, 6);

        map.VertexPtr[0].Position = new Vector2(topLeft.Position.X, topLeft.Position.Y);
        map.VertexPtr[1].Position = new Vector2(topRight.Position.X, topRight.Position.Y);
        map.VertexPtr[2].Position = new Vector2(bottomLeft.Position.X, bottomLeft.Position.Y);
        map.VertexPtr[3].Position = new Vector2(bottomRight.Position.X, bottomRight.Position.Y);

        map.VertexPtr[0].Color.R = topLeft.Color.R;
        map.VertexPtr[0].Color.G = topLeft.Color.G;
        map.VertexPtr[0].Color.B = topLeft.Color.B;
        map.VertexPtr[0].Color.A = topLeft.Color.A;
        map.VertexPtr[1].Color.R = topRight.Color.R;
        map.VertexPtr[1].Color.G = topRight.Color.G;
        map.VertexPtr[1].Color.B = topRight.Color.B;
        map.VertexPtr[1].Color.A = topRight.Color.A;
        map.VertexPtr[2].Color.R = bottomLeft.Color.R;
        map.VertexPtr[2].Color.G = bottomLeft.Color.G;
        map.VertexPtr[2].Color.B = bottomLeft.Color.B;
        map.VertexPtr[2].Color.A = bottomLeft.Color.A;
        map.VertexPtr[3].Color.R = bottomRight.Color.R;
        map.VertexPtr[3].Color.G = bottomRight.Color.G;
        map.VertexPtr[3].Color.B = bottomRight.Color.B;
        map.VertexPtr[3].Color.A = bottomRight.Color.A;

        map.VertexPtr[0].TextureCoordinate = topLeft.TextureCoordinate;
        map.VertexPtr[1].TextureCoordinate = topRight.TextureCoordinate;
        map.VertexPtr[2].TextureCoordinate = bottomLeft.TextureCoordinate;
        map.VertexPtr[3].TextureCoordinate = bottomRight.TextureCoordinate;

        long texId = this.VixieRenderer.GetTextureId((Texture)texture);
        map.VertexPtr[0].TexId = texId;
        map.VertexPtr[1].TexId = texId;
        map.VertexPtr[2].TexId = texId;
        map.VertexPtr[3].TexId = texId;

        map.IndexPtr[0] = (ushort)(0 + map.IndexOffset);
        map.IndexPtr[1] = (ushort)(2 + map.IndexOffset);
        map.IndexPtr[2] = (ushort)(1 + map.IndexOffset);
        map.IndexPtr[3] = (ushort)(2 + map.IndexOffset);
        map.IndexPtr[4] = (ushort)(3 + map.IndexOffset);
        map.IndexPtr[5] = (ushort)(1 + map.IndexOffset);
    }
    public ITexture2DManager TextureManager {
        get;
    }
}