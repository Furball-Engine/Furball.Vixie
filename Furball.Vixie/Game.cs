using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Helpers;
using Kettu;
using NativeLibraryLoader;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using LibraryLoader=NativeLibraryLoader.LibraryLoader;
using PathResolver=NativeLibraryLoader.PathResolver;

namespace Furball.Vixie {
    public abstract class Game : IDisposable {
        /// <summary>
        /// Window Input Context
        /// </summary>
        internal IInputContext _inputContext;

        /// <summary>
        /// Is the Window Active/Focused?
        /// </summary>
        public bool IsActive { get; private set; }
        /// <summary>
        /// Window Manager, handles everything Window Related, from Creation to the Window Projection Matrix
        /// </summary>
        public WindowManager WindowManager { get; internal set;}
        /// <summary>
        /// All of the Game Components
        /// </summary>
        public GameComponentCollection Components { get; internal set;}

        /// <summary>
        /// Creates a Game Window using `options`
        /// </summary>
        protected Game() {
            if (Global.AlreadyInitialized)
                throw new Exception("no we dont support multiple game instances yet");

            Global.AlreadyInitialized = true;
        }
        /// <summary>
        /// Runs the Game
        /// </summary>
        public void Run(WindowOptions options, Backend backend = Backend.None) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.FrameworkDescription.Contains("Mono")) {
                // Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", Assembly.GetExecutingAssembly().Location);
                // IntPtr ptr = LibraryLoader.GetPlatformDefaultLoader().LoadNativeLibrary("libcimgui");
                
                // Console.WriteLine(ptr);
            }
            
            if (backend == Backend.None)
                backend = GraphicsBackend.GetReccomendedBackend();
            
            this.WindowManager = new WindowManager(options, backend);
            this.WindowManager.Create();

            this.WindowManager.GameWindow.Update            += this.Update;
            this.WindowManager.GameWindow.Render            += this.VixieDraw;
            this.WindowManager.GameWindow.Load              += this.RendererInitialize;
            this.WindowManager.GameWindow.Closing           += this.RendererOnClosing;
            this.WindowManager.GameWindow.FileDrop          += this.OnFileDrop;
            this.WindowManager.GameWindow.Move              += this.OnWindowMove;
            this.WindowManager.GameWindow.FocusChanged      += this.EngineOnFocusChanged;
            this.WindowManager.GameWindow.StateChanged      += this.EngineOnWindowStateChange;
            this.WindowManager.GameWindow.FramebufferResize += this.EngineFrameBufferResize;
            this.WindowManager.GameWindow.Resize            += this.EngineWindowResize;
            
            Global.GameInstance = this;

            this.Components = new GameComponentCollection();

            Logger.AddLogger(new ConsoleLogger());
            
            Logger.StartLogging();
            
            this.WindowManager.RunWindow();
        }

        #region Renderer Actions
        /// <summary>
        /// Used to Initialize the Renderer and stuff,
        /// </summary>
        private void RendererInitialize() {
            this._inputContext = this.WindowManager.GameWindow.CreateInput();

            this.WindowManager.InputContext = this._inputContext;
            this.WindowManager.SetupGraphicsApi();

            Global.WindowManager = this.WindowManager;

            this.Initialize();
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
            GraphicsBackend.Current.HandleFramebufferResize(newSize.X, newSize.Y);

            this.OnFrameBufferResize(newSize);
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Used to Initialize any Game Stuff before the Game Begins
        /// </summary>
        protected virtual void Initialize() {
            this.LoadContent();
        }
        /// <summary>
        /// Used to Preload content
        /// </summary>
        protected virtual void LoadContent() {}
        /// <summary>
        /// Update Method, Do your Updating work in here
        /// </summary>
        /// <param name="deltaTime">Delta Time</param>
        protected virtual void Update(double deltaTime) {
            GraphicsBackend.Current.ImGuiUpdate(deltaTime);
            this.Components.Update(deltaTime);

            DisposeQueue.DoDispose();
        }
        /// <summary>
        /// Sets up and ends the scene
        /// </summary>
        /// <param name="deltaTime"></param>
        private void VixieDraw(double deltaTime) {
            GraphicsBackend.Current.BeginScene();

            this.Draw(deltaTime);
            
            GraphicsBackend.Current.EndScene();

            GraphicsBackend.Current.Present();
        }
        /// <summary>
        /// Draw Method, do your Drawing work in there
        /// </summary>
        /// <param name="deltaTime"></param>
        protected virtual void Draw(double deltaTime) {
            this.Components.Draw(deltaTime);
            GraphicsBackend.Current.ImGuiDraw(deltaTime);
        }
        /// <summary>
        /// Dispose any IDisposables and other things left to clean up here
        /// </summary>
        public virtual void Dispose() {
            this.Components.Dispose();
            DisposeQueue.DisposeAll();
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
