using Silk.NET.OpenGL;

namespace Furball.Vixie {
    public static class Global {
        public static bool                        AlreadyInitialized;
        public static GL                          Gl;
        public static Game                        GameInstance;
        public static WindowManager               WindowManager;
        public static GraphicsDeviceCaptabilities DeviceCaptabilities;
    }
}
