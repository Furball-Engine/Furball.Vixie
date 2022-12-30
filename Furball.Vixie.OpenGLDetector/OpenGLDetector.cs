using System;
using Silk.NET.SDL;
using Silk.NET.Windowing;
using Window=Silk.NET.SDL.Window;

namespace Furball.Vixie.OpenGLDetector;

// ReSharper disable once InconsistentNaming
public static class OpenGLDetector {
    private static unsafe Window* CreateWindow(Sdl sdl) {
        var window = sdl.CreateWindow("", 0, 0, 1, 1, (uint)(WindowFlags.Hidden | WindowFlags.Opengl));

        return window;
    }

    private static APIVersion _lastTested   = new(0, 0);
    private static APIVersion _lastTestedEs = new(0, 0);

    private static readonly APIVersion[] KnownOpenGlVersions = {
        //1.x
        new(1, 0),
        new(1, 1),
        new(1, 2),
        new(1, 3),
        new(1, 4),
        new(1, 5),
        //2.x
        new(2, 0),
        new(2, 1),
        //3.x
        new(3, 0),
        new(3, 1),
        new(3, 2),
        new(3, 3),
        //4.x
        new(4, 0),
        new(4, 1),
        new(4, 2),
        new(4, 3),
        new(4, 4),
        new(4, 5),
        new(4, 6)
    };

    private static readonly APIVersion[] KnownOpenGlesVersions = {
        //1.x
        // new APIVersion(1, 0),
        // new APIVersion(1, 1),
        //2.x
        new(2, 0),
        //3.x
        new(3, 0),
        new(3, 1),
        new(3, 2)
    };

    private static unsafe Window* _window;

    public static unsafe (APIVersion GL, APIVersion GLES) GetLatestSupported(bool testGl = true, bool testGles = true) {
        var sdl = Sdl.GetApi();

        if (sdl.Init(Sdl.InitVideo) < 0)
            throw new Exception("Unable to init video");

        try {
            _window = CreateWindow(sdl);

            if (testGl)
                GetLatestGlSupported(sdl);
            if (testGles)
                GetLatestGlesSupported(sdl);

            sdl.DestroyWindow(_window);
        }
        catch {
            sdl.Quit();
            sdl.Dispose();

            throw;
        }

        sdl.Quit();
        sdl.Dispose();

        return (_lastTested, _lastTestedEs);
    }

    private static void GetLatestGlSupported(Sdl sdl) {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (APIVersion openGlVersion in KnownOpenGlVersions) {
            if (!TestApiVersion(sdl, openGlVersion, ContextAPI.OpenGL) && _lastTested.MajorVersion != 0)
                return;
        }

    }

    private static void GetLatestGlesSupported(Sdl sdl) {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (APIVersion openGlesVersion in KnownOpenGlesVersions) {
            if (!TestApiVersion(sdl, openGlesVersion, ContextAPI.OpenGLES) && _lastTestedEs.MajorVersion != 0)
                return;
        }

    }

    private static unsafe bool TestApiVersion(Sdl sdl, APIVersion version, ContextAPI contextApi) {
        sdl.GLSetAttribute(GLattr.ContextMajorVersion, version.MajorVersion);
        sdl.GLSetAttribute(GLattr.ContextMinorVersion, version.MinorVersion);
        if (contextApi == ContextAPI.OpenGLES)
            sdl.GLSetAttribute(GLattr.ContextProfileMask, (int)GLprofile.ES);
        else
            sdl.GLSetAttribute(GLattr.ContextProfileMask, (int)GLprofile.Core);

        var ctx = sdl.GLCreateContext(_window);

        string err = sdl.GetErrorS();
        if (err.Length != 0) {
            sdl.ClearError();
            if (_window != null) {
                sdl.DestroyWindow(_window);
                if (err.Contains("GLXBadFBConfig")) {
                    sdl.Quit();
                    sdl.Init(Sdl.InitVideo);
                }

                _window = null;
            }

            _window = CreateWindow(sdl);
            if (_window == null)
                return false;

            return false;
        }
        if (contextApi == ContextAPI.OpenGLES)
            _lastTestedEs = version;
        else
            _lastTested = version;
        sdl.GLDeleteContext(ctx);

        return true;
    }
}