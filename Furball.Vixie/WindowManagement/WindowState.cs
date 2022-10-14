using System;

namespace Furball.Vixie.WindowManagement; 

[Flags]
public enum WindowState {
    Minimized,
    Maximized,
    Windowed,
    Fullscreen
}