using System;
using System.Diagnostics;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using JetBrains.Annotations;
using Kettu;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Furball.Vixie; 

public abstract class Game : IDisposable {
    /// <summary>
    /// Window Input Context
    /// </summary>
    internal IInputContext InputContext;

    /// <summary>
    /// Is the Window Active/Focused?
    /// </summary>
    public bool IsActive { get; private set; }
    /// <summary>
    /// Window Manager, handles everything Window Related, from Creation to the Window Projection Matrix
    /// </summary>
    public WindowManager WindowManager { get; internal set;}

    private bool _doDisplayLoadingScreen;

    public event EventHandler<string[]> FileDrop;
    
    /// <summary>
    /// Creates a Game Window using `options`
    /// </summary>
    protected Game() {
        if (Global.AlreadyInitialized)
            throw new Exception("no we dont support multiple game instances yet");

        Global.AlreadyInitialized = true;
    }

    private void RunInternal(WindowOptions options, Backend backend, bool requestViewOnly) {
        try {
            Backends.Shared.Global.LatestSupportedGl = OpenGLDetector.OpenGLDetector.GetLatestSupported();
        }
        catch {
            //if an error occurs detecting the OpenGL version, fallback to legacy gl
            //todo make this logic smarter
            Backends.Shared.Global.LatestSupportedGl.GL   = new APIVersion(2, 0);
            Backends.Shared.Global.LatestSupportedGl.GLES = new APIVersion(0, 0);
        }

        if (backend == Backend.None)
            backend = GraphicsBackend.GetReccomendedBackend();

        this.WindowManager = new WindowManager(options, backend);
        
        this.WindowManager.RequestViewOnly = requestViewOnly;

        this.WindowManager.Initialize();

        this.WindowManager.Create();

        this.HookWindowEvents();
            
        Global.GameInstance = this;

        Logger.AddLogger(new ConsoleLogger());
            
        Logger.StartLogging();
            
        this.WindowManager.RunWindow();
    }

    private void HookWindowEvents() {
        this.WindowManager.GameView.Load    += this.RendererInitialize;
        this.WindowManager.GameView.Closing += this.RendererOnClosing;
        this.WindowManager.GameView.Update  += this.VixieUpdate;
        this.WindowManager.GameView.Render    += this.VixieDraw;

        this.WindowManager.GameView.FocusChanged      += this.EngineOnFocusChanged;
        this.WindowManager.GameView.FramebufferResize += this.EngineFrameBufferResize;
        this.WindowManager.GameView.Resize            += this.EngineViewResize;

        if (!this.WindowManager.ViewOnly) {
            this.WindowManager.GameWindow.FileDrop     += this.OnFileDrop;
            this.WindowManager.GameWindow.Move         += this.OnViewMove;
            this.WindowManager.GameWindow.StateChanged += this.EngineOnViewStateChange;
        }
    }
    
    protected void RunViewOnly(WindowOptions options, Backend backend = Backend.None) {
        this.RunInternal(options, backend, true);
    }
        
    /// <summary>
    /// Runs the Game
    /// </summary>
    protected void Run(WindowOptions options, Backend backend = Backend.None) {
        this.RunInternal(options, backend, false);
    }

    protected void RunHeadless() {
        //TODO: dont always choose `Dummy` backend, Vulkan can work without a window, this may be useful for CI/Automated testing
        this.RunInternal(WindowOptions.Default, Backend.Dummy, false);
    }

    #region Renderer Actions
    /// <summary>
    /// Used to Initialize the Renderer and stuff,
    /// </summary>
    private void RendererInitialize() {
        this.InputContext = this.WindowManager.GameView.CreateInput();

        this.WindowManager.InputContext = this.InputContext;
        this.WindowManager.SetupGraphicsApi();

        Global.WindowManager = this.WindowManager;
            
        // else { //this is for the dummy backend
        //     //todo: support other backends headless
        //     GraphicsBackend.SetBackend(Backend.Dummy);
        //     Global.GameInstance.SetApiFeatureLevels();
        //     GraphicsBackend.Current.SetMainThread();
        //     GraphicsBackend.Current.Initialize(null, null);
        //
        //     GraphicsBackend.Current.HandleFramebufferResize(1280, 720);
        // }

        this._doDisplayLoadingScreen = true;
        this.WindowManager.GameView.DoRender();

        this.Initialize();
    }

    /// <summary>
    /// Gets Fired when the Window Gets Closed
    /// </summary>
    private void RendererOnClosing() {
        this.OnClosing();
        this.Dispose();
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
    private void EngineOnViewStateChange(WindowState newState) {
        this.WindowManager.WindowState = newState;

        this.OnWindowStateChange(newState);
    }
    /// <summary>
    /// Gets Fired when The Window gets Resized
    /// </summary>
    /// <param name="newSize"></param>
    private void EngineViewResize(Vector2D<int> newSize) {
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

        this._stopwatch.Start();
    }
    /// <summary>
    /// Used to Preload content
    /// </summary>
    protected virtual void LoadContent() {}
    /// <summary>
    /// Used to set the feature levels of all the APIs you are using
    /// </summary>
    public virtual void SetApiFeatureLevels() {}
    private double _trackedDelta = 0;
    private void VixieUpdate(double deltaTime) {
        this.Update(deltaTime * 1000);
    }
    
    /// <summary>
    /// Update Method, Do your Updating work in here
    /// </summary>
    /// <param name="deltaTime">Delta Time</param>
    protected virtual void Update(double deltaTime) {
        DisposeQueue.DoDispose();

        this.CheckForInvalidTrackerResourceReferences(deltaTime);
    }
    
    [Conditional("DEBUG")]
    private void CheckForInvalidTrackerResourceReferences(double deltaTime) {
        this._trackedDelta += deltaTime;
        if (this._trackedDelta > 5000) {
            Global.TrackedTextures.RemoveAll(x => !x.TryGetTarget(out _));
            Global.TrackedRenderTargets.RemoveAll(x => !x.TryGetTarget(out _));

            this._trackedDelta -= 5000;
        }
    }

    private readonly Stopwatch _stopwatch = new();

#if USE_IMGUI
    private          bool      _isFirstImguiUpdate = true;
    private          double    _lastImguiDrawTime;
#endif

    /// <summary>
    /// Sets up and ends the scene
    /// </summary>
    /// <param name="_"></param>
    /// <param name="deltaTime">Delta time</param>
    private void VixieDraw(double deltaTime) {
        GraphicsBackend.Current.BeginScene();

        if (this._doDisplayLoadingScreen) {
            this.DrawLoadingScreen();
            this._doDisplayLoadingScreen = false;
        } else {
            this.PreDraw(deltaTime * 1000);
            this.Draw(deltaTime * 1000);
            this.PostDraw(deltaTime * 1000);
        }
#if USE_IMGUI
        if (!this._isFirstImguiUpdate)
            GraphicsBackend.Current.ImGuiDraw(deltaTime);

        double finalDelta = this._lastImguiDrawTime == 0 ? deltaTime
                                : this._stopwatch.Elapsed.TotalSeconds - this._lastImguiDrawTime;

        GraphicsBackend.Current.ImGuiUpdate(finalDelta);
        this._lastImguiDrawTime  = this._stopwatch.Elapsed.TotalSeconds;
        this._isFirstImguiUpdate = false;
#endif

        GraphicsBackend.Current.EndScene();

        GraphicsBackend.Current.Present();
            
        GraphicsBackend.Current.SetFullScissorRect();
    }
    protected virtual void PreDraw(double deltaTime) {}
    protected virtual void PostDraw(double deltaTime) {}
    /// <summary>
    /// Draw Method, do your Drawing work in there
    /// </summary>
    /// <param name="deltaTime"></param>
    protected virtual void Draw(double deltaTime) {
        
    }
    protected virtual void DrawLoadingScreen() {}
    /// <summary>
    /// Dispose any IDisposables and other things left to clean up here
    /// </summary>
    public virtual void Dispose() {
        this._stopwatch.Stop();
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
    protected virtual void OnFileDrop(string[] files) {
        this.FileDrop?.Invoke(this, files);
    }
    /// <summary>
    /// Gets fired when the Window Moves
    /// </summary>
    /// <param name="newPosition">New Window Position</param>
    protected virtual void OnViewMove(Vector2D<int> newPosition) {}
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