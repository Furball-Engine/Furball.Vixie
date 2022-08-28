using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Furball.Vixie.Backends.Shared.ImGuiController;

public abstract class ImGuiController : IDisposable {
    private          IView            _view;
    private          IInputContext    _input;
    private readonly ImGuiFontConfig? _imGuiFontConfig;
    private readonly Action?          _onConfigureIo;
    private          bool             _frameBegun;
    private readonly List<char>       _pressedChars = new();
    private          IKeyboard        _keyboard;

    private int _windowWidth;
    private int _windowHeight;

    public IntPtr Context;

    /// <summary>
    ///     Constructs a new ImGuiController with font configuration and onConfigure Action.
    /// </summary>
    public ImGuiController(IView  view, IInputContext input, ImGuiFontConfig? imGuiFontConfig = null,
                            Action onConfigureIo = null) {
        this._view            = view;
        this._input           = input;
        this._imGuiFontConfig = imGuiFontConfig;
        this._onConfigureIo   = onConfigureIo;
    }

    public void Initialize() {
        this.Init(_view, _input);

        ImGuiIOPtr io = ImGui.GetIO();
        if (_imGuiFontConfig is not null)
            io.Fonts.AddFontFromFileTTF(_imGuiFontConfig.Value.FontPath, _imGuiFontConfig.Value.FontSize);

        _onConfigureIo?.Invoke();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        this.CreateDeviceResources();
        SetKeyMappings();

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
    }

    private void OnKeyChar(IKeyboard arg1, char arg2) {
        this._pressedChars.Add(arg2);
    }

    private void WindowResized(Vector2D<int> size) {
        this._windowWidth  = size.X;
        this._windowHeight = size.Y;
    }

    /// <summary>
    ///     Renders the ImGui draw list data.
    ///     This method requires a <see cref="GraphicsDevice" /> because it may create new DeviceBuffers if the size of vertex
    ///     or index data has increased beyond the capacity of the existing buffers.
    ///     A <see cref="CommandList" /> is needed to submit drawing and resource update commands.
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
        this.UpdateImGuiInput();

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

    private static readonly Key[] keyEnumArr = (Key[])Enum.GetValues(typeof(Key));
    private void UpdateImGuiInput() {
        ImGuiIOPtr io = ImGui.GetIO();

        MouseState? mouseState    = this._input.Mice[0].CaptureState();
        IKeyboard?  keyboardState = this._input.Keyboards[0];

        io.MouseDown[0] = mouseState.IsButtonPressed(MouseButton.Left);
        io.MouseDown[1] = mouseState.IsButtonPressed(MouseButton.Right);
        io.MouseDown[2] = mouseState.IsButtonPressed(MouseButton.Middle);

        Point point = new((int)mouseState.Position.X, (int)mouseState.Position.Y);
        io.MousePos = new Vector2(point.X, point.Y);

        ScrollWheel wheel = mouseState.GetScrollWheels()[0];
        io.MouseWheel  = wheel.Y;
        io.MouseWheelH = wheel.X;

        foreach (Key key in keyEnumArr) {
            if (key == Key.Unknown)
                continue;
            io.KeysDown[(int)key] = keyboardState.IsKeyPressed(key);
        }

        foreach (char c in this._pressedChars)
            io.AddInputCharacter(c);

        this._pressedChars.Clear();

        io.KeyCtrl  = keyboardState.IsKeyPressed(Key.ControlLeft) || keyboardState.IsKeyPressed(Key.ControlRight);
        io.KeyAlt   = keyboardState.IsKeyPressed(Key.AltLeft)     || keyboardState.IsKeyPressed(Key.AltRight);
        io.KeyShift = keyboardState.IsKeyPressed(Key.ShiftLeft)   || keyboardState.IsKeyPressed(Key.ShiftRight);
        io.KeySuper = keyboardState.IsKeyPressed(Key.SuperLeft)   || keyboardState.IsKeyPressed(Key.SuperRight);
    }

    internal void PressChar(char keyChar) {
        this._pressedChars.Add(keyChar);
    }

    private static void SetKeyMappings() {
        ImGuiIOPtr io = ImGui.GetIO();
        io.KeyMap[(int)ImGuiKey.Tab]        = (int)Key.Tab;
        io.KeyMap[(int)ImGuiKey.LeftArrow]  = (int)Key.Left;
        io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
        io.KeyMap[(int)ImGuiKey.UpArrow]    = (int)Key.Up;
        io.KeyMap[(int)ImGuiKey.DownArrow]  = (int)Key.Down;
        io.KeyMap[(int)ImGuiKey.PageUp]     = (int)Key.PageUp;
        io.KeyMap[(int)ImGuiKey.PageDown]   = (int)Key.PageDown;
        io.KeyMap[(int)ImGuiKey.Home]       = (int)Key.Home;
        io.KeyMap[(int)ImGuiKey.End]        = (int)Key.End;
        io.KeyMap[(int)ImGuiKey.Delete]     = (int)Key.Delete;
        io.KeyMap[(int)ImGuiKey.Backspace]  = (int)Key.Backspace;
        io.KeyMap[(int)ImGuiKey.Enter]      = (int)Key.Enter;
        io.KeyMap[(int)ImGuiKey.Escape]     = (int)Key.Escape;
        io.KeyMap[(int)ImGuiKey.A]          = (int)Key.A;
        io.KeyMap[(int)ImGuiKey.C]          = (int)Key.C;
        io.KeyMap[(int)ImGuiKey.V]          = (int)Key.V;
        io.KeyMap[(int)ImGuiKey.X]          = (int)Key.X;
        io.KeyMap[(int)ImGuiKey.Y]          = (int)Key.Y;
        io.KeyMap[(int)ImGuiKey.Z]          = (int)Key.Z;
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

        this.DisposeInternal();

        ImGui.DestroyContext(this.Context);
    }
}