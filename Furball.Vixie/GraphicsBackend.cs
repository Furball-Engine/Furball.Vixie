using System;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Direct3D11;
using Furball.Vixie.Backends.OpenGL20;
using Furball.Vixie.Backends.OpenGL41;
using Furball.Vixie.Backends.OpenGLES;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Veldrid;
using Furball.Vixie.Backends.Vulkan;
using Furball.Vixie.Helpers.Helpers;
using Kettu;

namespace Furball.Vixie; 

public class GraphicsBackend {
    /// <summary>
    ///     Represents the Currently used Graphics Backend
    /// </summary>
    public static IGraphicsBackend Current;
    /// <summary>
    ///     Sets the Graphics Backend
    /// </summary>
    /// <param name="backend">What backend to use</param>
    /// <exception cref="ArgumentOutOfRangeException">Throws if a Invalid API was chosen</exception>
    public static void SetBackend(Backend backend) {
        Current = backend switch {
            Backend.OpenGLES     => new OpenGLESBackend(),
            Backend.Direct3D11   => new Direct3D11Backend(),
            Backend.LegacyOpenGL => new OpenGL20Backend(),
            Backend.ModernOpenGL => new OpenGL41Backend(),
            Backend.Veldrid      => new VeldridBackend(),
            Backend.Vulkan       => new VulkanBackend(),
            _                    => throw new ArgumentOutOfRangeException(nameof (backend), backend, "Invalid API")
        };
    }

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
        bool preferOpenGl             = PrefferedBackends.HasFlag(Backend.ModernOpenGL);
        bool preferOpenGlLegacy       = PrefferedBackends.HasFlag(Backend.LegacyOpenGL);
        bool preferOpenGlesOverOpenGl = PrefferedBackends.HasFlag(Backend.OpenGLES);
            
        bool supportsGl   = Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion   != 0;
        bool supportsGles = Backends.Shared.Global.LatestSupportedGL.GLES.MajorVersion != 0;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            if (preferVeldridOverNative)
                return Backend.Veldrid;

            if (!preferOpenGl)
                return Backend.Direct3D11;

            if ((!supportsGl && supportsGles) || (supportsGles))
                if (preferOpenGlesOverOpenGl) {
                    if (Backends.Shared.Global.LatestSupportedGL.GLES.MajorVersion < 3)
                        throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");
                        
                    return Backend.OpenGLES;
                }

            if (supportsGl) {
                //if we are asking for legacy, or our GPU doesnt support OpenGL 4.x
                if (preferOpenGlLegacy || Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion < 3 || Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion == 3 && Backends.Shared.Global.LatestSupportedGL.GL.MinorVersion < 2) {
                    if (Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion < 2)
                        throw new NotSupportedException("Your GPU does not support OpenGL version 2.0 or above!");

                    return Backend.LegacyOpenGL;
                }

                if (Backends.Shared.Global.LatestSupportedGL.GL.MinorVersion >= 1)
                    return Backend.ModernOpenGL;
            }

            throw new NotSupportedException("Your GPU does not support OpenGL!");
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"))) {
            if (preferVeldridOverNative) return Backend.Veldrid;

            if (supportsGles)
                if (preferOpenGlesOverOpenGl) {
                    if (Backends.Shared.Global.LatestSupportedGL.GLES.MajorVersion < 3)
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
                    if (Backends.Shared.Global.LatestSupportedGL.GLES.MajorVersion < 3)
                        throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                    return Backend.OpenGLES;
                }

            if (supportsGl) {
                //if we are asking for legacy, or our GPU doesnt support OpenGL 4.x
                if (preferOpenGlLegacy || Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion < 3 || Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion == 3 && Backends.Shared.Global.LatestSupportedGL.GL.MinorVersion < 2) {
                    if (Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion < 2)
                        throw new NotSupportedException("Your GPU does not support OpenGL version 2.0 or above!");

                    return Backend.LegacyOpenGL;
                }

                if (Backends.Shared.Global.LatestSupportedGL.GL.MinorVersion >= 1)
                    return Backend.ModernOpenGL;
            }

            throw new NotSupportedException("Your GPU does not support OpenGL!");
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            if (preferOpenGl) {
                if (preferOpenGlesOverOpenGl)
                    Logger.Log("OpenGLES is considered unsupported on MacOS!", LoggerLevelDebugMessageCallback.InstanceNotification);

                if ((!supportsGl && supportsGles) || (supportsGles))
                    if (preferOpenGlesOverOpenGl) {
                        if (Backends.Shared.Global.LatestSupportedGL.GLES.MajorVersion < 3)
                            throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                        return Backend.OpenGLES;
                    }

                if (supportsGl) {
                    //if we are asking for legacy, or our GPU doesnt support OpenGL 4.x
                    if (preferOpenGlLegacy || Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion < 3 || Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion == 3 && Backends.Shared.Global.LatestSupportedGL.GL.MinorVersion < 2) {
                        if (Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion < 2)
                            throw new NotSupportedException("Your GPU does not support OpenGL version 2.0 or above!");

                        return Backend.LegacyOpenGL;
                    }

                    if (Backends.Shared.Global.LatestSupportedGL.GL.MinorVersion >= 1)
                        return Backend.ModernOpenGL;
                }

                throw new NotSupportedException("Your GPU does not support OpenGL!");
            }
                
            return Backend.Veldrid;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD"))) {
            if ((!supportsGl && supportsGles) || (supportsGles))
                if (preferOpenGlesOverOpenGl) {
                    if (Backends.Shared.Global.LatestSupportedGL.GLES.MajorVersion < 3)
                        throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                    return Backend.OpenGLES;
                }

            if (supportsGl) {
                //if we are asking for legacy, or our GPU doesnt support OpenGL 4.x
                if (preferOpenGlLegacy || Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion < 3 || Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion == 3 && Backends.Shared.Global.LatestSupportedGL.GL.MinorVersion < 2) {
                    if (Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion < 2)
                        throw new NotSupportedException("Your GPU does not support OpenGL version 2.0 or above!");

                    return Backend.LegacyOpenGL;
                }

                if (Backends.Shared.Global.LatestSupportedGL.GL.MinorVersion >= 1)
                    return Backend.ModernOpenGL;
            }

            throw new NotSupportedException("Your GPU does not support OpenGL!");
        }

        Logger.Log("You are running on an untested, unsupported platform!", LoggerLevelDebugMessageCallback.InstanceNotification);
        IsOnUnsupportedPlatform = true;
        return Backend.LegacyOpenGL; //return the most likely supported backend
    }
}