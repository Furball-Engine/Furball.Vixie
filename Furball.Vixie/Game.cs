using System;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Furball.Vixie {
    public abstract class Game : IDisposable {
        private   IWindow _gameWindow;
        protected GL      gl;

        public bool IsActive { get; private set; }
        public WindowState WindowState { get; private set; }

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
        }

        public void Run() {
            this._gameWindow.Run();
        }

        #region Renderer Actions

        private void RendererInitialize() {
            Global.Gl = GL.GetApi(this._gameWindow);
            this.gl   = Global.Gl;

            this.gl.Enable(GLEnum.DebugOutput);
            this.gl.Enable(GLEnum.DebugOutputSynchronous);

            //TODO: input stuffs

            this.Initialize();
        }

        private void RendererOnClosing() {
            this.OnClosing();
        }

        private void EngineOnFocusChanged(bool focused) {
            this.IsActive = focused;

            this.OnFocusChanged(this.IsActive);
        }

        private void EngineOnWindowStateChange(WindowState newState) {
            this.WindowState = newState;

            this.OnWindowStateChange(newState);
        }

        private void EngineWindowResize(Vector2D<int> newSize) {
            this.OnWindowReize(newSize);
        }

        private void EngineFrameBufferResize(Vector2D<int> newSize) {
            this.OnFrameBufferResize(newSize);
        }

        #endregion

        #region Overrides

        protected abstract void Initialize();
        protected abstract void Update(double obj);
        protected abstract void Draw(double obj);
        public abstract void Dispose();
        protected virtual void OnClosing() {}
        protected virtual void OnFileDrop(string[] files) {}
        protected virtual void OnWindowMove(Vector2D<int> newPosition) {}
        protected virtual void OnFocusChanged(bool newFocus) {}
        protected virtual void OnWindowReize(Vector2D<int> newSize) {}
        protected virtual void OnFrameBufferResize(Vector2D<int> newSize) {}
        protected virtual void OnWindowStateChange(WindowState newState) {}

        #endregion
    }
}
