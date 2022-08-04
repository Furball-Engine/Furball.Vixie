// ReSharper disable InconsistentNaming

using System;

namespace Furball.Vixie.Backends.Shared.Backends;

[Flags]
public enum Backend {
    None       = 1 << 0, //Not a real backend
    Direct3D11 = 1 << 1,
    Direct3D9  = 1 << 2,
    OpenGL     = 1 << 3,
    OpenGLES   = 1 << 4,
    Veldrid    = 1 << 5,
    Vulkan     = 1 << 6,
    Dummy      = 1 << 7
}