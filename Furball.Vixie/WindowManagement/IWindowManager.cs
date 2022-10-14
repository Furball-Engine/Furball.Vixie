#nullable enable

using System;
using Furball.Vixie.Backends.Shared.Backends;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Furball.Vixie.WindowManagement;

public interface IWindowManager : IDisposable {
    public Backend Backend { get; }

    public WindowState WindowState { get; set; }

    public nint WindowHandle { get; }

    public IMonitor? Monitor { get; }

    public Vector2D<int> WindowSize      { get; set; }
    public Vector2D<int> FramebufferSize { get; }
    public Vector2D<int> WindowPosition  { get; set; }

    public double TargetFramerate  { get; set; }
    public double TargetUpdaterate { get; set; }

    public double TargetUnfocusedFramerate  { get; set; }
    public double TargetUnfocusedUpdaterate { get; set; }

    public bool FramerateCap        { get; set; }
    public bool UnfocusFramerateCap { get; set; }

    public bool Focused { get; }

    public void Focus();

    public bool VSync { get; set; }

    public string WindowTitle { get; set; }

    public GraphicsBackend GraphicsBackend { get; }

    public void CreateWindow();
    public void RunWindow();
    public void CloseWindow();

    /// <summary>
    ///     Try to force an update to happen immediately
    /// </summary>
    /// <returns>Whether the update was successful</returns>
    public bool TryForceUpdate();
    /// <summary>
    ///     Try to force a draw to happen immediately
    /// </summary>
    /// <returns>Whether the draw was successful</returns>
    public bool TryForceDraw();

    public event Action?                WindowLoad;
    public event Action?                WindowClosing;
    public event Action<double>?        Update;
    public event Action<double>?        Draw;
    public event Action<bool>?          FocusChanged;
    public event Action<Vector2D<int>>? FramebufferResize;
    public event Action<WindowState>?   StateChanged;
    public event Action<string[]>?      FileDrop;
}