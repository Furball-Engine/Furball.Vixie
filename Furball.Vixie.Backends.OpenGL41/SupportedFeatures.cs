using System.Collections.Generic;

namespace Furball.Vixie.Backends.OpenGL41 {
    internal static class SupportedFeatures {
        public static bool SupportsBindlessTexturing = false;
        public static bool IsArbBindlessTexturing    = false;

        public static List<string> Extensions = new();
    }
}
