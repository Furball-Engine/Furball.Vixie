using System;
using System.Collections.Generic;
using Furball.Vixie.Backends.OpenGL;
using Furball.Vixie.Backends.Shared.Backends;

namespace Furball.Vixie; 

internal static class Global {
    internal static bool          AlreadyInitialized;
    internal static Game          GameInstance;
    internal static WindowManager WindowManager;
        
    public static Dictionary<string, FeatureLevel> GetFeatureLevels(Backend backend) {
        return backend switch {
            Backend.None         => new Dictionary<string, FeatureLevel>(),
            Backend.Direct3D11   => new Dictionary<string, FeatureLevel>(),
            Backend.OpenGL => OpenGLBackend.FeatureLevels,
            Backend.OpenGLES     => OpenGLBackend.FeatureLevels,
            Backend.Veldrid      => new Dictionary<string, FeatureLevel>(),
            Backend.Vulkan       => new Dictionary<string, FeatureLevel>(),
            _                    => throw new ArgumentOutOfRangeException(nameof(backend), backend, null)
        };
    }
}