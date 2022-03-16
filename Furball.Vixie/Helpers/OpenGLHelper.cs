using System.Diagnostics;
using System.Threading;
using Silk.NET.OpenGLES;

namespace Furball.Vixie.Helpers {
    public static class OpenGLHelper {
        [Conditional("DEBUG")]
        public static void CheckError() {
            //TODO: do this
//            GLEnum error = Global.Gl.GetError();
//
//            if (error != GLEnum.NoError) {
//#if DEBUGWITHGL
//                throw new Exception($"Got GL Error {error}!");
//#else
//                Debugger.Break();
//#endif
            //}
        }

        private static Thread _MainThread;

        [Conditional("DEBUG")]
        public static void GetMainThread() {
           // _MainThread = Thread.CurrentThread;
        }

        [Conditional("DEBUG")]
        public static void CheckThread() {
            //if (Thread.CurrentThread != _MainThread) throw new ThreadStateException("You are calling GL on the wrong thread!");
        }
    }
}
