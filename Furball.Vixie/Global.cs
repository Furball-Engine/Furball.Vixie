using Silk.NET.OpenGL;

namespace Furball.Vixie {
    internal static class Global {
        internal static bool                        AlreadyInitialized;
        internal static GL                          Gl;
        internal static Game                        GameInstance;
        internal static WindowManager               WindowManager;
        internal static GraphicsDevice Device;
    }
}
