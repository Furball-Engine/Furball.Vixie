// ReSharper disable InconsistentNaming

using System;

namespace Furball.Vixie.Backends.Shared.Backends;

[Flags]
public enum Backend {
    None       = 1 << 0, //Not a real backend
#if VIXIE_BACKEND_D3D11
    Direct3D11 = 1 << 1,
#endif
#if VIXIE_BACKEND_OPENGL
    OpenGL     = 1 << 2,
    OpenGLES   = 1 << 3,
#endif
#if VIXIE_BACKEND_VELDRID
    Veldrid    = 1 << 4,
#endif
#if VIXIE_BACKEND_VULKAN
    Vulkan     = 1 << 5,
#endif
#if VIXIE_BACKEND_MOLA
    Mola       = 1 << 6,
#endif
#if VIXIE_BACKEND_DUMMY
    Dummy      = 1 << 7,
#endif
#if VIXIE_BACKEND_WEBGPU
    WebGPU     = 1 << 8,
#endif
}