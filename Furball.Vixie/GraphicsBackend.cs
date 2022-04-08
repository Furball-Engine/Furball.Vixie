using System;
using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.Graphics.Backends.Direct3D11;
using Furball.Vixie.Graphics.Backends.OpenGL20;
using Furball.Vixie.Graphics.Backends.OpenGL41;
using Furball.Vixie.Graphics.Backends.OpenGLES;
using Furball.Vixie.Graphics.Backends.Veldrid;
using Furball.Vixie.Helpers;
using Kettu;

namespace Furball.Vixie {
    public class GraphicsBackend {
        /// <summary>
        /// Represents the Currently used Graphics Backend
        /// </summary>
        public static IGraphicsBackend Current;
        /// <summary>
        /// Sets the Graphics Backend
        /// </summary>
        /// <param name="backend">What backend to use</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws if a Invalid API was chosen</exception>
        public static void SetBackend(Backend backend) {
            Current = backend switch {
                Backend.OpenGLES   => new OpenGLESBackend(),
                Backend.Direct3D11 => new Direct3D11Backend(),
                Backend.OpenGL20   => new OpenGL20Backend(),
                Backend.OpenGL41   => new OpenGL41Backend(),
                Backend.Veldrid    => new VeldridBackend(),
                _                  => throw new ArgumentOutOfRangeException(nameof (backend), backend, "Invalid API")
            };
        }
        
                public static bool IsOnUnsupportedPlatform {
            get;
            private set;
        } = false;

        public static Backend PrefferedBackends = Backend.None;
        public static Backend GetReccomendedBackend() {
            bool preferVeldridOverNative  = PrefferedBackends.HasFlag(Backend.Veldrid);
            bool preferOpenGl             = PrefferedBackends.HasFlag(Backend.OpenGL41);
            bool preferOpenGlLegacy       = PrefferedBackends.HasFlag(Backend.OpenGL20);
            bool preferOpenGlesOverOpenGl = PrefferedBackends.HasFlag(Backend.OpenGLES);
            
            if (OperatingSystem.IsWindows()) {
                if (preferVeldridOverNative) 
                    return Backend.Veldrid;
                return preferOpenGlesOverOpenGl ? Backend.OpenGLES : preferOpenGlLegacy ? Backend.OpenGL20 : Backend.OpenGL41;
            }
            if (OperatingSystem.IsAndroid()) {
                return preferVeldridOverNative ? Backend.Veldrid : Backend.OpenGLES;
            }
            if (OperatingSystem.IsLinux()) {
                if (preferVeldridOverNative) 
                    return Backend.Veldrid;
                return preferOpenGlesOverOpenGl ? Backend.OpenGLES : preferOpenGlLegacy ? Backend.OpenGL20 : Backend.OpenGL41;
            }
            if (OperatingSystem.IsMacOSVersionAtLeast(10, 11)) { //Most models that run this version are guarenteed Metal support, so go with veldrid
                if (preferOpenGl) {
                    if(preferOpenGlesOverOpenGl)
                        Logger.Log("OpenGLES is considered unsupported on MacOS!", LoggerLevelDebugMessageCallback.InstanceNotification);

                    return preferOpenGlesOverOpenGl ? Backend.OpenGLES : preferOpenGlLegacy ? Backend.OpenGL20 : Backend.OpenGL41;
                }
                return Backend.Veldrid;
            }
            if(OperatingSystem.IsMacOS()) { //if we are on older macos, then we cant use Metal probably, so just stick with GL
                if (preferVeldridOverNative) 
                    return Backend.Veldrid; //we can just pray that Veldrid doesnt actually choose metal for these old things
                
                if(preferOpenGlesOverOpenGl)
                    Logger.Log("OpenGLES is considered unsupported on MacOS!", LoggerLevelDebugMessageCallback.InstanceNotification);

                return preferOpenGlesOverOpenGl ? Backend.OpenGLES : preferOpenGlLegacy ? Backend.OpenGL20 : Backend.OpenGL41;
            }
            if (OperatingSystem.IsFreeBSD()) {
                return preferOpenGlesOverOpenGl ? Backend.OpenGLES : preferOpenGlLegacy ? Backend.OpenGL20 : Backend.OpenGL41;
            }

            Logger.Log("You are running on an untested, unsupported platform!", LoggerLevelDebugMessageCallback.InstanceNotification);
            IsOnUnsupportedPlatform = true;
            return Backend.OpenGL20; //return the most supported backend
        }
    }
}
