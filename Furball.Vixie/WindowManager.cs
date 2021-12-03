using System;
using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;
using Window=Silk.NET.Windowing.Window;

namespace Furball.Vixie {
    public class WindowManager : IDisposable {
        /// <summary>
        /// The Window's Creation Options
        /// </summary>
        private WindowOptions _windowOptions;
        /// <summary>
        /// Actual Game Window
        /// </summary>
        internal IWindow GameWindow;
        /// <summary>
        /// OpenGL API of Window
        /// </summary>
        private GL _glApi;
        /// <summary>
        /// Current Window State
        /// </summary>
        public WindowState WindowState { get; internal set; }
        /// <summary>
        /// The Window's current Projection Matrix
        /// </summary>
        public Matrix4x4 ProjectionMatrix { get; private set; }

        public Vector2 WindowSize { get; private set; }
        /// <summary>
        /// Creates a Window Manager
        /// </summary>
        /// <param name="windowOptions">Window Creation Options</param>
        public WindowManager(WindowOptions windowOptions) {
            this._windowOptions = windowOptions;
        }

        public nint GetWindowHandle() => this.GameWindow.Handle;

        public void SetWindowSize(int width, int height) {
            this.GameWindow.Size = new(width, height);
            
            this.UpdateProjectionAndSize(width, height);
        }

        public void SetTargetFramerate(int framerate) {
            this.GameWindow.FramesPerSecond = framerate;
        }

        private void UpdateProjectionAndSize(int width, int height) {
            this.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 1f, 0f);
            this.WindowSize       = new Vector2(width, height);

            try {
                //this is terrible and will be redone soon
                this.GetGlApi().Viewport(new Vector2D<int>(width, height));
            }
            catch {

            }
        }

        /// <summary>
        /// Creates the Window and grabs the OpenGL API of Window
        /// </summary>
        public void Create() {
            SdlWindowing.RegisterPlatform();
            Window.PrioritizeSdl();

            this.GameWindow = Window.Create(this._windowOptions);

            this.UpdateProjectionAndSize(this._windowOptions.Size.X, this._windowOptions.Size.Y);

            this.GameWindow.FramebufferResize += newSize => {
                this.UpdateProjectionAndSize(newSize.X, newSize.Y);
            };
        }
        /// <summary>
        /// Runs the Window
        /// </summary>
        public void RunWindow() {
            this.GameWindow.Run();
        }
        /// <summary>
        /// Gets the OpenGL API
        /// </summary>
        /// <returns>Window's OpenGL API</returns>
        public GL GetGlApi() => this._glApi ??= GL.GetApi(this.GameWindow);
        /// <summary>
        /// Disposes the Window Manager
        /// </summary>
        public void Dispose() {
            try {
                this.GameWindow?.Dispose();
                this._glApi?.Dispose();
            }
            catch {

            }
        }
    }
}
