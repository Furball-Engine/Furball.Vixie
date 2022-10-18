using System;
using Silk.NET.Maths;
// ReSharper disable UnusedMember.Global

namespace Furball.Vixie.WindowManagement.JSCanvas; 

internal static partial class Exports {
    internal static JSCanvasWindowManager WindowManager; 
    
    public static string WindowResize(int width, int height) {
        WindowManager.Resize(width, height);

        return "";
    }

    public static string Frame() {
        WindowManager.JsFrame();

        return "";
    }
}