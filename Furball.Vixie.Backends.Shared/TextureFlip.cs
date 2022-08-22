using System;

namespace Furball.Vixie.Backends.Shared; 

[Flags]
public enum TextureFlip {
    None = 0,
    FlipHorizontal = 1 << 0,
    FlipVertical = 1 << 1
}