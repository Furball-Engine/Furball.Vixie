using System;
using System.Collections.Generic;
using Silk.NET.Windowing;

namespace Furball.Vixie.Backends.Shared; 

public static class Global {
    public static (APIVersion GL, APIVersion GLES) LatestSupportedGL;
}