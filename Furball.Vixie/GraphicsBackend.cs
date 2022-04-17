using System;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Direct3D11;
using Furball.Vixie.Backends.OpenGL20;
using Furball.Vixie.Backends.OpenGL41;
using Furball.Vixie.Backends.OpenGLES;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Veldrid;
using Furball.Vixie.Helpers.Helpers;
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
      
        public static bool IsOnUnsupportedPlatform { get; private set; } = false;

        public static Backend PrefferedBackends = Backend.None;
        public static Backend GetReccomendedBackend() {
            bool preferVeldridOverNative  = PrefferedBackends.HasFlag(Backend.Veldrid);
            bool preferOpenGl             = PrefferedBackends.HasFlag(Backend.OpenGL41);
            bool preferOpenGlLegacy       = PrefferedBackends.HasFlag(Backend.OpenGL20);
            bool preferOpenGlesOverOpenGl = PrefferedBackends.HasFlag(Backend.OpenGLES);
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                if (preferVeldridOverNative) 
                    return Backend.Veldrid;

                if (!preferOpenGl)
                    return Backend.Direct3D11;
                    
                return preferOpenGlesOverOpenGl ? Backend.OpenGLES : preferOpenGlLegacy ? Backend.OpenGL20 : Backend.OpenGL41;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"))) {
                return preferVeldridOverNative ? Backend.Veldrid : Backend.OpenGLES;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                if (preferVeldridOverNative) 
                    return Backend.Veldrid;
                return preferOpenGlesOverOpenGl ? Backend.OpenGLES : preferOpenGlLegacy ? Backend.OpenGL20 : Backend.OpenGL41;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                if (preferOpenGl) {
                    if(preferOpenGlesOverOpenGl)
                        Logger.Log("OpenGLES is considered unsupported on MacOS!", LoggerLevelDebugMessageCallback.InstanceNotification);

                    return preferOpenGlesOverOpenGl ? Backend.OpenGLES : preferOpenGlLegacy ? Backend.OpenGL20 : Backend.OpenGL41;
                }
                return Backend.Veldrid;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD"))) {
                return preferOpenGlesOverOpenGl ? Backend.OpenGLES : preferOpenGlLegacy ? Backend.OpenGL20 : Backend.OpenGL41;
            }

            Logger.Log("You are running on an untested, unsupported platform!", LoggerLevelDebugMessageCallback.InstanceNotification);
            IsOnUnsupportedPlatform = true;
            return Backend.OpenGL20; //return the most supported backend
        }
    }
}
