// ReSharper disable InconsistentNaming

using System;

namespace Furball.Vixie.Backends.Shared.Backends;

[Flags]
public enum Backend {
    None       = 1 << 0, //Not a real backend
    Direct3D11 = 1 << 1,
    OpenGL     = 1 << 2,
    OpenGLES   = 1 << 3,
    Veldrid    = 1 << 4,
    Vulkan     = 1 << 5,
    Mola       = 1 << 6,
    Dummy      = 1 << 7
}