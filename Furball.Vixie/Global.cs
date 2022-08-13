using System;
using System.Collections.Generic;
using Furball.Vixie.Backends.OpenGL;
using Furball.Vixie.Backends.Shared.Backends;

namespace Furball.Vixie; 

public static class Global {
    internal static bool          AlreadyInitialized;
    internal static Game          GameInstance;
    internal static WindowManager WindowManager;
        
    internal static Dictionary<string, FeatureLevel> GetFeatureLevels(Backend backend) {
        return backend switch {
            Backend.None       => new Dictionary<string, FeatureLevel>(),
            Backend.Direct3D11 => new Dictionary<string, FeatureLevel>(),
            Backend.OpenGL     => OpenGLBackend.FeatureLevels,
            Backend.OpenGLES   => OpenGLBackend.FeatureLevels,
            Backend.Veldrid    => new Dictionary<string, FeatureLevel>(),
            Backend.Vulkan     => new Dictionary<string, FeatureLevel>(),
            _                  => throw new ArgumentOutOfRangeException(nameof (backend), backend, null)
        };
    }
    
    public static readonly List<WeakReference<Texture>>      TRACKED_TEXTURES       = new();
    public static readonly List<WeakReference<RenderTarget>> TRACKED_RENDER_TARGETS = new();
    public static          List<WeakReference<Renderer>>     TRACKED_RENDERERS      = new();
}