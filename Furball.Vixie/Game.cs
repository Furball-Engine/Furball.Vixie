using Kettu;
using System;
using Furball.Vixie.Helpers;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Furball.Vixie {
    public abstract class Game : IDisposable {
        /// <summary>
        /// OpenGL API, used to not do Global.Gl everytime
        /// </summary>
        internal GL      gl;

        /// <summary>
        /// Is the Window Active/Focused?
        /// </summary>
        public bool IsActive { get; private set; }
        /// <summary>
        /// Window Manager, handles everything Window Related, from Creation to the Window Projection Matrix
        /// </summary>
        public readonly WindowManager WindowManager;
        /// <summary>
        /// All of the Game Components
        /// </summary>
        public GameComponentCollection Components;

        /// <summary>
        /// Creates a Game Window using `options`
        /// </summary>
        /// <param name="options">Window Creation Options</param>
        protected Game(WindowOptions options) {
            this.WindowManager = new WindowManager(options);
            this.WindowManager.Create();

            this.WindowManager.GameWindow.Update            += this.Update;
            this.WindowManager.GameWindow.Render            += this.Draw;
            this.WindowManager.GameWindow.Load              += this.RendererInitialize;
            this.WindowManager.GameWindow.Closing           += this.RendererOnClosing;
            this.WindowManager.GameWindow.FileDrop          += this.OnFileDrop;
            this.WindowManager.GameWindow.Move              += this.OnWindowMove;
            this.WindowManager.GameWindow.FocusChanged      += this.EngineOnFocusChanged;
            this.WindowManager.GameWindow.StateChanged      += this.EngineOnWindowStateChange;
            this.WindowManager.GameWindow.FramebufferResize += this.EngineFrameBufferResize;
            this.WindowManager.GameWindow.Resize            += this.EngineWindowResize;

            Global.GameInstance = this;

            this.Components = new GameComponentCollection(this);
        }
        /// <summary>
        /// Runs the Game
        /// </summary>
        public void Run() {
            Logger.AddLogger(new ConsoleLogger());
            
            Logger.StartLogging();
            
            this.WindowManager.RunWindow();
        }

        #region Renderer Actions
        /// <summary>
        /// Used to Initialize the Renderer and stuff,
        /// </summary>
        private unsafe void RendererInitialize() {
            Global.Gl = this.WindowManager.GetGlApi();
            this.gl   = Global.Gl;

            //Enables Debugging
            //gl.Enable(GLEnum.DebugOutput);
            //gl.Enable(GLEnum.DebugOutputSynchronous);
            //gl.DebugMessageCallback(this.Callback, null);

            //Enables Blending (Required for Transparent Objects)
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(GLEnum.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            //TODO: input stuffs

            this.Initialize();
        }
        /// <summary>
        /// Debug Callback
        /// </summary>
        private void Callback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userparam) {
            string stringMessage = SilkMarshal.PtrToString(message);

            LoggerLevel level = severity switch {
                GLEnum.DebugSeverityHigh         => LoggerLevelDebugMessageCallback.InstanceHigh,
                GLEnum.DebugSeverityMedium       => LoggerLevelDebugMessageCallback.InstanceMedium,
                GLEnum.DebugSeverityLow          => LoggerLevelDebugMessageCallback.InstanceLow,
                GLEnum.DebugSeverityNotification => LoggerLevelDebugMessageCallback.InstanceNotification,
                _                                => null
            };
            //before u say something beyley, i commented this out cuz it was crashing
            //smth about array not being able to fit somewhere which i went like ???????????? what fuckin array
            //Logger.Log($"{stringMessage}", level);

            Console.WriteLine(stringMessage);
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
            this.WindowManager.WindowState = newState;

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
        protected virtual void Initialize() {}
        /// <summary>
        /// Update Method, Do your Updating work in here
        /// </summary>
        /// <param name="deltaTime">Delta Time</param>
        protected virtual void Update(double deltaTime) {
            this.Components.Update(deltaTime);
        }
        /// <summary>
        /// Draw Method, do your Drawing work in there
        /// </summary>
        /// <param name="deltaTime"></param>
        protected virtual void Draw(double deltaTime) {
            this.Components.Draw(deltaTime);
        }
        /// <summary>
        /// Dispose any IDisposables and other things left to clean up here
        /// </summary>
        public virtual void Dispose() {
            this.Components.Dispose();
        }
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
