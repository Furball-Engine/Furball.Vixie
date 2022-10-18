using System;
using System.Diagnostics;
using Furball.Vixie.Backends.Dummy;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.WebGL;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Uno.Foundation;

namespace Furball.Vixie.WindowManagement; 

public class JSCanvasWindowManager : IWindowManager {
    private bool _running;
    
    public JSCanvasWindowManager(Backend backend) {
        if (backend != Backend.WebGL)
            throw new PlatformNotSupportedException("You must use WebGL when using a JS canvas!");
    }
    
    public void Dispose() {
        
    }
    
    public Backend Backend {
        get;
    }
    
    public WindowState WindowState {
        get;
        set;
    }
    
    public nint WindowHandle {
        get;
    }
    
    public IMonitor Monitor {
        get;
    }
    
    public Vector2D<int> WindowSize {
        get;
        set;
    }
    
    public Vector2D<int> FramebufferSize {
        get;
    }
    
    public Vector2D<int> WindowPosition {
        get;
        set;
    }
    
    public double TargetFramerate {
        get;
        set;
    }
    
    public double TargetUpdaterate {
        get;
        set;
    }
    
    public double TargetUnfocusedFramerate {
        get;
        set;
    }
    
    public double TargetUnfocusedUpdaterate {
        get;
        set;
    }
    
    public bool FramerateCap {
        get;
        set;
    }
    
    public bool UnfocusFramerateCap {
        get;
        set;
    }
    
    public bool Focused {
        get;
    }
    
    public void Focus() {
        throw new NotImplementedException();
    }
    
    public void SetIcon(Image<Rgba32> image) {
        throw new NotImplementedException();
    }
    
    public bool VSync {
        get;
        set;
    }
    
    public string WindowTitle {
        get;
        set;
    }
    
    public GraphicsBackend GraphicsBackend {
        get;
        private set;
    }

    public void CreateWindow() {
        WebAssemblyRuntime.InvokeJS(@"var canvas = document.createElement('canvas');
canvas.style.position = ""absolute"";
canvas.style.left       = ""0px"";
canvas.style.top        = ""0px"";
canvas.style.zIndex     = ""100"";
canvas.style.width      = ""1920"";
canvas.style.height     = ""1080"";
canvas.style.width = ""100%"";
canvas.style.height = ""100%"";
canvas.id = 'webgl-canvas';
document.body.appendChild(canvas);");
    }
    
    public void RunWindow() {
        this.CreateGraphicsDevice();
        
        this.WindowLoad?.Invoke();

        Stopwatch stopwatch = Stopwatch.StartNew();
        double deltaTime = 0;
        double lastTime = 0;
        while (this._running) {
            this.Update?.Invoke(deltaTime);
            this.Draw?.Invoke(deltaTime);
            
            deltaTime = (stopwatch.Elapsed.TotalSeconds - lastTime) * 1000;
            lastTime = stopwatch.Elapsed.TotalSeconds;
        }
        stopwatch.Stop();
        
        this.WindowClosing?.Invoke();
    }
    private void CreateGraphicsDevice() {
        this.GraphicsBackend = new WebGLGraphicsBackend();
        
        this.GraphicsBackend.SetMainThread();
        this.GraphicsBackend.Initialize(null!, null!);
    }

    public void CloseWindow() {
        this._running = false;
    }
    
    public bool TryForceUpdate() {
        return false;
    }
    
    public bool TryForceDraw() {
        return true;
    }
    
    public event Action                WindowLoad;
    public event Action                WindowClosing;
    public event Action<double>        Update;
    public event Action<double>        Draw;
    public event Action<bool>          FocusChanged;
    public event Action<Vector2D<int>> FramebufferResize;
    public event Action<WindowState>   StateChanged;
    public event Action<string[]>      FileDrop;
}