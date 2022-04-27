using System;
using System.Runtime.CompilerServices;
using Silk.NET.SDL;
using Silk.NET.Windowing;
using Window=Silk.NET.SDL.Window;

namespace Furball.Vixie.OpenGLDetector {
    public static class OpenGLDetector {
        private static unsafe Window* CreateWindow(Sdl sdl) {
            var window = sdl.CreateWindow("", 0, 0, 1, 1, (uint)(WindowFlags.WindowHidden | WindowFlags.WindowOpengl));

            if (window == (Window*)0)
                throw new Exception();

            return window;
        }

        private static APIVersion _LastTested = new(0, 0);
        private static APIVersion _LastTestedES = new(0, 0);

        private static readonly APIVersion[] OPEN_GL_VERSIONS = new [] {
            //1.x
            new APIVersion(1, 0),
            new APIVersion(1, 1),
            new APIVersion(1, 2),
            new APIVersion(1, 3),
            new APIVersion(1, 4),
            new APIVersion(1, 5),
            //2.x
            new APIVersion(2, 0),
            new APIVersion(2, 1),
            //3.x
            new APIVersion(3, 0),
            new APIVersion(3, 1),
            new APIVersion(3, 2),
            new APIVersion(3, 3),
            //4.x
            new APIVersion(4, 0),
            new APIVersion(4, 1),
            new APIVersion(4, 2),
            new APIVersion(4, 3),
            new APIVersion(4, 4),
            new APIVersion(4, 5),
            new APIVersion(4, 6),
        };
        
        private static readonly APIVersion[] OPEN_GLES_VERSIONS = new [] {
            //1.x
            // new APIVersion(1, 0),
            // new APIVersion(1, 1),
            //2.x
            new APIVersion(2, 0),
            //3.x
            new APIVersion(3, 0),
            new APIVersion(3, 1),
            new APIVersion(3, 2),
        };
        private static unsafe Window* _Window;

        public static unsafe (APIVersion gl, APIVersion gles) GetLatestSupported(bool testgl = true, bool testgles = true) {
            var sdl = Sdl.GetApi();

            _Window = CreateWindow(sdl);

            if (sdl.Init(Sdl.InitVideo) < 0)
                throw new Exception();

            if(testgl)
                GetLatestGLSupported(sdl);
            if(testgles)
                GetLatestGLESSupported(sdl);
            
            sdl.DestroyWindow(_Window);

            sdl.Quit();
            sdl.Dispose();

            return (_LastTested, _LastTestedES);
        }
        
        private static void GetLatestGLSupported(Sdl sdl) {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (APIVersion openGlVersion in OPEN_GL_VERSIONS) {
                if (!TestApiVersion(sdl, openGlVersion, ContextAPI.OpenGL))
                    return;
            }

        }

        private static void GetLatestGLESSupported(Sdl sdl) {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (APIVersion openGlesVersion in OPEN_GLES_VERSIONS) {
                if (!TestApiVersion(sdl, openGlesVersion, ContextAPI.OpenGLES))
                    return;
            }

        }
        
        private static unsafe bool TestApiVersion(Sdl sdl, APIVersion version, ContextAPI contextApi) {
            sdl.GLSetAttribute(GLattr.GLContextMajorVersion, version.MajorVersion);
            sdl.GLSetAttribute(GLattr.GLContextMinorVersion, version.MinorVersion);
            if (contextApi == ContextAPI.OpenGLES)
                sdl.GLSetAttribute(GLattr.GLContextProfileMask, (int)GLprofile.GLContextProfileES);
            else
                sdl.GLSetAttribute(GLattr.GLContextProfileMask, (int)GLprofile.GLContextProfileCore);
            
            var ctx = sdl.GLCreateContext(_Window);
            
            string err = sdl.GetErrorS();
            if (err.Length != 0) {
                sdl.DestroyWindow(_Window);
                return false;
            }
            if (contextApi == ContextAPI.OpenGLES)
                _LastTestedES = version;
            else
                _LastTested = version;
            sdl.GLDeleteContext(ctx);
         
            return true;
        }
    }
}
