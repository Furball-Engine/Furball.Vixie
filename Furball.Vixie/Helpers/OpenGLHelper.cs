using System;
using System.Diagnostics;
using Silk.NET.OpenGLES;

namespace Furball.Vixie.Helpers {
    public static class OpenGLHelper {
        [Conditional("DEBUG")]
        public static void CheckError() {
            GLEnum error = Global.Gl.GetError();

            if (error != GLEnum.NoError) {
#if DEBUGWITHGL
                throw new Exception($"Got GL Error {error}!");
#else
                Debugger.Break();
#endif
            }
        }
    }
}
