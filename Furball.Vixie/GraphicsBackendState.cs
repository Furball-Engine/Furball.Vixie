using System;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Helpers.Helpers;
using Kettu;

namespace Furball.Vixie; 

public static class GraphicsBackendState {
    public static bool IsOnUnsupportedPlatform {
        get;
        internal set;
    } = false; //TODO: notify the user somehow about *why* its unsupported

    public static Backend PrefferedBackends = Backend.None;
    public static Backend GetReccomendedBackend() {
        string backendForce = Environment.GetEnvironmentVariable("VIXIE_BACKEND_FORCE", EnvironmentVariableTarget.Process);
        if (backendForce != null) {
            if (!Enum.TryParse(backendForce, out Backend backend))
                throw new NotSupportedException($"{backendForce} is not a valid option for VIXIE_BACKEND_FORCE!");

            return backend;
        }
            
#if VIXIE_BACKEND_OPENGL
        bool preferOpenGl             = PrefferedBackends.HasFlag(Backend.OpenGL);
        bool preferOpenGlesOverOpenGl = PrefferedBackends.HasFlag(Backend.OpenGLES);

        Lazy<bool> supportsGl   = new(() => Backends.Shared.Global.LatestSupportedGl.Value.GL.MajorVersion   != 0);
        Lazy<bool> supportsGles = new(() => Backends.Shared.Global.LatestSupportedGl.Value.GLES.MajorVersion != 0);
#endif

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"))) {
            //TODO: check if we are able to use WebGPU (for now we will assume we can,
            //      but due to the fact that most browsers do not support WebGPU [at the time of writing neither
            //      chrome nor firefox have it enabled by default], this assumption is bad)
            
#if VIXIE_BACKEND_OPENGL
            //If the user wants to use OpenGL, use it instead of WebGPU
            if (preferOpenGl)
                return Backend.OpenGL;
#endif

#if VIXIE_BACKEND_WEBGPU
            //WebGPU is the fastest API available on the web, so its the default
            return Backend.WebGPU;
#endif
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#if VIXIE_BACKEND_OPENGL
            if (!preferOpenGl)
                return Backend.Direct3D11;
            
            if ((!supportsGl.Value && supportsGles.Value) || (supportsGles.Value))
                if (preferOpenGlesOverOpenGl) {
                    if (Backends.Shared.Global.LatestSupportedGl.Value.GLES.MajorVersion < 3)
                        throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                    return Backend.OpenGLES;
                }

            if (supportsGl.Value) {
                if (Backends.Shared.Global.LatestSupportedGl.Value.GL.MajorVersion >= 2)
                    return Backend.OpenGL;
            }

            throw new NotSupportedException("Your GPU does not support OpenGL!");
#endif

#if VIXIE_BACKEND_D3D11
#endif
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"))) {
#if VIXIE_BACKEND_OPENGL
            if (supportsGles.Value)
                if (preferOpenGlesOverOpenGl) {
                    if (Backends.Shared.Global.LatestSupportedGl.Value.GLES.MajorVersion < 3)
                        throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                    return Backend.OpenGLES;
                }

            throw new NotSupportedException("Your phone does not support GLES?");
#endif
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
#if VIXIE_BACKEND_OPENGL
            if ((!supportsGl.Value && supportsGles.Value) || (supportsGles.Value))
                if (preferOpenGlesOverOpenGl) {
                    if (Backends.Shared.Global.LatestSupportedGl.Value.GLES.MajorVersion < 3)
                        throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                    return Backend.OpenGLES;
                }

            if (supportsGl.Value) {
                if(Backends.Shared.Global.LatestSupportedGl.Value.GL.MajorVersion >= 2)
                    return Backend.OpenGL;
            }

            throw new NotSupportedException("Your GPU does not support OpenGL!");
#endif
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
#if VIXIE_BACKEND_OPENGL
            if (preferOpenGl) {
                if (preferOpenGlesOverOpenGl)
                    Logger.Log("OpenGLES is considered unsupported on MacOS!", LoggerLevelDebugMessageCallback.InstanceNotification);

                if ((!supportsGl.Value && supportsGles.Value) || (supportsGles.Value))
                    if (preferOpenGlesOverOpenGl) {
                        if (Backends.Shared.Global.LatestSupportedGl.Value.GLES.MajorVersion < 3)
                            throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                        return Backend.OpenGLES;
                    }

                if (supportsGl.Value) {
                    if(Backends.Shared.Global.LatestSupportedGl.Value.GL.MajorVersion >= 2)
                        return Backend.OpenGL;
                }

                throw new NotSupportedException("Your GPU does not support OpenGL!");
            }
#endif
            return Backend.OpenGL;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD"))) {
#if VIXIE_BACKEND_OPENGL
            if ((!supportsGl.Value && supportsGles.Value) || (supportsGles.Value))
                if (preferOpenGlesOverOpenGl) {
                    if (Backends.Shared.Global.LatestSupportedGl.Value.GLES.MajorVersion < 3)
                        throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                    return Backend.OpenGLES;
                }

            if (supportsGl.Value) {
                if(Backends.Shared.Global.LatestSupportedGl.Value.GL.MajorVersion >= 2)
                    return Backend.OpenGL;
            }

            throw new NotSupportedException("Your GPU does not support OpenGL!");
#endif
        }

        Logger.Log("You are running on an untested, unsupported platform!", LoggerLevelDebugMessageCallback.InstanceNotification);
        IsOnUnsupportedPlatform = true;
#if VIXIE_BACKEND_OPENGL
        return Backend.OpenGL;
#else
        throw new Exception("Unable to find suitable render backend for your platform!");
#endif
    }
}