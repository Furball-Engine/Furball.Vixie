// ReSharper disable InconsistentNaming

using System;
using Silk.NET.Windowing;

namespace Furball.Vixie.Backends.Shared.Backends {
    [Flags]
    public enum Backend {
        None       = 1 << 0,//Not a real backend
        Direct3D11 = 1 << 1,
        OpenGL20   = 1 << 2,
        OpenGL41   = 1 << 3,
        OpenGLES30 = 1 << 4,
        OpenGLES32 = 1 << 5,
        Veldrid    = 1 << 6,
    }

    public struct SysetmSupportedVersions {
        public APIVersion OpenGL;
        public APIVersion OpenGLES;
        public APIVersion Vulkan; //TODO
        public APIVersion DirectX; //TODO
    }
}
