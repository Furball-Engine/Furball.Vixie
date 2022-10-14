﻿#nullable enable
using System;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Direct3D11;
using Furball.Vixie.Backends.Dummy;
using Furball.Vixie.Backends.Mola;
using Furball.Vixie.Backends.OpenGL;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Veldrid;
using Furball.Vixie.Backends.Vulkan;
using Furball.Vixie.Helpers;
using Silk.NET.Input;
using Silk.NET.Input.Glfw;
using Silk.NET.Input.Sdl;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Extensions.Veldrid;
using Silk.NET.Windowing.Glfw;
using Silk.NET.Windowing.Sdl;

namespace Furball.Vixie.WindowManagement;

public class SilkWindowManager : IWindowManager {
    //Input context is public so that the engine can use it to get input
    public  IInputContext InputContext;
    
    private IWindow       _window;
    private WindowState   _windowState;

    public SilkWindowManager(Backend backend) {
        this.Backend = backend;

        //Register the windowing libraries
        SdlWindowing.RegisterPlatform();
        GlfwWindowing.RegisterPlatform();

        //Register the input libraries
        SdlInput.RegisterPlatform();
        GlfwInput.RegisterPlatform();

        //Prioritize GLFW over SDL
        Window.PrioritizeGlfw();
    }

    private GraphicsAPI GetSilkGraphicsApi() {
        ContextAPI api = this.Backend switch {
            Backend.OpenGLES => ContextAPI.OpenGLES,
            Backend.OpenGL => ContextAPI.OpenGL,
            Backend.Veldrid => ContextAPI.None, //Veldrid handles this internally
            Backend.Vulkan => ContextAPI.Vulkan,
            Backend.Direct3D11 => RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? ContextAPI.Vulkan //If we are on linux and using d3d11, we likely want DXVK-native, so we use vulkan
                : ContextAPI.None, //If we are on windows, we specify none
            Backend.Dummy => ContextAPI.None, //Dummy backend is just a dummy, so we specify none
            Backend.Mola => ContextAPI
               .None, //Mola is software rendering, and we dont currently output anything to the window
            _ => throw new Exception("Invalid API chosen...")
        };

        ContextProfile profile = this.Backend switch {
            Backend.OpenGLES   => ContextProfile.Core, //OpenGLES is always core
            Backend.OpenGL     => ContextProfile.Core, //OpenGL is always core
            Backend.Veldrid    => ContextProfile.Core, //This doesnt matter for Veldrid
            Backend.Vulkan     => ContextProfile.Core, //This doesnt matter for Vulkan
            Backend.Direct3D11 => ContextProfile.Core, //This doesnt matter for D3D11
            Backend.Dummy      => ContextProfile.Core, //This doesnt matter for Dummy
            Backend.Mola       => ContextProfile.Core, //This doesnt matter for Mola, as it is software rendering
            _                  => throw new Exception("Invalid API chosen...")
        };

        const bool debug
#if DEBUG
            = true;
#else
            false;
#endif

        ContextFlags flags = this.Backend switch {
            // ReSharper disable HeuristicUnreachableCode
            Backend.Direct3D11 => debug ? ContextFlags.Debug : ContextFlags.Default,
            Backend.OpenGL     => debug ? ContextFlags.Debug : ContextFlags.Default,
            Backend.OpenGLES   => debug ? ContextFlags.Debug : ContextFlags.Default,
            Backend.Veldrid => debug ? ContextFlags.Debug | ContextFlags.ForwardCompatible
                : ContextFlags.Default                    | ContextFlags.ForwardCompatible,
            Backend.Vulkan => debug ? ContextFlags.Debug : ContextFlags.Default,
            Backend.Mola   => debug ? ContextFlags.Debug : ContextFlags.Default,
            Backend.Dummy  => debug ? ContextFlags.Debug : ContextFlags.Default,
            // ReSharper restore HeuristicUnreachableCode
            _ => throw new Exception("Invalid API chosen...")
        };

        APIVersion version;
        switch (this.Backend) {
            case Backend.OpenGLES:
                //If the user's GPU doesnt support GLES 3.0, force 3.0 and mark we are in unsupported mode
                if (Backends.Shared.Global.LatestSupportedGl.GLES.MajorVersion < 3) {
                    Backends.Shared.Global.LatestSupportedGl.GLES = new APIVersion(3, 0);
                    Vixie.GraphicsBackendState.IsOnUnsupportedPlatform = true; //mark us as running on an unsupported 
                }

                version = Backends.Shared.Global.LatestSupportedGl.GLES;
                break;
            case Backend.OpenGL:
                //If the user's GPU doesnt support GL 2.0, force 2.0 and mark we are in unsupported mode
                if (Backends.Shared.Global.LatestSupportedGl.GL.MajorVersion < 2) {
                    Backends.Shared.Global.LatestSupportedGl.GL = new APIVersion(2, 0);
                    Vixie.GraphicsBackendState.IsOnUnsupportedPlatform =
                        true; //mark us as running on an unsupported configuration
                }

                version = Backends.Shared.Global.LatestSupportedGl.GL;
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
            case Backend.Dummy:
                version = new APIVersion(0, 0);
                break;
            case Backend.Mola:
                version = new APIVersion(0, 0);
                break;
            default:
                throw new Exception("Invalid API chosen...");
        }

        return new GraphicsAPI(api, profile, flags, version);
    }

    public Backend Backend {
        get;
    }

    public WindowState WindowState {
        get => this._windowState;
        set {
            switch (value) {
                case WindowState.Minimized:
                    this._window.WindowState = Silk.NET.Windowing.WindowState.Minimized;
                    break;
                case WindowState.Maximized:
                    this._window.WindowState = Silk.NET.Windowing.WindowState.Maximized;
                    break;
                case WindowState.Windowed:
                    this._window.WindowState = Silk.NET.Windowing.WindowState.Normal;
                    break;
                case WindowState.Fullscreen:
                    this._window.WindowState = Silk.NET.Windowing.WindowState.Fullscreen;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof (value), value, null);
            }
        }
    }

    public nint WindowHandle => this._window.Handle;

    public IMonitor? Monitor => this._window.Monitor;

    public Vector2D<int> WindowSize {
        get => this._window.Size;
        set => this._window.Size = value;
    }
    public Vector2D<int> FramebufferSize => this._window.FramebufferSize;

    public Vector2D<int> WindowPosition {
        get => this._window.Position;
        set => this._window.Position = value;
    }

    private double _targetFramerate = 60.0;
    public double TargetFramerate {
        get => this._targetFramerate;
        set {
            this._targetFramerate = value;

            this.UpdateFpsCapState();
        }
    }

    private double _targetUpdateRate = 60.0;
    public double TargetUpdaterate {
        get => this._targetUpdateRate;
        set {
            this._targetUpdateRate = value;

            this.UpdateFpsCapState();
        }
    }

    private double _targetUnfocusedFramerate = 10.0;
    public double TargetUnfocusedFramerate {
        get => this._targetUnfocusedFramerate;
        set {
            this._targetUnfocusedFramerate = value;

            this.UpdateFpsCapState();
        }
    }

    private double _targetUnfocusedUpdateRate = 10.0;
    public double TargetUnfocusedUpdaterate {
        get => this._targetUnfocusedUpdateRate;
        set {
            this._targetUnfocusedUpdateRate = value;

            this.UpdateFpsCapState();
        }
    }

    private void UpdateFpsCapState() {
        if (this.Focused) {
            if (this.FramerateCap) {
                this._window.FramesPerSecond  = this.TargetFramerate;
                this._window.UpdatesPerSecond = this.TargetUpdaterate;
            }
            else {
                this._window.FramesPerSecond  = 0;
                this._window.UpdatesPerSecond = 0;
            }

            this._window.VSync = this._vsync;
        }
        else {
            if(this.UnfocusFramerateCap) {
                this._window.FramesPerSecond = this.TargetUnfocusedFramerate;
                this._window.UpdatesPerSecond  = this.TargetUnfocusedUpdaterate;
            }
            else {
                this._window.FramesPerSecond  = 0;
                this._window.UpdatesPerSecond = 0;
            }

            this._window.VSync = false;
        }
    }

    private bool _framerateCap;
    public bool FramerateCap {
        get => this._framerateCap;
        set {
            this._framerateCap = value;

            this.UpdateFpsCapState();
        }
    }

    private bool _unfocusFramerateCap = true;
    public bool UnfocusFramerateCap {
        get => this._unfocusFramerateCap;
        set {
            this._unfocusFramerateCap = value;

            this.UpdateFpsCapState();
        }
    }

    public bool Focused {
        get;
        private set;
    }

    public void Focus() {
        throw new NotImplementedException("This is not implemented in Silk.NET!");
    }

    //We have a backing field for this, as we need to set the VSync value on the window independently of the value the user chooses
    private bool _vsync;
    public bool VSync {
        get => this._vsync;
        set {
            this._vsync = value;

            this.UpdateFpsCapState();
        }
    }

    public string WindowTitle {
        get => this._window.Title;
        set => this._window.Title = value;
    }


    public GraphicsBackend GraphicsBackend {
        get;
        private set;
    }

    public void CreateWindow() {
        WindowOptions options = WindowOptions.Default;

        options.API = this.GetSilkGraphicsApi();

        //Disable vsync by default
        options.VSync = false;

        //Veldrid specific hacks
        if (this.Backend == Backend.Veldrid) {
            options.API = VeldridBackend.PrefferedBackend.ToGraphicsAPI();

            //Veldrid handles swapping
            options.ShouldSwapAutomatically = false;
            //Veldrid handles the context, so we dont want silk managing it
            options.IsContextControlDisabled = true;
        }

        //Explicitly request 0 depth bits, as we dont use depth testing
        options.PreferredDepthBufferBits = 0;

        //Specify 4 samples for multisampling
        options.Samples = 4;

        try {
            this._window = Window.Create(options);
        }
        catch {
            throw new WindowCreationFailedException();
        }

        //Hook to the window's events
        this.HookEvents();
    }
    private void HookEvents() {
        this._window.FramebufferResize += newSize => {
            this.FramebufferResize?.Invoke(newSize);

            this.GraphicsBackend.HandleFramebufferResize(newSize.X, newSize.Y);
        };

        this._window.FocusChanged += b => {
            this.Focused = b;
        };

        this._window.StateChanged += state => {
            this._windowState = state switch {
                Silk.NET.Windowing.WindowState.Fullscreen => WindowState.Fullscreen,
                Silk.NET.Windowing.WindowState.Maximized  => WindowState.Maximized,
                Silk.NET.Windowing.WindowState.Minimized  => WindowState.Minimized,
                Silk.NET.Windowing.WindowState.Normal     => WindowState.Windowed,
                _                                         => this.WindowState
            };
        };

        this._window.FileDrop += paths => {
            this.FileDrop?.Invoke(paths);
        };

        this._window.Load              += this.SilkWindowLoad;
        this._window.Closing           += this.SilkWindowClosing;
        this._window.Update            += this.SilkWindowUpdate;
        this._window.Render            += this.SilkWindowRender;
        this._window.FocusChanged      += this.SilkWindowFocusChange;
        this._window.FramebufferResize += this.SilkWindowFramebufferResize;
    }

    private void SilkWindowFramebufferResize(Vector2D<int> obj) {
        this.FramebufferResize?.Invoke(obj);
    }

    private void SilkWindowFocusChange(bool obj) {
        this.Focused = obj;

        this.UpdateFpsCapState();
        
        this.FocusChanged?.Invoke(obj);
    }

    private void SilkWindowRender(double obj) {
        this.Draw?.Invoke(obj);
    }

    private void SilkWindowUpdate(double obj) {
        this.Update?.Invoke(obj);
    }

    private void SilkWindowClosing() {
        this.WindowClosing?.Invoke();

        this.GraphicsBackend.Cleanup();
    }

    private void SilkWindowLoad() {
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        this.GraphicsBackend = this.Backend switch {
            Backend.OpenGLES   => new OpenGLBackend(this.Backend),
            Backend.Direct3D11 => new Direct3D11Backend(),
            Backend.OpenGL     => new OpenGLBackend(this.Backend),
            Backend.Veldrid    => new VeldridBackend(),
            Backend.Vulkan     => new VulkanBackend(),
            Backend.Dummy      => new DummyBackend(),
            Backend.Mola       => new MolaBackend(),
            _                  => throw new Exception("Invalid Backend Selected...")
        };

        this.InputContext = this._window.CreateInput();

        //Set the main thread of the graphics backend to the current thread
        this.GraphicsBackend.SetMainThread(); 
        
        //Initialize the backend
        this.GraphicsBackend.Initialize(this._window, this.InputContext);
        
        //Immediately notify the backend of a framebuffer resize, so it knows incase the window is already resized
        this.GraphicsBackend.HandleFramebufferResize(this.FramebufferSize.X, this.FramebufferSize.Y);

        //Update the fps cap state, as the window will have bogus values on creation
        this.UpdateFpsCapState();
        
        this.WindowLoad?.Invoke();
    }

    public void RunWindow() {
        Guard.EnsureNonNull(this._window);

        this._window.Run();
    }

    public void CloseWindow() {
        this._window.Close();
    }

    public bool TryForceUpdate() {
        this._window.DoUpdate();

        return true;
    }

    public bool TryForceDraw() {
        this._window.DoRender();

        return true;
    }

    public event Action?                WindowLoad;
    public event Action?                WindowClosing;
    public event Action<double>?        Update;
    public event Action<double>?        Draw;
    public event Action<bool>?          FocusChanged;
    public event Action<Vector2D<int>>? FramebufferResize;
    public event Action<WindowState>?   StateChanged;
    public event Action<string[]>?      FileDrop;

    public void Dispose() {
        this._window.Dispose();
    }
}