using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Veldrid;
using Silk.NET.Input;
using Silk.NET.Input.Glfw;
using Silk.NET.Input.Sdl;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Extensions.Veldrid;
using Silk.NET.Windowing.Glfw;
using Silk.NET.Windowing.Sdl;

namespace Furball.Vixie; 

public class WindowManager : IDisposable {
    /// <summary>
    /// The Window's Creation Options
    /// </summary>
    private WindowOptions _windowOptions;
    public bool    Debug   => this._windowOptions.API.Flags.HasFlag(ContextFlags.Debug);
    public Backend Backend { get; internal set; }
    /// <summary>
    /// Actual Game Window
    /// </summary>
    internal IView GameView;
    internal IWindow GameWindow;
    
    /// <summary>
    /// Current Window State
    /// </summary>
    public WindowState WindowState { get; internal set; }

    public bool ViewOnly {
        get;
        private set;
    } = false;

    private Vector2 _windowSize; 
    public Vector2 WindowSize => this._windowSize;
    
    public bool Fullscreen {
        get {
            if (this.ViewOnly)
                throw new NotSupportedException("No fullscreen on view only platforms!");

            return this.GameWindow.WindowState == WindowState.Fullscreen;
        }
        set {
            if (this.ViewOnly)
                throw new NotSupportedException("No fullscreen on view only platforms!");

            this.GameWindow.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
        }
    }
        
    public IInputContext InputContext;

    /// <summary>
    /// Creates a Window Manager
    /// </summary>
    /// <param name="windowOptions">Window Creation Options</param>
    public WindowManager(WindowOptions windowOptions, Backend backend) {
        this.Backend        = backend;
        this._windowOptions = windowOptions;
    }

    public nint GetWindowHandle() => this.GameView.Handle;

    public IMonitor Monitor => this.ViewOnly ? null : this.GameWindow.Monitor;

    public void SetWindowSize(int width, int height) {
        if (ViewOnly)
            throw new NotSupportedException("You cant set window size on a view only platform!");
            
        this.GameWindow.Size = new Vector2D<int>(width, height);
        this.UpdateWindowSize(this.GameView.FramebufferSize.X, this.GameView.FramebufferSize.Y);
    }

    private double _targetUnfocusedFramerate  = 15;
    private double _targetUnfocusedUpdaterate = 30;
    
    public double TargetUnfocusedFramerate  {
        get => this._targetUnfocusedFramerate;
        set {
            this._targetUnfocusedFramerate = value;
            this.UpdateFramerates();
        }
    }
    public double TargetUnfocusedUpdaterate {
        get => this._targetUnfocusedUpdaterate;
        set {
            this._targetUnfocusedUpdaterate = value;
            this.UpdateFramerates();
        }
    }

    private double _targetFramerate;
    private double _targetUpdaterate;

    public double TargetFramerate {
        get => this._targetFramerate;
        set {
            this._targetFramerate = value;
            this.UpdateFramerates();
        }
    }

    public double TargetUpdaterate {
        get => this._targetUpdaterate;
        set {
            this._targetUpdaterate = value;
            this.UpdateFramerates();
        }
    }

    public bool EnableUnfocusCap = true;
    
    private void UpdateFramerates() {
        bool focus = this.Focused;
        
        if (!this.EnableUnfocusCap)
            focus = true;
        
        if(focus) {
            this.GameView.FramesPerSecond  = this._targetFramerate;
            this.GameView.UpdatesPerSecond = this._targetUpdaterate;
        } else {
            this.GameView.FramesPerSecond  = this.TargetUnfocusedFramerate;
            this.GameView.UpdatesPerSecond = this.TargetUnfocusedUpdaterate;
        }
    }

    public bool Focused {
        get;
        private set;
    } = true;
    
    private void GameViewOnFocusChanged(bool focus) {
        this.Focused = focus;
        this.UpdateFramerates();
    }

    public string WindowTitle {
        get {
            if (ViewOnly)
                throw new NotSupportedException("You cant set the window title on a view only platform!");

            return this.GameWindow.Title;
        }
        set {
            if (ViewOnly)
                throw new NotSupportedException("You cant set the window title on a view only platform!");

            this.GameWindow.Title = value;
        }
    }

    public void Close() {
        this.GameView.Close();
    }

    public event EventHandler<Vector2> OnFramebufferResize;

    private void UpdateWindowSize(int width, int height) {
        this._windowSize = new Vector2(width, height);
        this.OnFramebufferResize?.Invoke(this, this._windowSize);
    }

    internal bool RequestViewOnly = false;
        
    /// <summary>
    /// Creates the Window and grabs the OpenGL API of Window
    /// </summary>
    public void Create() {
        #region Silk platform registration (for NativeAOT/No Refection)
        SdlWindowing.RegisterPlatform();
        GlfwWindowing.RegisterPlatform();
        
        GlfwInput.RegisterPlatform();
        SdlInput.RegisterPlatform();
        #endregion
    
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && this.Backend == Backend.Direct3D11)
            SdlWindowing.Use();
        
        ContextAPI api = this.Backend switch {
            Backend.OpenGLES   => ContextAPI.OpenGLES,
            Backend.OpenGL     => ContextAPI.OpenGL,
            Backend.Veldrid    => ContextAPI.None,
            Backend.Vulkan     => ContextAPI.Vulkan,
            Backend.Direct3D11 => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? ContextAPI.Vulkan : ContextAPI.None,
            _                  => throw new ArgumentOutOfRangeException("backend", "Invalid API chosen...")
        };

        ContextProfile profile = this.Backend switch {
            Backend.OpenGLES   => ContextProfile.Core,
            Backend.OpenGL     => ContextProfile.Core,
            Backend.Veldrid    => ContextProfile.Core,
            Backend.Vulkan     => ContextProfile.Core,
            Backend.Direct3D11 => ContextProfile.Core,
            _                  => throw new ArgumentOutOfRangeException("backend", "Invalid API chosen...")
        };

        ContextFlags flags = this.Backend switch {
#if DEBUG
            Backend.OpenGLES   => ContextFlags.Debug,
            Backend.OpenGL     => ContextFlags.Debug,
            Backend.Veldrid    => ContextFlags.ForwardCompatible | ContextFlags.Debug,
            Backend.Vulkan     => ContextFlags.Debug,
            Backend.Direct3D11 => ContextFlags.Debug,
#else
            Backend.OpenGLES   => ContextFlags.Default,
            Backend.OpenGL     => ContextFlags.Default,
            Backend.Direct3D11 => ContextFlags.Default,
            Backend.Veldrid    => ContextFlags.ForwardCompatible,
            Backend.Vulkan     => ContextFlags.Default,
#endif
            _ => throw new ArgumentOutOfRangeException("backend", "Invalid API chosen...")
        };

        APIVersion version;
        switch (this.Backend) {
            case Backend.OpenGLES:
                if (Backends.Shared.Global.LatestSupportedGL.GLES.MajorVersion < 3) {
                    Backends.Shared.Global.LatestSupportedGL.GLES = new APIVersion(3, 0);
                    GraphicsBackend.IsOnUnsupportedPlatform       = true;//mark us as running on an unsupported configuration
                }

                version = Backends.Shared.Global.LatestSupportedGL.GLES;
                break;
            case Backend.OpenGL:
                if (Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion < 2) {
                    Backends.Shared.Global.LatestSupportedGL.GL = new APIVersion(2, 0);
                    GraphicsBackend.IsOnUnsupportedPlatform     = true;//mark us as running on an unsupported configuration
                }

                version = Backends.Shared.Global.LatestSupportedGL.GL;
                break;
            case Backend.Veldrid:
                version = new APIVersion(0, 0);
                break;
            case Backend.Direct3D11:
                version = new APIVersion(11, 0);
                break;
            case Backend.Vulkan:
                version = new APIVersion(0, 0);
                break;
            default:
                throw new ArgumentOutOfRangeException("backend", "Invalid API chosen...");
        }

        this._windowOptions.API = new GraphicsAPI(api, profile, flags, version);

        if (this.Backend == Backend.Veldrid) {
            this._windowOptions.API = VeldridBackend.PrefferedBackend.ToGraphicsAPI();

            this._windowOptions.ShouldSwapAutomatically = false;
        }

        if (this.RequestViewOnly) {
            this.GameView = Window.GetView(new ViewOptions(this._windowOptions));
            this.ViewOnly = true;
        }
        else
            this.GameView = this.GameWindow = Window.Create(this._windowOptions);

        if (this.Backend == Backend.Veldrid) {
            this.GameView.IsContextControlDisabled = true;
        }
            
        this.GameView.FramebufferResize += newSize => {
            this.UpdateWindowSize(newSize.X, newSize.Y);
        };

        this.GameView.Load += () => {
            this._windowSize = new(this.GameView.FramebufferSize.X, this.GameView.FramebufferSize.Y);
        };
            
        this.GameView.Closing += this.OnViewClosing;
        
        this.GameView.FocusChanged += GameViewOnFocusChanged;
    }

    public void SetupGraphicsApi() {
        GraphicsBackend.SetBackend(this.Backend);
        Global.GameInstance.SetApiFeatureLevels();
        GraphicsBackend.Current.SetMainThread();
        GraphicsBackend.Current.Initialize(this.GameView, this.InputContext);

        GraphicsBackend.Current.HandleFramebufferResize(this.GameView.FramebufferSize.X, this.GameView.FramebufferSize.Y);
    }
    /// <summary>
    /// Runs the Window
    /// </summary>
    public void RunWindow() {
        this.GameView.Run();
    }
        
    private void OnViewClosing() {
        this.Dispose();
    }
        
    /// <summary>
    /// Disposes the Window Manager
    /// </summary>
    public void Dispose() {
        GraphicsBackend.Current.Cleanup();
    }
}