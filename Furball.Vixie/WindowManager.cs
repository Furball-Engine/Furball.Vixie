using System;
using System.Numerics;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Veldrid;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Extensions.Veldrid;

namespace Furball.Vixie {
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

        public bool ViewOnly { get; internal set; } = false;

        public Vector2 WindowSize { get; private set; }
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
            this.Backend       = backend;
            this._windowOptions = windowOptions;
        }

        public nint GetWindowHandle() => this.GameView.Handle;

        public IMonitor Monitor => this.ViewOnly ? null : this.GameWindow.Monitor;

        public void SetWindowSize(int width, int height) {
            if (ViewOnly)
                throw new NotSupportedException("You cant set window size on a view only platform!");
            
            this.GameWindow.Size = new Vector2D<int>(width, height);
            this.UpdateProjectionAndSize(width, height);
        }

        public void SetTargetFramerate(int framerate) {
            this.GameView.FramesPerSecond = framerate;
        }

        public int GetTargetFramerate() {
            return (int)this.GameView.FramesPerSecond;
        }
        
        public void SetWindowTitle(string title) {
            if (ViewOnly)
                throw new NotSupportedException("You cant set the window title on a view only platform!");
            
            this.GameWindow.Title = title;
        }

        public void Close() {
            this.GameView.Close();
        }

        private void UpdateProjectionAndSize(int width, int height) {
            this.WindowSize = new Vector2(width, height);
        }

        internal bool RequestViewOnly = false;
        
        /// <summary>
        /// Creates the Window and grabs the OpenGL API of Window
        /// </summary>
        public void Create() {
            ContextAPI api = this.Backend switch {
                Backend.OpenGLES     => ContextAPI.OpenGLES,
                Backend.LegacyOpenGL => ContextAPI.OpenGL,
                Backend.ModernOpenGL => ContextAPI.OpenGL,
                Backend.Veldrid      => ContextAPI.None,
                Backend.Vulkan       => ContextAPI.Vulkan,
                Backend.Direct3D11   => ContextAPI.None,
                _                    => throw new ArgumentOutOfRangeException("backend", "Invalid API chosen...")
            };

            ContextProfile profile = this.Backend switch {
                Backend.OpenGLES     => ContextProfile.Core,
                Backend.LegacyOpenGL => ContextProfile.Core,
                Backend.ModernOpenGL => ContextProfile.Core,
                Backend.Veldrid      => ContextProfile.Core,
                Backend.Vulkan       => ContextProfile.Core,
                Backend.Direct3D11   => ContextProfile.Core,
                _                    => throw new ArgumentOutOfRangeException("backend", "Invalid API chosen...")
            };

            ContextFlags flags = this.Backend switch {
#if DEBUG
                Backend.OpenGLES     => ContextFlags.Debug,
                Backend.ModernOpenGL => ContextFlags.Debug,
                Backend.LegacyOpenGL => ContextFlags.Debug,
                Backend.Veldrid      => ContextFlags.ForwardCompatible | ContextFlags.Debug,
                Backend.Vulkan       => ContextFlags.Debug,
                Backend.Direct3D11   => ContextFlags.Debug,
#else
                Backend.OpenGLES => ContextFlags.Default,
                Backend.ModernOpenGL => ContextFlags.Default,
                Backend.LegacyOpenGL => ContextFlags.Default,
                Backend.Direct3D11 => ContextFlags.Default,
                Backend.Veldrid  => ContextFlags.ForwardCompatible,
                Backend.Vulkan   => ContextFlags.Default,
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
                case Backend.LegacyOpenGL:
                    if (Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion > 3 || Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion == 3 && Backends.Shared.Global.LatestSupportedGL.GL.MinorVersion > 0) {
                        Backends.Shared.Global.LatestSupportedGL.GL = new APIVersion(3, 0);
                        GraphicsBackend.IsOnUnsupportedPlatform     = true;//mark us as running on an unsupported configuration
                    }

                    version = Backends.Shared.Global.LatestSupportedGL.GL;
                    break;
                case Backend.ModernOpenGL:
                    if (Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion < 3 || Backends.Shared.Global.LatestSupportedGL.GL.MajorVersion == 3 && Backends.Shared.Global.LatestSupportedGL.GL.MinorVersion < 2) {
                        Backends.Shared.Global.LatestSupportedGL.GL = new APIVersion(3, 2);
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
                    version = new APIVersion(1, 1);
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
                this.UpdateProjectionAndSize(newSize.X, newSize.Y);
            };
            
            this.GameView.Closing += this.OnViewClosing;
        }
        public void SetupGraphicsApi() {
            GraphicsBackend.SetBackend(this.Backend);
            Global.GameInstance.SetApiFeatureLevels();
            GraphicsBackend.Current.SetMainThread();
            GraphicsBackend.Current.Initialize(this.GameView, this.InputContext);

            GraphicsBackend.Current.HandleFramebufferResize(this.GameView.Size.X, this.GameView.Size.Y);
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
}
