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
using Silk.NET.Windowing;

namespace Furball.Vixie {
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
                Backend.OpenGLES30 => new OpenGLESBackend(false),
                Backend.OpenGLES32 => new OpenGLESBackend(true),
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
            bool preferVeldridOverNative       = PrefferedBackends.HasFlag(Backend.Veldrid);
            bool preferOpenGl                  = PrefferedBackends.HasFlag(Backend.OpenGL41);
            bool preferOpenGlLegacy            = PrefferedBackends.HasFlag(Backend.OpenGL20);
            bool preferOpenGlesOverOpenGl      = PrefferedBackends.HasFlag(Backend.OpenGLES32);
            bool preferOpenGlesOldOverOpenGles = PrefferedBackends.HasFlag(Backend.OpenGLES30);

            (APIVersion gl, APIVersion gles) = OpenGLDetector.OpenGLDetector.GetLatestSupported();

            bool supportsGl   = gl.MajorVersion   != 0;
            bool supportsGles = gles.MajorVersion != 0;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                if (preferVeldridOverNative)
                    return Backend.Veldrid;

                if (!preferOpenGl)
                    return Backend.Direct3D11;

                if ((!supportsGl && supportsGles) || (supportsGles))
                    if (preferOpenGlesOverOpenGl) {
                        if (gles.MajorVersion < 3)
                            throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                        if (preferOpenGlesOldOverOpenGles)
                            return Backend.OpenGLES30;

                        if (gles.MinorVersion >= 2)
                            return Backend.OpenGLES32;

                        return Backend.OpenGLES30;
                    }

                if (supportsGl) {
                    //if we are asking for legacy, or our GPU doesnt support OpenGL 4.x
                    if (preferOpenGlLegacy || gl.MajorVersion < 4 || (gl.MajorVersion == 4 && gl.MinorVersion == 0)) {
                        if (gl.MajorVersion < 2)
                            throw new NotSupportedException("Your GPU does not support OpenGL version 2.0 or above!");

                        return Backend.OpenGL20;
                    }

                    if (gl.MinorVersion >= 1)
                        return Backend.OpenGL41;
                }

                throw new NotSupportedException("Your GPU does not support OpenGL!");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"))) {
                if (preferVeldridOverNative) return Backend.Veldrid;

                if (supportsGles)
                    if (preferOpenGlesOverOpenGl) {
                        if (gles.MajorVersion < 3)
                            throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                        if (preferOpenGlesOldOverOpenGles)
                            return Backend.OpenGLES30;

                        if (gles.MinorVersion >= 2)
                            return Backend.OpenGLES32;

                        return Backend.OpenGLES30;
                    }

                throw new NotSupportedException("Your phone does not support GLES?");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                if (preferVeldridOverNative)
                    return Backend.Veldrid;

                if ((!supportsGl && supportsGles) || (supportsGles))
                    if (preferOpenGlesOverOpenGl) {
                        if (gles.MajorVersion < 3)
                            throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                        if (preferOpenGlesOldOverOpenGles)
                            return Backend.OpenGLES30;

                        if (gles.MinorVersion >= 2)
                            return Backend.OpenGLES32;

                        return Backend.OpenGLES30;
                    }

                if (supportsGl) {
                    //if we are asking for legacy, or our GPU doesnt support OpenGL 4.x
                    if (preferOpenGlLegacy || gl.MajorVersion < 4 || (gl.MajorVersion == 4 && gl.MinorVersion == 0)) {
                        if (gl.MajorVersion < 2)
                            throw new NotSupportedException("Your GPU does not support OpenGL version 2.0 or above!");

                        return Backend.OpenGL20;
                    }

                    if (gl.MinorVersion >= 1)
                        return Backend.OpenGL41;
                }

                throw new NotSupportedException("Your GPU does not support OpenGL!");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                if (preferOpenGl) {
                    if (preferOpenGlesOverOpenGl)
                        Logger.Log("OpenGLES is considered unsupported on MacOS!", LoggerLevelDebugMessageCallback.InstanceNotification);

                    if ((!supportsGl && supportsGles) || (supportsGles))
                        if (preferOpenGlesOverOpenGl) {
                            if (gles.MajorVersion < 3)
                                throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                            if (preferOpenGlesOldOverOpenGles)
                                return Backend.OpenGLES30;

                            if (gles.MinorVersion >= 2)
                                return Backend.OpenGLES32;

                            return Backend.OpenGLES30;
                        }

                    if (supportsGl) {
                        //if we are asking for legacy, or our GPU doesnt support OpenGL 4.x
                        if (preferOpenGlLegacy || gl.MajorVersion < 4 || (gl.MajorVersion == 4 && gl.MinorVersion == 0)) {
                            if (gl.MajorVersion < 2)
                                throw new NotSupportedException("Your GPU does not support OpenGL version 2.0 or above!");

                            return Backend.OpenGL20;
                        }

                        if (gl.MinorVersion >= 1)
                            return Backend.OpenGL41;
                    }

                    throw new NotSupportedException("Your GPU does not support OpenGL!");
                }
                
                return Backend.Veldrid;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD"))) {
                if ((!supportsGl && supportsGles) || (supportsGles))
                    if (preferOpenGlesOverOpenGl) {
                        if (gles.MajorVersion < 3)
                            throw new NotSupportedException("Your GPU does not support OpenGLES version 3.0 or above!");

                        if (preferOpenGlesOldOverOpenGles)
                            return Backend.OpenGLES30;

                        if (gles.MinorVersion >= 2)
                            return Backend.OpenGLES32;

                        return Backend.OpenGLES30;
                    }

                if (supportsGl) {
                    //if we are asking for legacy, or our GPU doesnt support OpenGL 4.x
                    if (preferOpenGlLegacy || gl.MajorVersion < 4) {
                        if (gl.MajorVersion < 2)
                            throw new NotSupportedException("Your GPU does not support OpenGL version 2.0 or above!");

                        return Backend.OpenGL20;
                    }

                    if (gl.MinorVersion >= 1)
                        return Backend.OpenGL41;
                }

                throw new NotSupportedException("Your GPU does not support OpenGL!");
            }

            Logger.Log("You are running on an untested, unsupported platform!", LoggerLevelDebugMessageCallback.InstanceNotification);
            IsOnUnsupportedPlatform = true;
            return Backend.OpenGL20;//return the most likely supported backend
        }
    }
}
