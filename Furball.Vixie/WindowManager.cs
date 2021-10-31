using System;
using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

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
        /// <summary>
        /// Creates a Window Manager
        /// </summary>
        /// <param name="windowOptions">Window Creation Options</param>
        public WindowManager(WindowOptions windowOptions) {
            this._windowOptions = windowOptions;
        }
        /// <summary>
        /// Creates the Window and grabs the OpenGL API of Window
        /// </summary>
        public void Create() {
            this.GameWindow = Window.Create(this._windowOptions);

            this.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, this._windowOptions.Size.X, this._windowOptions.Size.Y, 0, 1f, 0f);

            this.GameWindow.FramebufferResize += newSize => {
                this.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, newSize.X, newSize.Y, 0, 1f, 0f);
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
            this.GameWindow?.Dispose();
            this._glApi?.Dispose();
        }
    }
}
