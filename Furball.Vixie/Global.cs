using System;
using System.Collections.Generic;
#if VIXIE_BACKEND_OPENGL
using Furball.Vixie.Backends.OpenGL;
#endif
using Furball.Vixie.Backends.Shared.Backends;

namespace Furball.Vixie; 

public static class Global {
    internal static bool          AlreadyInitialized;
    internal static Game          GameInstance;
        
    internal static Dictionary<string, FeatureLevel> GetFeatureLevels(Backend backend) {
        return backend switch {
            Backend.None       => new Dictionary<string, FeatureLevel>(),
#if VIXIE_BACKEND_D3D11
            Backend.Direct3D11 => new Dictionary<string, FeatureLevel>(),
#endif
#if VIXIE_BACKEND_OPENGL
            Backend.OpenGL     => OpenGLBackend.FeatureLevels,
            Backend.OpenGLES   => OpenGLBackend.FeatureLevels,
#endif
#if VIXIE_BACKEND_VULKAN
            Backend.Vulkan     => new Dictionary<string, FeatureLevel>(),
#endif
#if VIXIE_BACKEND_WEBGPU
            Backend.WebGPU     => new Dictionary<string, FeatureLevel>(),            
#endif
            _                  => throw new ArgumentOutOfRangeException(nameof (backend), backend, null)
        };
    }
    
    public static readonly List<WeakReference<Texture>>      TrackedTextures      = new();
    public static readonly List<WeakReference<RenderTarget>> TrackedRenderTargets = new();
    public static readonly List<WeakReference<Renderer>>     TrackedRenderers     = new();
}