using System;

namespace Furball.Vixie.WindowManagement;

[Flags]
public enum WindowState {
    Minimized  = 1,
    Maximized  = 2,
    Windowed   = 4,
    Fullscreen = 8
}