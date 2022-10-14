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
            
        bool preferVeldridOverNative  = PrefferedBackends.HasFlag(Backend.Veldrid);
        bool preferOpenGl             = PrefferedBackends.HasFlag(Backend.OpenGL);
        bool preferOpenGlesOverOpenGl = PrefferedBackends.HasFlag(Backend.OpenGLES);
            
        bool supportsGl   = Backends.Shared.Global.LatestSupportedGl.GL.MajorVersion   != 0;
        bool supportsGles = Backends.Shared.Global.LatestSupportedGl.GLES.MajorVersion != 0;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            if (preferVeldridOverNative)
                return Backend.Veldrid;

            if (!preferOpenGl)
                return Backend.Direct3D11;

            if ((!supportsGl && supportsGles) || (supportsGles))
                if (preferOpenGlesOverOpenGl) {
                    if (Backends.Shared.Global.LatestSupportedGl.GLES.MajorVersion < 3)
                        throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");
                        
                    return Backend.OpenGLES;
                }

            if (supportsGl) {
                if(Backends.Shared.Global.LatestSupportedGl.GL.MajorVersion >= 2)
                    return Backend.OpenGL;
            }

            throw new NotSupportedException("Your GPU does not support OpenGL!");
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"))) {
            if (preferVeldridOverNative) return Backend.Veldrid;

            if (supportsGles)
                if (preferOpenGlesOverOpenGl) {
                    if (Backends.Shared.Global.LatestSupportedGl.GLES.MajorVersion < 3)
                        throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                    return Backend.OpenGLES;
                }

            throw new NotSupportedException("Your phone does not support GLES?");
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            if (preferVeldridOverNative)
                return Backend.Veldrid;

            if ((!supportsGl && supportsGles) || (supportsGles))
                if (preferOpenGlesOverOpenGl) {
                    if (Backends.Shared.Global.LatestSupportedGl.GLES.MajorVersion < 3)
                        throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                    return Backend.OpenGLES;
                }

            if (supportsGl) {
                if(Backends.Shared.Global.LatestSupportedGl.GL.MajorVersion >= 2)
                    return Backend.OpenGL;
            }

            throw new NotSupportedException("Your GPU does not support OpenGL!");
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            if (preferOpenGl) {
                if (preferOpenGlesOverOpenGl)
                    Logger.Log("OpenGLES is considered unsupported on MacOS!", LoggerLevelDebugMessageCallback.InstanceNotification);

                if ((!supportsGl && supportsGles) || (supportsGles))
                    if (preferOpenGlesOverOpenGl) {
                        if (Backends.Shared.Global.LatestSupportedGl.GLES.MajorVersion < 3)
                            throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                        return Backend.OpenGLES;
                    }

                if (supportsGl) {
                    if(Backends.Shared.Global.LatestSupportedGl.GL.MajorVersion >= 2)
                        return Backend.OpenGL;
                }

                throw new NotSupportedException("Your GPU does not support OpenGL!");
            }
                
            return Backend.Veldrid;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD"))) {
            if ((!supportsGl && supportsGles) || (supportsGles))
                if (preferOpenGlesOverOpenGl) {
                    if (Backends.Shared.Global.LatestSupportedGl.GLES.MajorVersion < 3)
                        throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                    return Backend.OpenGLES;
                }

            if (supportsGl) {
                if(Backends.Shared.Global.LatestSupportedGl.GL.MajorVersion >= 2)
                    return Backend.OpenGL;
            }

            throw new NotSupportedException("Your GPU does not support OpenGL!");
        }

        Logger.Log("You are running on an untested, unsupported platform!", LoggerLevelDebugMessageCallback.InstanceNotification);
        IsOnUnsupportedPlatform = true;
        return Backend.OpenGL;
    }
}