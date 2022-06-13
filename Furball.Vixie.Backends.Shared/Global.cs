using System;
using System.Collections.Generic;
using Furball.Vixie.Backends.Shared.Backends;
using Silk.NET.Windowing;

namespace Furball.Vixie.Backends.Shared {
    public static class Global {
        public static (APIVersion GL, APIVersion GLES) LatestSupportedGL;
    }
}
