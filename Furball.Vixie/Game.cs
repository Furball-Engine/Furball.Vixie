using System;
using System.Diagnostics;
using System.Threading;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Helpers;
using Furball.Vixie.WindowManagement;
using Kettu;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using WindowState = Furball.Vixie.WindowManagement.WindowState;

namespace Furball.Vixie;

public abstract class Game : IDisposable {
    /// <summary>
    ///     Is the Window Active/Focused?
    /// </summary>
    public bool IsActive { get; private set; }
    /// <summary>
    ///     Window Manager, handles everything Window Related, from Creation to the Window Projection Matrix
    /// </summary>
    public IWindowManager WindowManager { get; internal set; }

    private bool _doDisplayLoadingScreen;

    public event EventHandler<string[]> FileDrop;

    private static readonly ThreadLocal<GraphicsResourceFactory> ResourceFactoryThreadLocal =
        new ThreadLocal<GraphicsResourceFactory>();
    public static GraphicsResourceFactory ResourceFactory => ResourceFactoryThreadLocal.Value;
    
    /// <summary>
    ///     Creates a Game Window using `options`
    /// </summary>
    protected Game() {
        if (Global.AlreadyInitialized)
            throw new Exception("no we dont support multiple game instances yet");

        Global.AlreadyInitialized = true;
    }

    private void RunInternal(Backend backend) {
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
            backend = GraphicsBackendState.GetReccomendedBackend();

        this.WindowManager = new SilkWindowManager(backend);

        this.WindowManager.CreateWindow();

        this.HookWindowManagerEvents();

        Global.GameInstance = this;

        Logger.AddLogger(new ConsoleLogger());

        Logger.StartLogging();

        this.WindowManager.RunWindow();
    }

    private void HookWindowManagerEvents() {
        this.WindowManager.WindowLoad    += this.RendererInitialize;
        this.WindowManager.WindowClosing += this.RendererOnClosing;
        this.WindowManager.Update        += this.VixieUpdate;
        this.WindowManager.Draw          += this.VixieDraw;

        this.WindowManager.FocusChanged      += this.EngineOnFocusChanged;
        this.WindowManager.FramebufferResize += this.EngineFrameBufferResize;

        this.WindowManager.FileDrop     += this.OnFileDrop;
        this.WindowManager.StateChanged += this.EngineOnViewStateChange;
    }

    /// <summary>
    ///     Runs the Game
    /// </summary>
    protected void Run(Backend backend = Backend.None) {
        this.RunInternal(backend);
    }

    protected void RunHeadless() {
        //TODO: dont always choose `Dummy` backend, Vulkan can work without a window, this may be useful for CI/Automated testing
        this.RunInternal(Backend.Dummy);
    }

    #region Renderer Actions

    /// <summary>
    ///     Used to Initialize the Renderer 
    /// </summary>
    private void RendererInitialize() {
        // this.InputContext = this.WindowManager.GameView.CreateInput();
        
        ResourceFactoryThreadLocal.Value = new GraphicsResourceFactory(this.WindowManager.GraphicsBackend);

        this._doDisplayLoadingScreen = true;
        this.WindowManager.TryForceDraw();
        
        this.Initialize();
    }

    /// <summary>
    ///     Gets Fired when the Window Gets Closed
    /// </summary>
    private void RendererOnClosing() {
        this.OnClosing();
        this.Dispose();
    }
    /// <summary>
    ///     Gets fired when the Window Focus changes
    /// </summary>
    /// <param name="focused">New Focus</param>
    private void EngineOnFocusChanged(bool focused) {
        this.IsActive = focused;

        this.OnFocusChanged(this.IsActive);
    }
    /// <summary>
    ///     Gets fired when the Window Gets Maxi/Minimized
    /// </summary>
    /// <param name="newState"></param>
    private void EngineOnViewStateChange(WindowState newState) {
        this.OnWindowStateChange(newState);
    }
    /// <summary>
    ///     Gets Fired when the Frame Buffer needs/gets resized
    /// </summary>
    /// <param name="newSize">New Size</param>
    private void EngineFrameBufferResize(Vector2D<int> newSize) {
        // GraphicsBackend.Current.HandleFramebufferResize(newSize.X, newSize.Y);

        this.OnFrameBufferResize(newSize);
    }

    #endregion

    #region Overrides

    /// <summary>
    ///     Used to Initialize any Game Stuff before the Game Begins
    /// </summary>
    protected virtual void Initialize() {
        this.LoadContent();

        this._stopwatch.Start();
    }
    /// <summary>
    ///     Used to Preload content
    /// </summary>
    protected virtual void LoadContent() {}
    /// <summary>
    ///     Used to set the feature levels of all the APIs you are using
    /// </summary>
    public virtual void SetApiFeatureLevels() {}
    private double _trackedDelta;
    private void VixieUpdate(double deltaTime) {
        this.Update(deltaTime * 1000);
    }

    /// <summary>
    ///     Update Method, Do your Updating work in here
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

    private readonly Stopwatch _stopwatch = new Stopwatch();

#if USE_IMGUI
    private bool   _isFirstImguiUpdate = true;
    private double _lastImguiDrawTime;
#endif

    /// <summary>
    ///     Sets up and ends the scene
    /// </summary>
    /// <param name="_"></param>
    /// <param name="deltaTime">Delta time</param>
    private void VixieDraw(double deltaTime) {
        this.WindowManager.GraphicsBackend.BeginScene();

        if (this._doDisplayLoadingScreen) {
            this.DrawLoadingScreen();
            this._doDisplayLoadingScreen = false;
        }
        else {
            this.PreDraw(deltaTime  * 1000);
            this.Draw(deltaTime     * 1000);
            this.PostDraw(deltaTime * 1000);
        }
#if USE_IMGUI
        if (!this._isFirstImguiUpdate)
            this.WindowManager.GraphicsBackend.ImGuiDraw(deltaTime);

        double finalDelta = this._lastImguiDrawTime == 0 ? deltaTime
            : this._stopwatch.Elapsed.TotalSeconds - this._lastImguiDrawTime;

        this.WindowManager.GraphicsBackend.ImGuiUpdate(finalDelta);
        this._lastImguiDrawTime  = this._stopwatch.Elapsed.TotalSeconds;
        this._isFirstImguiUpdate = false;
#endif

        this.WindowManager.GraphicsBackend.EndScene();

        this.WindowManager.GraphicsBackend.Present();

        this.WindowManager.GraphicsBackend.SetFullScissorRect();
    }
    protected virtual void PreDraw(double  deltaTime) {}
    protected virtual void PostDraw(double deltaTime) {}
    /// <summary>
    ///     Draw Method, do your Drawing work in there
    /// </summary>
    /// <param name="deltaTime"></param>
    protected virtual void Draw(double deltaTime) {}
    protected virtual void DrawLoadingScreen() {}
    /// <summary>
    ///     Dispose any IDisposables and other things left to clean up here
    /// </summary>
    public virtual void Dispose() {
        this._stopwatch.Stop();
        DisposeQueue.DisposeAll();
    }
    /// <summary>
    ///     Gets fired when The Window is being closed
    /// </summary>
    protected virtual void OnClosing() {}
    /// <summary>
    ///     Gets fired when a File Drag and Drop Occurs
    /// </summary>
    /// <param name="files">File paths to the Dropped files</param>
    protected virtual void OnFileDrop(string[] files) {
        this.FileDrop?.Invoke(this, files);
    }
    /// <summary>
    ///     Gets fired when the Focus of the Window Changes
    /// </summary>
    /// <param name="newFocus"></param>
    protected virtual void OnFocusChanged(bool newFocus) {}
    /// <summary>
    ///     Gets Fired when the Frame Buffer needs/gets resized
    /// </summary>
    /// <param name="newSize">New Size</param>
    protected virtual void OnFrameBufferResize(Vector2D<int> newSize) {}
    /// <summary>
    ///     Gets fired when the Window Gets Maximized or Minimized
    /// </summary>
    /// <param name="newState"></param>
    protected virtual void OnWindowStateChange(WindowState newState) {}

    #endregion
}