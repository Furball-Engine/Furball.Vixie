using System;
using System.Diagnostics;
using Furball.Vixie.Backends.Dummy;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.WebGL;
using Furball.Vixie.WindowManagement.JSCanvas;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Uno.Foundation;

namespace Furball.Vixie.WindowManagement; 

public class JSCanvasWindowManager : IWindowManager {
    private  bool          _running;
    internal Vector2D<int> _framebufferSize;
    private  Stopwatch     _stopwatch;

    public JSCanvasWindowManager(Backend backend) {
        if (backend != Backend.WebGL)
            throw new PlatformNotSupportedException("You must use WebGL when using a JS canvas!");

        Exports.WindowManager = this;
    }
    
    public void Dispose() {
        
    }
    
    public Backend Backend {
        get => Backend.WebGL;
    }
    
    public WindowState WindowState {
        get => WindowState.Windowed; //TODO implement this
        set {}
    }
    
    public nint WindowHandle => 0;

    public IMonitor Monitor => null;
    
    public Vector2D<int> WindowSize {
        get => this._framebufferSize;
        set {}
    }
    
    public Vector2D<int> FramebufferSize {
        get => this._framebufferSize;
    }
    
    public Vector2D<int> WindowPosition {
        get => Vector2D<int>.Zero;
        set {}
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
    
    public bool Focused => bool.Parse(WebAssemblyRuntime.InvokeJS("document.hasFocus()"));

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
        get => WebAssemblyRuntime.InvokeJS("return window.document.title");
        set {
            string escapedTitle = WebAssemblyRuntime.EscapeJs(value);
            
            WebAssemblyRuntime.InvokeJS($"window.document.title = \"{escapedTitle}\"");
        }
    }
    
    public GraphicsBackend GraphicsBackend {
        get;
        private set;
    }

    public void CreateWindow() {
        WebAssemblyRuntime.InvokeJS(@"canvas = document.createElement('canvas');
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

    private void HookCanvasEvents() {
        WebAssemblyRuntime.InvokeJS(@"
window.addEventListener('resize', function() {
    var canvas = document.getElementById('webgl-canvas');

    var resizeWindow = Module.mono_bind_static_method(""[Furball.Vixie] Furball.Vixie.WindowManagement.JSCanvas.Exports:WindowResize"");
    var result = resizeWindow(canvas.clientWidth, canvas.clientHeight);
});

//Request animation frame
window.requestAnimationFrame = (function() {
    return window.requestAnimationFrame ||
        window.webkitRequestAnimationFrame ||
        window.mozRequestAnimationFrame ||
        window.oRequestAnimationFrame ||
        window.msRequestAnimationFrame ||
        function(callback) {
            window.setTimeout(callback, 1000 / 60);
        };
})();

running = true;

//Infinite requestAnimationFrame
(function animLoop() {
    if(!running) return;

    Module.mono_bind_static_method(""[Furball.Vixie] Furball.Vixie.WindowManagement.JSCanvas.Exports:Frame"")();
    requestAnimationFrame(animLoop);
})();
");
    }
    
    
    
    public void RunWindow() {
        this._stopwatch = Stopwatch.StartNew();
        
        Exports.WindowResize(1920, 1080);
        
        this.CreateGraphicsDevice();
        this.HookCanvasEvents();
        
        this.WindowLoad?.Invoke();

        this._running = true;
        Exports.Frame();
    }
    private void CreateGraphicsDevice() {
        this.GraphicsBackend = new WebGLGraphicsBackend();
        
        this.GraphicsBackend.SetMainThread();
        this.GraphicsBackend.Initialize(null!, null!);
    }

    public void CloseWindow() {
        this._running = false;
        
        //Stop the frame loop
        WebAssemblyRuntime.InvokeJS("running = false;");
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

    private double _deltaTime;
    private double _lastTime;
    public void JsFrame() {
        this.Update?.Invoke(this._deltaTime);
        this.Draw?.Invoke(this._deltaTime);

        this._deltaTime = this._stopwatch.Elapsed.TotalMilliseconds - this._lastTime;
        this._lastTime  = this._stopwatch.Elapsed.TotalMilliseconds;
    }
    
    internal void Resize(int width, int height) {
        this._framebufferSize = new Vector2D<int>(width, height);
        
        this.GraphicsBackend?.HandleFramebufferResize(width, height);
    }
}