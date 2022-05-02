using System;
using System.Numerics;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Veldrid;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Extensions.Veldrid;
using Silk.NET.Windowing.Sdl;

namespace Furball.Vixie {
    public class WindowManager : IDisposable {
        /// <summary>
        /// The Window's Creation Options
        /// </summary>
        private WindowOptions _windowOptions;
        private Backend _backend;
        /// <summary>
        /// Actual Game Window
        /// </summary>
        internal IWindow GameWindow;
        /// <summary>
        /// Current Window State
        /// </summary>
        public WindowState WindowState { get; internal set; }

        public Vector2 WindowSize { get; private set; }
        public bool Fullscreen {
            get => this.GameWindow.WindowState == WindowState.Fullscreen;
            set => this.GameWindow.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
        }
        
        public IInputContext InputContext;

        /// <summary>
        /// Creates a Window Manager
        /// </summary>
        /// <param name="windowOptions">Window Creation Options</param>
        public WindowManager(WindowOptions windowOptions, Backend backend) {
            this._backend       = backend;
            this._windowOptions = windowOptions;
        }

        public nint GetWindowHandle() => this.GameWindow.Handle;

        public void SetWindowSize(int width, int height) {
            this.GameWindow.Size = new Vector2D<int>(width, height);
            
            this.UpdateProjectionAndSize(width, height);
        }

        public void SetTargetFramerate(int framerate) {
            this.GameWindow.FramesPerSecond = framerate;
        }

        public int GetTargetFramerate() {
            return (int)this.GameWindow.FramesPerSecond;
        }
        
        public void SetWindowTitle(string title) {
            this.GameWindow.Title = title;
        }

        public void Close() {
            this.GameWindow.Close();
        }

        private void UpdateProjectionAndSize(int width, int height) {
            this.WindowSize       = new Vector2(width, height);

            GraphicsBackend.Current.HandleWindowSizeChange(width, height);
        }

        /// <summary>
        /// Creates the Window and grabs the OpenGL API of Window
        /// </summary>
        public void Create() {
            SdlWindowing.Use(); //dont tell perskey and kai that i do this! shhhhhhhhhhhhhhh

            ContextAPI api = this._backend switch {
                Backend.OpenGLES32 => ContextAPI.OpenGLES,
                Backend.OpenGLES30 => ContextAPI.OpenGLES,
                Backend.OpenGL20   => ContextAPI.OpenGL,
                Backend.OpenGL41   => ContextAPI.OpenGL,
                Backend.Veldrid    => ContextAPI.None,
                Backend.Direct3D11 => ContextAPI.None,
                _                  => throw new ArgumentOutOfRangeException("backend", "Invalid API chosen...")
            };

            ContextProfile profile = this._backend switch {
                Backend.OpenGLES30 => ContextProfile.Core,
                Backend.OpenGLES32 => ContextProfile.Core,
                Backend.OpenGL20   => ContextProfile.Core,
                Backend.OpenGL41   => ContextProfile.Core,
                Backend.Veldrid    => ContextProfile.Core,
                Backend.Direct3D11 => ContextProfile.Core,
                _                  => throw new ArgumentOutOfRangeException("backend", "Invalid API chosen...")
            };

            ContextFlags flags = this._backend switch {
#if DEBUG
                Backend.OpenGLES30 => ContextFlags.Debug,
                Backend.OpenGLES32 => ContextFlags.Debug,
                Backend.OpenGL41   => ContextFlags.Debug,
                Backend.OpenGL20   => ContextFlags.Debug,
                Backend.Veldrid    => ContextFlags.ForwardCompatible,
                Backend.Direct3D11 => ContextFlags.Debug,
#else
                Backend.OpenGLES30 => ContextFlags.Default,
                Backend.OpenGLES32 => ContextFlags.Default,
                Backend.OpenGL41 => ContextFlags.Default,
                Backend.OpenGL20 => ContextFlags.Default,
                Backend.Direct3D11 => ContextFlags.Default,
                Backend.Veldrid  => ContextFlags.ForwardCompatible | ContextFlags.Debug,
#endif
                _ => throw new ArgumentOutOfRangeException("backend", "Invalid API chosen...")
            };

            APIVersion version = this._backend switch {
                Backend.OpenGLES30 => new APIVersion(3, 0),
                Backend.OpenGLES32 => new APIVersion(3, 2),
                Backend.OpenGL20   => new APIVersion(2, 0),
                Backend.OpenGL41   => new APIVersion(4, Global.SupportedVersions.OpenGL.MinorVersion),
                Backend.Veldrid    => new APIVersion(0,  0),
                Backend.Direct3D11 => new APIVersion(11, 0),
                _                  => throw new ArgumentOutOfRangeException("backend", "Invalid API chosen...")
            };

            this._windowOptions.API = new GraphicsAPI(api, profile, flags, version);

            if (this._backend == Backend.Veldrid) {
                this._windowOptions.API = VeldridBackend.PrefferedBackend.ToGraphicsAPI();

                this._windowOptions.ShouldSwapAutomatically = false;
            }
            
            this.GameWindow = Window.Create(this._windowOptions);

            if (this._backend == Backend.Veldrid) {
                this.GameWindow.IsContextControlDisabled = true;
            }
            
            this.GameWindow.FramebufferResize += newSize => {
                this.UpdateProjectionAndSize(newSize.X, newSize.Y);
            };
            
            this.GameWindow.Closing += OnWindowClosing;
        }
        public void SetupGraphicsApi() {
            GraphicsBackend.SetBackend(this._backend);
            GraphicsBackend.Current.Initialize(this.GameWindow, this.InputContext, Global.SupportedVersions);

            this.UpdateProjectionAndSize(this._windowOptions.Size.X, this._windowOptions.Size.Y);
        }
        /// <summary>
        /// Runs the Window
        /// </summary>
        public void RunWindow() {
            this.GameWindow.Run();
        }
        
        private void OnWindowClosing() {
            this.Dispose();
        }
        
        /// <summary>
        /// Disposes the Window Manager
        /// </summary>
        public void Dispose() {
            GraphicsBackend.Current.Cleanup();
        }
    }
}
