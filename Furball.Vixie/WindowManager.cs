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
        public Backend Backend { get; internal set; }
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
            this.Backend       = backend;
            this._windowOptions = windowOptions;
        }

        public nint GetWindowHandle() => this.GameWindow.Handle;

        public IMonitor Monitor => this.GameWindow.Monitor;
        
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
                Backend.OpenGL41 => ContextFlags.Default,
                Backend.OpenGL20 => ContextFlags.Default,
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
            
            this.GameWindow = Window.Create(this._windowOptions);

            if (this.Backend == Backend.Veldrid) {
                this.GameWindow.IsContextControlDisabled = true;
            }
            
            this.GameWindow.FramebufferResize += newSize => {
                this.UpdateProjectionAndSize(newSize.X, newSize.Y);
            };
            
            this.GameWindow.Closing += OnWindowClosing;
        }
        public void SetupGraphicsApi() {
            GraphicsBackend.SetBackend(this.Backend);
            Global.GameInstance.SetApiFeatureLevels();
            GraphicsBackend.Current.SetMainThread();
            GraphicsBackend.Current.Initialize(this.GameWindow, this.InputContext);
            
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
