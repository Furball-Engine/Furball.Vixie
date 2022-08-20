using System.Numerics;

namespace Furball.Vixie.Backends.Shared; 

public struct Vertex {
    public Vector2 Position;
    public Vector2 TextureCoordinate;
    public Color   Color;
    public long    TexId;
}