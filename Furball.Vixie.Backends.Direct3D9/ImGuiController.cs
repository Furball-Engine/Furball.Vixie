using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Key=Silk.NET.Input.Key;
using MouseButton=Silk.NET.Input.MouseButton;
using Point=System.Drawing.Point;

namespace Furball.Vixie.Backends.Direct3D9; 

public class ImGuiController : IDisposable {
    private          IView         _view;
    private          IInputContext _input;
    private          bool          _frameBegun;
    private readonly List<char>    _pressedChars = new List<char>();
    private          IKeyboard     _keyboard;

    private int            _windowWidth;
    private int            _windowHeight;
    // private GraphicsDevice _gd;

    // Device objects
    private IntPtr _fontAtlasID = (IntPtr)1;

    // Image trackers
    private readonly List<IDisposable> _ownedResources = new List<IDisposable>();
    public           Assembly          _assembly;

    private IntPtr _context;

    /// <summary>
    /// Constructs a new ImGuiController with font configuration and onConfigure Action.
    /// </summary>
    public ImGuiController(/* GraphicsDevice gd, OutputDescription outputDescription, */ IView view, IInputContext input) {
        this._assembly = typeof(ImGuiController).Assembly;

        this.Init(/* gd, */ view, input);

        var io = ImGui.GetIO();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        this.CreateDeviceResources(/* gd, outputDescription, this._colorSpaceHandling */);
        SetKeyMappings();

        this.SetPerFrameImGuiData(1f / 60f);

        this.BeginFrame();
    }

    private void Init(/* GraphicsDevice gd, */ IView view, IInputContext input) {
        // this._gd           = gd;
        this._view         = view;
        this._input        = input;
        this._windowWidth  = view.Size.X;
        this._windowHeight = view.Size.Y;

        this._context = ImGui.CreateContext();
        ImGui.SetCurrentContext(this._context);
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
    /// Renders the ImGui draw list data.
    /// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
    /// or index data has increased beyond the capacity of the existing buffers.
    /// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
    /// </summary>
    public void Render(/* GraphicsDevice gd, CommandList cl*/ ) {
        if (this._frameBegun) {
            this._frameBegun = false;
            ImGui.Render();
            this.RenderImDrawData(ImGui.GetDrawData() /* , gd, cl */);
        }
    }

    /// <summary>
    /// Updates ImGui input and IO configuration state.
    /// </summary>
    public void Update(float deltaSeconds) {
        if (this._frameBegun) {
            ImGui.Render();
        }

        this.SetPerFrameImGuiData(deltaSeconds);
        this.UpdateImGuiInput();

        this._frameBegun = true;
        ImGui.NewFrame();
    }

    /// <summary>
    /// Sets per-frame data based on the associated window.
    /// This is called by Update(float).
    /// </summary>
    private void SetPerFrameImGuiData(float deltaSeconds) {
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(this._windowWidth, this._windowHeight);

        if (this._windowWidth > 0 && this._windowHeight > 0) {
            io.DisplayFramebufferScale = new Vector2(this._view.FramebufferSize.X / this._windowWidth, this._view.FramebufferSize.Y / this._windowHeight);
        }

        io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
    }

    private Key[] keyEnumValues = (Key[])Enum.GetValues(typeof(Key));
    private void UpdateImGuiInput() {
        var io = ImGui.GetIO();

        var mouseState    = this._input.Mice[0].CaptureState();
        var keyboardState = this._input.Keyboards[0];

        io.MouseDown[0] = mouseState.IsButtonPressed(MouseButton.Left);
        io.MouseDown[1] = mouseState.IsButtonPressed(MouseButton.Right);
        io.MouseDown[2] = mouseState.IsButtonPressed(MouseButton.Middle);

        var point = new Point((int)mouseState.Position.X, (int)mouseState.Position.Y);
        io.MousePos = new Vector2(point.X, point.Y);

        var wheel = mouseState.GetScrollWheels()[0];
        io.MouseWheel  = wheel.Y;
        io.MouseWheelH = wheel.X;

        for (var i = 0; i < this.keyEnumValues.Length; i++) {
            Key key = this.keyEnumValues[i];
            if (key == Key.Unknown) {
                continue;
            }
            io.KeysDown[(int)key] = keyboardState.IsKeyPressed(key);
        }

        foreach (var c in this._pressedChars) {
            io.AddInputCharacter(c);
        }

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
        var io = ImGui.GetIO();
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

    /* public ResourceSet GetImageResourceSet(IntPtr imGuiBinding) {
        if (!this._viewsById.TryGetValue(imGuiBinding, out ResourceSetInfo rsi)) {
            throw new InvalidOperationException("No registered ImGui binding with id " + imGuiBinding);
        }

        return rsi.ResourceSet;
    } */

    private unsafe void RenderImDrawData(ImDrawDataPtr draw_data/* , GraphicsDevice gd, CommandList cl */) {
        uint vertexOffsetInVertices = 0;
        uint indexOffsetInElements  = 0;

        if (draw_data.CmdListsCount == 0) {
            return;
        }
        
        /*
        for (int i = 0; i < draw_data.CmdListsCount; i++) {
            ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

            cl.UpdateBuffer(this._vertexBuffer, vertexOffsetInVertices * (uint)sizeof(ImDrawVert), cmd_list.VtxBuffer.Data, (uint)(cmd_list.VtxBuffer.Size * sizeof(ImDrawVert)));

            cl.UpdateBuffer(this._indexBuffer, indexOffsetInElements * sizeof(ushort), cmd_list.IdxBuffer.Data, (uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));

            vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
            indexOffsetInElements  += (uint)cmd_list.IdxBuffer.Size;
        }
        */

        // Setup orthographic projection matrix into our constant buffer
        {
            var io = ImGui.GetIO();

            Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0.0f, -1.0f, 1.0f);

            // this._gd.UpdateBuffer(this._projMatrixBuffer, 0, ref mvp);
        }

        // cl.SetVertexBuffer(0, this._vertexBuffer);
        // cl.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt16);
        // cl.SetPipeline(this._pipeline);
        // cl.SetGraphicsResourceSet(0, this._mainResourceSet);

        draw_data.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

        // Render command lists
        int vtx_offset = 0;
        int idx_offset = 0;
        for (int n = 0; n < draw_data.CmdListsCount; n++) {
            ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];
            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++) {
                ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != IntPtr.Zero) {
                    throw new NotImplementedException();
                }
                if (pcmd.TextureId != IntPtr.Zero) {
                    if (pcmd.TextureId == this._fontAtlasID) {
                        // cl.SetGraphicsResourceSet(1, this._fontTextureResourceSet);
                    } else {
                        // cl.SetGraphicsResourceSet(1, this.GetImageResourceSet(pcmd.TextureId));
                    }
                }

                // cl.SetScissorRect(0, (uint)pcmd.ClipRect.X, (uint)pcmd.ClipRect.Y, (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X), (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                // cl.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)idx_offset, (int)(pcmd.VtxOffset + vtx_offset), 0);
            }

            idx_offset += cmd_list.IdxBuffer.Size;
            vtx_offset += cmd_list.VtxBuffer.Size;
        }
            
        // cl.SetFullScissorRect(0);
    }

    private string GetEmbeddedResourceText(string resourceName) {
        using (StreamReader sr = new StreamReader(this._assembly.GetManifestResourceStream(resourceName))) {
            return sr.ReadToEnd();
        }
    }

    private byte[] GetEmbeddedResourceBytes(string resourceName) {
        using (Stream s = this._assembly.GetManifestResourceStream(resourceName)) {
            byte[] ret = new byte[s.Length];
            s.Read(ret, 0, (int)s.Length);
            return ret;
        }
    }

    private void CreateDeviceResources() {
        this.RecreateFontDeviceTexture();
    }

    /// <summary>
    /// Creates the texture used to render text.
    /// </summary>
    private unsafe void RecreateFontDeviceTexture() {
        ImGuiIOPtr io = ImGui.GetIO();
        // Build
        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);

        // Store our identifier
        io.Fonts.SetTexID(this._fontAtlasID);

        //TODO: fill font texture

        io.Fonts.ClearTexData();
    }

    /// <summary>
    /// Frees all graphics resources used by the renderer.
    /// </summary>
    public void Dispose() {
        this._view.Resize      -= this.WindowResized;
        this._keyboard.KeyChar -= this.OnKeyChar;

        foreach (IDisposable resource in this._ownedResources) {
            resource.Dispose();
        }

        ImGui.DestroyContext(this._context);
    }
}