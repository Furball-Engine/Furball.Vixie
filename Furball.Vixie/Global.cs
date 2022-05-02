using Furball.Vixie.Backends.Shared.Backends;

namespace Furball.Vixie {
    internal static class Global {
        internal static bool                    AlreadyInitialized;
        internal static Game                    GameInstance;
        internal static WindowManager           WindowManager;
        internal static SysetmSupportedVersions SupportedVersions;
    }
}
