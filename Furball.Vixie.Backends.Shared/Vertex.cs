using System.Numerics;

namespace Furball.Vixie.Backends.Shared; 

public struct Vertex {
    Vector2 Position;
    Vector2 TextureCoordinate;
    Color   Color;
    int     TexId;
}