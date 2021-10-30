using System;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Furball.Vixie {
    public abstract class Game : IDisposable {
        /// <summary>
        /// Actual Game Window
        /// </summary>
        private   IWindow _gameWindow;
        /// <summary>
        /// OpenGL API, used to not do Global.Gl everytime
        /// </summary>
        protected GL      gl;

        /// <summary>
        /// Is the Window Active/Focused?
        /// </summary>
        public bool IsActive { get; private set; }
        /// <summary>
        /// Current Window State
        /// </summary>
        public WindowState WindowState { get; private set; }

        /// <summary>
        /// Creates a Game Window using `options`
        /// </summary>
        /// <param name="options">Window Creation Options</param>
        protected Game(WindowOptions options) {
            this._gameWindow = Window.Create(options);

            this._gameWindow.Update            += this.Update;
            this._gameWindow.Render            += this.Draw;
            this._gameWindow.Load              += this.RendererInitialize;
            this._gameWindow.Closing           += this.RendererOnClosing;
            this._gameWindow.FileDrop          += this.OnFileDrop;
            this._gameWindow.Move              += this.OnWindowMove;
            this._gameWindow.FocusChanged      += this.EngineOnFocusChanged;
            this._gameWindow.StateChanged      += this.EngineOnWindowStateChange;
            this._gameWindow.FramebufferResize += this.EngineFrameBufferResize;
            this._gameWindow.Resize            += this.EngineWindowResize;
        }
        /// <summary>
        /// Runs the Game
        /// </summary>
        public void Run() {
            this._gameWindow.Run();
        }

        #region Renderer Actions
        /// <summary>
        /// Used to Initialize the Renderer and stuff,
        /// </summary>
        private unsafe void RendererInitialize() {
            Global.Gl = GL.GetApi(this._gameWindow);
            this.gl   = Global.Gl;

            gl.Enable(GLEnum.DebugOutput);
            gl.Enable(GLEnum.DebugOutputSynchronous);
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(GLEnum.SrcAlpha, BlendingFactor.SrcAlpha);

            //TODO: input stuffs

            gl.DebugMessageCallback(this.Callback, null);

            this.Initialize();
        }
        /// <summary>
        /// Debug Callback
        /// </summary>
        private void Callback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userparam) {
            string messagea = SilkMarshal.PtrToString(message);

            Console.WriteLine(messagea);
        }
        /// <summary>
        /// Gets Fired when the Window Gets Closed
        /// </summary>
        private void RendererOnClosing() {
            this.OnClosing();
        }
        /// <summary>
        /// Gets fired when the Window Focus changes
        /// </summary>
        /// <param name="focused">New Focus</param>
        private void EngineOnFocusChanged(bool focused) {
            this.IsActive = focused;

            this.OnFocusChanged(this.IsActive);
        }
        /// <summary>
        /// Gets fired when the Window Gets Maxi/Minimized
        /// </summary>
        /// <param name="newState"></param>
        private void EngineOnWindowStateChange(WindowState newState) {
            this.WindowState = newState;

            this.OnWindowStateChange(newState);
        }
        /// <summary>
        /// Gets Fired when The Window gets Resized
        /// </summary>
        /// <param name="newSize"></param>
        private void EngineWindowResize(Vector2D<int> newSize) {
            this.OnWindowResize(newSize);
        }
        /// <summary>
        /// Gets Fired when the Frame Buffer needs/gets resized
        /// </summary>
        /// <param name="newSize">New Size</param>
        private void EngineFrameBufferResize(Vector2D<int> newSize) {
            gl.Viewport(Vector2D<int>.Zero, newSize);

            this.OnFrameBufferResize(newSize);
        }

        #endregion

        #region Overrides
        /// <summary>
        /// Used to Initialize any Game Stuff before the Game Begins
        /// </summary>
        protected abstract void Initialize();
        /// <summary>
        /// Update Method, Do your Updating work in here
        /// </summary>
        /// <param name="deltaTime">Delta Time</param>
        protected abstract void Update(double deltaTime);
        /// <summary>
        /// Draw Method, do your Drawing work in there
        /// </summary>
        /// <param name="deltaTime"></param>
        protected abstract void Draw(double deltaTime);
        /// <summary>
        /// Dispose any IDisposables and other things left to clean up here
        /// </summary>
        public abstract void Dispose();
        /// <summary>
        /// Gets fired when The Window is being closed
        /// </summary>
        protected virtual void OnClosing() {}
        /// <summary>
        /// Gets fired when a File Drag and Drop Occurs
        /// </summary>
        /// <param name="files">File paths to the Dropped files</param>
        protected virtual void OnFileDrop(string[] files) {}
        /// <summary>
        /// Gets fired when the Window Moves
        /// </summary>
        /// <param name="newPosition">New Window Position</param>
        protected virtual void OnWindowMove(Vector2D<int> newPosition) {}
        /// <summary>
        /// Gets fired when the Focus of the Window Changes
        /// </summary>
        /// <param name="newFocus"></param>
        protected virtual void OnFocusChanged(bool newFocus) {}
        /// <summary>
        /// Gets Fired when The Window gets Resized
        /// </summary>
        /// <param name="newSize"></param>
        protected virtual void OnWindowResize(Vector2D<int> newSize) {}
        /// <summary>
        /// Gets Fired when the Frame Buffer needs/gets resized
        /// </summary>
        /// <param name="newSize">New Size</param>
        protected virtual void OnFrameBufferResize(Vector2D<int> newSize) {}
        /// <summary>
        /// Gets fired when the Window Gets Maximized or Minimized
        /// </summary>
        /// <param name="newState"></param>
        protected virtual void OnWindowStateChange(WindowState newState) {}

        #endregion
    }
}
