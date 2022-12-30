using System;
using Silk.NET.Windowing;

namespace Furball.Vixie.Backends.Shared; 

public static class Global {
    public static Lazy<(APIVersion GL, APIVersion GLES)> LatestSupportedGl;
}