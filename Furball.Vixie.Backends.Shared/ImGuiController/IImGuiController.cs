#if USE_IMGUI
using System;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Furball.Vixie.Backends.Shared.ImGuiController;
public abstract class ImGuiController : IDisposable {
    private          IView            _view;
    private          IInputContext    _input;
    private readonly ImGuiFontConfig? _imGuiFontConfig;
    private readonly Action?          _onConfigureIo;
    private          bool             _frameBegun;
    private          IKeyboard        _keyboard = null!;

    private int _windowWidth;
    private int _windowHeight;

    public IntPtr Context;

    /// <summary>
    ///     Constructs a new ImGuiController with font configuration and onConfigure Action.
    /// </summary>
    public ImGuiController(IView  view, IInputContext input, ImGuiFontConfig? imGuiFontConfig = null,
                            Action? onConfigureIo = null) {
        this._view            = view;
        this._input           = input;
        this._imGuiFontConfig = imGuiFontConfig;
        this._onConfigureIo   = onConfigureIo;
    }

    public void Initialize() {
        this.Init(this._view, this._input);

        ImGuiIOPtr io = ImGui.GetIO();
        if (this._imGuiFontConfig is not null)
            io.Fonts.AddFontFromFileTTF(this._imGuiFontConfig.Value.FontPath, this._imGuiFontConfig.Value.FontSize);

        this._onConfigureIo?.Invoke();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        this.CreateDeviceResources();

        this.SetPerFrameImGuiData(1f / 60f);

        this.BeginFrame();
    }

    public void MakeCurrent() {
        ImGui.SetCurrentContext(this.Context);
    }

    private void Init(IView view, IInputContext input) {
        this._view         = view;
        this._input        = input;
        this._windowWidth  = view.Size.X;
        this._windowHeight = view.Size.Y;

        this.Context = ImGui.CreateContext();
        ImGui.SetCurrentContext(this.Context);
        ImGui.StyleColorsDark();
    }

    private void BeginFrame() {
        ImGui.NewFrame();
        this._frameBegun       =  true;
        this._keyboard         =  this._input.Keyboards[0];
        this._view.Resize      += this.WindowResized;
        this._keyboard.KeyChar += this.OnKeyChar;
        this._keyboard.KeyDown += this.OnKeyDown;
        this._keyboard.KeyUp += this.OnKeyUp;
        foreach (IMouse inputMouse in this._input.Mice) {
            inputMouse.MouseDown += this.OnMouseDown;
            inputMouse.MouseUp   += this.OnMouseUp;
            inputMouse.MouseMove += this.OnMouseMove;
            inputMouse.Scroll    += this.OnScroll;
        }
    }
    private void OnScroll(IMouse arg1, ScrollWheel arg2) {
        this.MakeCurrent();
        ImGui.GetIO().AddMouseWheelEvent(arg2.X, arg2.Y);
    }
    private void OnMouseMove(IMouse arg1, Vector2 arg2) {
        this.MakeCurrent();
        ImGui.GetIO().AddMousePosEvent(arg2.X, arg2.Y);
    }
    private void OnMouseDown(IMouse arg1, MouseButton arg2) {
        this.MakeCurrent();
        int idx = arg2.ToImGuiButton();
        if(idx != -1)
            ImGui.GetIO().AddMouseButtonEvent(idx, true);
    }
    private void OnMouseUp(IMouse arg1, MouseButton arg2) {
        this.MakeCurrent();
        int idx = arg2.ToImGuiButton();
        if(idx != -1)
            ImGui.GetIO().AddMouseButtonEvent(idx, false);
    }
    private void OnKeyDown(IKeyboard arg1, Key arg2, int arg3) {
        this.MakeCurrent();
        ImGui.GetIO().AddKeyEvent(arg2.ToImGuiKey(), true);
    }
    private void OnKeyUp(IKeyboard arg1, Key arg2, int arg3) {
        this.MakeCurrent();
        ImGui.GetIO().AddKeyEvent(arg2.ToImGuiKey(), false);
    }

    private void OnKeyChar(IKeyboard kb, char c) {
        this.MakeCurrent();
        ImGui.GetIO().AddInputCharacter(c);
    }

    private void WindowResized(Vector2D<int> size) {
        this._windowWidth  = size.X;
        this._windowHeight = size.Y;
    }

    /// <summary>
    ///     Renders the ImGui draw list data.
    /// </summary>
    public void Render() {
        if (this._frameBegun) {
            IntPtr oldCtx = ImGui.GetCurrentContext();

            if (oldCtx != this.Context)
                ImGui.SetCurrentContext(this.Context);

            this._frameBegun = false;
            ImGui.Render();
            this.RenderImDrawData(ImGui.GetDrawData());

            if (oldCtx != this.Context)
                ImGui.SetCurrentContext(oldCtx);
        }
    }

    /// <summary>
    ///     Updates ImGui input and IO configuration state.
    /// </summary>
    public void Update(float deltaSeconds) {
        IntPtr oldCtx = ImGui.GetCurrentContext();

        if (oldCtx != this.Context)
            ImGui.SetCurrentContext(this.Context);

        if (this._frameBegun)
            ImGui.Render();

        this.SetPerFrameImGuiData(deltaSeconds);

        this._frameBegun = true;
        ImGui.NewFrame();

        if (oldCtx != this.Context)
            ImGui.SetCurrentContext(oldCtx);
    }

    /// <summary>
    ///     Sets per-frame data based on the associated window.
    ///     This is called by Update(float).
    /// </summary>
    private void SetPerFrameImGuiData(float deltaSeconds) {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new Vector2(this._windowWidth, this._windowHeight);

        if (this._windowWidth > 0 && this._windowHeight > 0)
            io.DisplayFramebufferScale = new Vector2(this._view.FramebufferSize.X / this._windowWidth,
                                                     this._view.FramebufferSize.Y / this._windowHeight);

        io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
    }

    protected abstract void SetupRenderState(ImDrawDataPtr drawDataPtr, int framebufferWidth, int framebufferHeight);

    protected abstract void PreDraw();
    protected abstract void Draw(ImDrawDataPtr drawDataPtr);
    protected abstract void PostDraw();

    private void RenderImDrawData(ImDrawDataPtr drawDataPtr) {
        int framebufferWidth  = (int)(drawDataPtr.DisplaySize.X * drawDataPtr.FramebufferScale.X);
        int framebufferHeight = (int)(drawDataPtr.DisplaySize.Y * drawDataPtr.FramebufferScale.Y);
        if (framebufferWidth <= 0 || framebufferHeight <= 0)
            return;

        this.PreDraw();

        this.SetupRenderState(drawDataPtr, framebufferWidth, framebufferHeight);

        this.Draw(drawDataPtr);

        this.PostDraw();
    }

    protected abstract void CreateDeviceResources();


    /// <summary>
    ///     Creates the texture used to render text.
    /// </summary>
    protected abstract void RecreateFontDeviceTexture();

    protected abstract void DisposeInternal();

    private bool _isDisposed;
    /// <summary>
    ///     Frees all graphics resources used by the renderer.
    /// </summary>
    public void Dispose() {
        if (this._isDisposed)
            return;

        this._isDisposed = true;
        
        this._view.Resize      -= this.WindowResized;
        this._keyboard.KeyChar -= this.OnKeyChar;
        this._keyboard.KeyDown -= this.OnKeyDown;
        this._keyboard.KeyUp   -= this.OnKeyUp;
        foreach (IMouse inputMouse in this._input.Mice) {
            inputMouse.MouseDown -= this.OnMouseDown;
            inputMouse.MouseUp   -= this.OnMouseUp;
            inputMouse.MouseMove -= this.OnMouseMove;
            inputMouse.Scroll    -= this.OnScroll;
        }
        
        this.DisposeInternal();

        ImGui.DestroyContext(this.Context);
    }
}
#endif