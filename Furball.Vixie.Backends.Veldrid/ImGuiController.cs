using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Veldrid;
using Key=Silk.NET.Input.Key;
using MouseButton=Silk.NET.Input.MouseButton;

namespace Furball.Vixie.Backends.Veldrid {
    public class ImGuiController : IDisposable {
        private          IView         _view;
        private          IInputContext _input;
        private          bool          _frameBegun;
        private readonly List<char>    _pressedChars = new List<char>();
        private          IKeyboard     _keyboard;

        private int            _windowWidth;
        private int            _windowHeight;
        private GraphicsDevice _gd;

        // Device objects
        private DeviceBuffer            _vertexBuffer;
        private DeviceBuffer            _indexBuffer;
        private DeviceBuffer            _projMatrixBuffer;
        private Texture _fontTexture;
        private Shader                  _vertexShader;
        private Shader                  _fragmentShader;
        private ResourceLayout          _layout;
        private ResourceLayout          _textureLayout;
        private Pipeline                _pipeline;
        private ResourceSet             _mainResourceSet;
        private ResourceSet             _fontTextureResourceSet;
        private IntPtr                  _fontAtlasID = (IntPtr)1;

        // Image trackers
        private readonly Dictionary<IntPtr, ResourceSetInfo> _viewsById          = new Dictionary<IntPtr, ResourceSetInfo>();
        private readonly List<IDisposable>                   _ownedResources     = new List<IDisposable>();
        private          ColorSpaceHandling                  _colorSpaceHandling = ColorSpaceHandling.Legacy;
        public           Assembly                            _assembly;

        private IntPtr _context;

        /// <summary>
        /// Constructs a new ImGuiController with font configuration and onConfigure Action.
        /// </summary>
        public ImGuiController(GraphicsDevice gd, OutputDescription outputDescription, IView view, IInputContext input) {
            this._assembly = typeof(ImGuiController).Assembly;

            this.Init(gd, view, input);

            var io = ImGui.GetIO();

            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

            this.CreateDeviceResources(gd, outputDescription, this._colorSpaceHandling);
            SetKeyMappings();

            this.SetPerFrameImGuiData(1f / 60f);

            this.BeginFrame();
        }

        private void Init(GraphicsDevice gd, IView view, IInputContext input) {
            this._gd           = gd;
            this._view         = view;
            this._input        = input;
            this._windowWidth  = view.Size.X;
            this._windowHeight = view.Size.Y;

            this._context = ImGui.CreateContext();
            ImGui.SetCurrentContext(this._context);
            ImGui.StyleColorsDark();

            foreach (IKeyboard inputKeyboard in this._input.Keyboards) {
                inputKeyboard.KeyDown += this.KeyboardOnKeyDown;
                inputKeyboard.KeyUp   += this.KeyboardOnKeyUp;
                inputKeyboard.KeyChar += this.KeyboardOnKeyChar;
            }

            foreach (IMouse inputMouse in this._input.Mice) {
                inputMouse.MouseMove += this.MouseOnMove;
                inputMouse.MouseDown += this.MouseOnDown;
                inputMouse.MouseUp   += this.MouseOnUp;
                inputMouse.Scroll    += this.MouseOnScroll;
            }
        }

        private void MouseOnScroll(IMouse arg1, ScrollWheel wheel) {
            ImGui.SetCurrentContext(this._context);
            ImGuiIOPtr io = ImGui.GetIO();

            io.AddMouseWheelEvent(wheel.X, wheel.Y);
        }

        private static int MouseButtonToImGuiMouseIndex(MouseButton b) {
            switch (b) {
                case MouseButton.Left:
                    return 0;
                case MouseButton.Right:
                    return 1;
                case MouseButton.Middle:
                    return 2;
            }

            return -1;
        }

        private void MouseOnDown(IMouse arg1, MouseButton arg2) {
            ImGui.SetCurrentContext(this._context);
            ImGuiIOPtr io = ImGui.GetIO();

            int index = MouseButtonToImGuiMouseIndex(arg2);

            if (index != -1)
                io.AddMouseButtonEvent(index, true);
        }

        private void MouseOnUp(IMouse arg1, MouseButton arg2) {
            ImGui.SetCurrentContext(this._context);
            ImGuiIOPtr io = ImGui.GetIO();

            int index = MouseButtonToImGuiMouseIndex(arg2);

            if (index != -1)
                io.AddMouseButtonEvent(index, false);
        }

        private void MouseOnMove(IMouse arg1, Vector2 pos) {
            ImGui.SetCurrentContext(this._context);
            ImGuiIOPtr io = ImGui.GetIO();

            io.AddMousePosEvent(pos.X, pos.Y);
        }
        private void KeyboardOnKeyChar(IKeyboard arg1, char @char) {
            ImGui.SetCurrentContext(this._context);
            ImGuiIOPtr io = ImGui.GetIO();

            io.AddInputCharacter(@char);
        }

        private void KeyboardOnKeyUp(IKeyboard arg1, Key key, int arg3) {
            ImGui.SetCurrentContext(this._context);
            ImGuiIOPtr io = ImGui.GetIO();

            ImGuiKey k = KeyToImGuiKey(key);

            if (k != ImGuiKey.None)
                io.AddKeyEvent(k, false);
        }

        private static ImGuiKey KeyToImGuiKey(Key k) {
            return k switch {
                Key.Space          => ImGuiKey.Space,
                Key.Apostrophe     => ImGuiKey.Apostrophe,
                Key.Comma          => ImGuiKey.Comma,
                Key.Minus          => ImGuiKey.Minus,
                Key.Period         => ImGuiKey.Period,
                Key.Slash          => ImGuiKey.Slash,
                Key.Number0        => ImGuiKey._0,
                Key.Number1        => ImGuiKey._1,
                Key.Number2        => ImGuiKey._2,
                Key.Number3        => ImGuiKey._3,
                Key.Number4        => ImGuiKey._4,
                Key.Number5        => ImGuiKey._5,
                Key.Number6        => ImGuiKey._6,
                Key.Number7        => ImGuiKey._7,
                Key.Number8        => ImGuiKey._8,
                Key.Number9        => ImGuiKey._9,
                Key.Semicolon      => ImGuiKey.Semicolon,
                Key.Equal          => ImGuiKey.Equal,
                Key.A              => ImGuiKey.A,
                Key.B              => ImGuiKey.B,
                Key.C              => ImGuiKey.C,
                Key.D              => ImGuiKey.D,
                Key.E              => ImGuiKey.E,
                Key.F              => ImGuiKey.F,
                Key.G              => ImGuiKey.G,
                Key.H              => ImGuiKey.H,
                Key.I              => ImGuiKey.I,
                Key.J              => ImGuiKey.J,
                Key.K              => ImGuiKey.K,
                Key.L              => ImGuiKey.L,
                Key.M              => ImGuiKey.M,
                Key.N              => ImGuiKey.N,
                Key.O              => ImGuiKey.O,
                Key.P              => ImGuiKey.P,
                Key.Q              => ImGuiKey.Q,
                Key.R              => ImGuiKey.R,
                Key.S              => ImGuiKey.S,
                Key.T              => ImGuiKey.T,
                Key.U              => ImGuiKey.U,
                Key.V              => ImGuiKey.V,
                Key.W              => ImGuiKey.W,
                Key.X              => ImGuiKey.X,
                Key.Y              => ImGuiKey.Y,
                Key.Z              => ImGuiKey.Z,
                Key.LeftBracket    => ImGuiKey.LeftBracket,
                Key.BackSlash      => ImGuiKey.Backslash,
                Key.RightBracket   => ImGuiKey.RightBracket,
                Key.GraveAccent    => ImGuiKey.GraveAccent,
                Key.Escape         => ImGuiKey.Escape,
                Key.Enter          => ImGuiKey.Enter,
                Key.Tab            => ImGuiKey.Tab,
                Key.Backspace      => ImGuiKey.Backspace,
                Key.Insert         => ImGuiKey.Insert,
                Key.Delete         => ImGuiKey.Delete,
                Key.Right          => ImGuiKey.RightArrow,
                Key.Left           => ImGuiKey.LeftArrow,
                Key.Down           => ImGuiKey.DownArrow,
                Key.Up             => ImGuiKey.UpArrow,
                Key.PageUp         => ImGuiKey.PageUp,
                Key.PageDown       => ImGuiKey.PageDown,
                Key.Home           => ImGuiKey.Home,
                Key.End            => ImGuiKey.End,
                Key.CapsLock       => ImGuiKey.CapsLock,
                Key.ScrollLock     => ImGuiKey.ScrollLock,
                Key.NumLock        => ImGuiKey.NumLock,
                Key.PrintScreen    => ImGuiKey.PrintScreen,
                Key.Pause          => ImGuiKey.Pause,
                Key.F1             => ImGuiKey.F1,
                Key.F2             => ImGuiKey.F2,
                Key.F3             => ImGuiKey.F3,
                Key.F4             => ImGuiKey.F4,
                Key.F5             => ImGuiKey.F5,
                Key.F6             => ImGuiKey.F6,
                Key.F7             => ImGuiKey.F7,
                Key.F8             => ImGuiKey.F8,
                Key.F9             => ImGuiKey.F9,
                Key.F10            => ImGuiKey.F10,
                Key.F11            => ImGuiKey.F11,
                Key.F12            => ImGuiKey.F12,
                Key.Keypad0        => ImGuiKey.Keypad0,
                Key.Keypad1        => ImGuiKey.Keypad1,
                Key.Keypad2        => ImGuiKey.Keypad2,
                Key.Keypad3        => ImGuiKey.Keypad3,
                Key.Keypad4        => ImGuiKey.Keypad4,
                Key.Keypad5        => ImGuiKey.Keypad5,
                Key.Keypad6        => ImGuiKey.Keypad6,
                Key.Keypad7        => ImGuiKey.Keypad7,
                Key.Keypad8        => ImGuiKey.Keypad8,
                Key.Keypad9        => ImGuiKey.Keypad9,
                Key.KeypadDecimal  => ImGuiKey.KeypadDecimal,
                Key.KeypadDivide   => ImGuiKey.KeypadDivide,
                Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
                Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
                Key.KeypadAdd      => ImGuiKey.KeypadAdd,
                Key.KeypadEnter    => ImGuiKey.KeypadEnter,
                Key.KeypadEqual    => ImGuiKey.KeypadEqual,
                Key.ShiftLeft      => ImGuiKey.LeftShift,
                Key.ControlLeft    => ImGuiKey.LeftCtrl,
                Key.AltLeft        => ImGuiKey.LeftAlt,
                Key.SuperLeft      => ImGuiKey.LeftSuper,
                Key.ShiftRight     => ImGuiKey.RightShift,
                Key.ControlRight   => ImGuiKey.RightCtrl,
                Key.AltRight       => ImGuiKey.RightAlt,
                Key.SuperRight     => ImGuiKey.RightSuper,
                Key.Menu           => ImGuiKey.Menu,
                Key.World1         => ImGuiKey.None,
                Key.World2         => ImGuiKey.None,
                Key.F13            => ImGuiKey.None,
                Key.F14            => ImGuiKey.None,
                Key.F15            => ImGuiKey.None,
                Key.F16            => ImGuiKey.None,
                Key.F17            => ImGuiKey.None,
                Key.F18            => ImGuiKey.None,
                Key.F19            => ImGuiKey.None,
                Key.F20            => ImGuiKey.None,
                Key.F21            => ImGuiKey.None,
                Key.F22            => ImGuiKey.None,
                Key.F23            => ImGuiKey.None,
                Key.F24            => ImGuiKey.None,
                Key.F25            => ImGuiKey.None,
                Key.Unknown        => ImGuiKey.None,
                _                  => ImGuiKey.None
            };
        }

        private void KeyboardOnKeyDown(IKeyboard arg1, Key key, int arg3) {
            ImGui.SetCurrentContext(this._context);
            ImGuiIOPtr io = ImGui.GetIO();

            ImGuiKey k = KeyToImGuiKey(key);

            if (k != ImGuiKey.None)
                io.AddKeyEvent(k, true);
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
        public void Render(GraphicsDevice gd, CommandList cl) {
            if (this._frameBegun) {
                this._frameBegun = false;
                ImGui.Render();
                this.RenderImDrawData(ImGui.GetDrawData(), gd, cl);
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
            ImGui.SetCurrentContext(this._context);
            var io = ImGui.GetIO();
            io.DisplaySize = new Vector2(this._windowWidth, this._windowHeight);

            if (this._windowWidth > 0 && this._windowHeight > 0) {
                io.DisplayFramebufferScale = new Vector2(this._view.FramebufferSize.X / this._windowWidth, this._view.FramebufferSize.Y / this._windowHeight);
            }

            io.DeltaTime = deltaSeconds;// DeltaTime is in seconds.
        }

        private Key[]  keyEnumValues = (Key[])Enum.GetValues(typeof(Key));
        private void UpdateImGuiInput() {
            var io = ImGui.GetIO();

            io.AddFocusEvent(true);

            //
            // var mouseState    = this._input.Mice[0].CaptureState();
            // var keyboardState = this._input.Keyboards[0];
            //
            // io.MouseDown[0] = mouseState.IsButtonPressed(MouseButton.Left);
            // io.MouseDown[1] = mouseState.IsButtonPressed(MouseButton.Right);
            // io.MouseDown[2] = mouseState.IsButtonPressed(MouseButton.Middle);
            //
            // var point = new Point((int)mouseState.Position.X, (int)mouseState.Position.Y);
            // io.MousePos = new Vector2(point.X, point.Y);
            //
            // var wheel = mouseState.GetScrollWheels()[0];
            // io.MouseWheel  = wheel.Y;
            // io.MouseWheelH = wheel.X;
            //
            // for (var i = 0; i < this.keyEnumValues.Length; i++) {
            //     Key key = this.keyEnumValues[i];
            //     if (key == Key.Unknown) {
            //         continue;
            //     }
            //     io.KeysDown[(int)key] = keyboardState.IsKeyPressed(key);
            // }
            //
            // foreach (var c in this._pressedChars) {
            // io.AddInputCharacter(c);
            // }
            //
            // this._pressedChars.Clear();
            //
            // io.KeyCtrl  = keyboardState.IsKeyPressed(Key.ControlLeft) || keyboardState.IsKeyPressed(Key.ControlRight);
            // io.KeyAlt   = keyboardState.IsKeyPressed(Key.AltLeft)     || keyboardState.IsKeyPressed(Key.AltRight);
            // io.KeyShift = keyboardState.IsKeyPressed(Key.ShiftLeft)   || keyboardState.IsKeyPressed(Key.ShiftRight);
            // io.KeySuper = keyboardState.IsKeyPressed(Key.SuperLeft)   || keyboardState.IsKeyPressed(Key.SuperRight);
        }

        internal void PressChar(char keyChar) {
            this._pressedChars.Add(keyChar);
        }

        private static void SetKeyMappings() {
            // var io = ImGui.GetIO();
            // io.KeyMap[(int)ImGuiKey.Tab]        = (int)Key.Tab;
            // io.KeyMap[(int)ImGuiKey.LeftArrow]  = (int)Key.Left;
            // io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
            // io.KeyMap[(int)ImGuiKey.UpArrow]    = (int)Key.Up;
            // io.KeyMap[(int)ImGuiKey.DownArrow]  = (int)Key.Down;
            // io.KeyMap[(int)ImGuiKey.PageUp]     = (int)Key.PageUp;
            // io.KeyMap[(int)ImGuiKey.PageDown]   = (int)Key.PageDown;
            // io.KeyMap[(int)ImGuiKey.Home]       = (int)Key.Home;
            // io.KeyMap[(int)ImGuiKey.End]        = (int)Key.End;
            // io.KeyMap[(int)ImGuiKey.Delete]     = (int)Key.Delete;
            // io.KeyMap[(int)ImGuiKey.Backspace]  = (int)Key.Backspace;
            // io.KeyMap[(int)ImGuiKey.Enter]      = (int)Key.Enter;
            // io.KeyMap[(int)ImGuiKey.Escape]     = (int)Key.Escape;
            // io.KeyMap[(int)ImGuiKey.A]          = (int)Key.A;
            // io.KeyMap[(int)ImGuiKey.C]          = (int)Key.C;
            // io.KeyMap[(int)ImGuiKey.V]          = (int)Key.V;
            // io.KeyMap[(int)ImGuiKey.X]          = (int)Key.X;
            // io.KeyMap[(int)ImGuiKey.Y]          = (int)Key.Y;
            // io.KeyMap[(int)ImGuiKey.Z]          = (int)Key.Z;
        }

        public ResourceSet GetImageResourceSet(IntPtr imGuiBinding) {
            if (!this._viewsById.TryGetValue(imGuiBinding, out ResourceSetInfo rsi)) {
                throw new InvalidOperationException("No registered ImGui binding with id " + imGuiBinding);
            }

            return rsi.ResourceSet;
        }

        private unsafe void RenderImDrawData(ImDrawDataPtr draw_data, GraphicsDevice gd, CommandList cl) {
            uint vertexOffsetInVertices = 0;
            uint indexOffsetInElements  = 0;

            if (draw_data.CmdListsCount == 0) {
                return;
            }

            uint totalVBSize = (uint)(draw_data.TotalVtxCount * sizeof(ImDrawVert));
            if (totalVBSize > this._vertexBuffer.SizeInBytes) {
                this._vertexBuffer.Dispose();
                this._vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            }

            uint totalIBSize = (uint)(draw_data.TotalIdxCount * sizeof(ushort));
            if (totalIBSize > this._indexBuffer.SizeInBytes) {
                this._indexBuffer.Dispose();
                this._indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            }

            for (int i = 0; i < draw_data.CmdListsCount; i++) {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

                cl.UpdateBuffer(this._vertexBuffer, vertexOffsetInVertices * (uint)sizeof(ImDrawVert), cmd_list.VtxBuffer.Data, (uint)(cmd_list.VtxBuffer.Size * sizeof(ImDrawVert)));

                cl.UpdateBuffer(this._indexBuffer, indexOffsetInElements * sizeof(ushort), cmd_list.IdxBuffer.Data, (uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));

                vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
                indexOffsetInElements  += (uint)cmd_list.IdxBuffer.Size;
            }

            // Setup orthographic projection matrix into our constant buffer
            {
                var io = ImGui.GetIO();

                Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0.0f, -1.0f, 1.0f);

                this._gd.UpdateBuffer(this._projMatrixBuffer, 0, ref mvp);
            }

            cl.SetVertexBuffer(0, this._vertexBuffer);
            cl.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(this._pipeline);
            cl.SetGraphicsResourceSet(0, this._mainResourceSet);

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
                            cl.SetGraphicsResourceSet(1, this._fontTextureResourceSet);
                        } else {
                            cl.SetGraphicsResourceSet(1, this.GetImageResourceSet(pcmd.TextureId));
                        }
                    }

                    cl.SetScissorRect(0, (uint)pcmd.ClipRect.X, (uint)pcmd.ClipRect.Y, (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X), (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                    cl.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)idx_offset, (int)(pcmd.VtxOffset + vtx_offset), 0);
                }

                idx_offset += cmd_list.IdxBuffer.Size;
                vtx_offset += cmd_list.VtxBuffer.Size;
            }
            
            cl.SetFullScissorRect(0);
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

        private byte[] LoadEmbeddedShaderCode(ResourceFactory factory, string name, ShaderStages stage, ColorSpaceHandling colorSpaceHandling) {
            switch (factory.BackendType) {
                case GraphicsBackend.Direct3D11: {
                    if (stage == ShaderStages.Vertex && colorSpaceHandling == ColorSpaceHandling.Legacy) { name += "-legacy"; }
                    string resourceName = name + ".hlsl.bytes";
                    return this.GetEmbeddedResourceBytes(resourceName);
                }
                case GraphicsBackend.OpenGL: {
                    if (stage == ShaderStages.Vertex && colorSpaceHandling == ColorSpaceHandling.Legacy) { name += "-legacy"; }
                    string resourceName = name + ".glsl";
                    return this.GetEmbeddedResourceBytes(resourceName);
                }
                case GraphicsBackend.OpenGLES: {
                    if (stage == ShaderStages.Vertex && colorSpaceHandling == ColorSpaceHandling.Legacy) { name += "-legacy"; }
                    string resourceName = name + ".glsles";
                    return this.GetEmbeddedResourceBytes(resourceName);
                }
                case GraphicsBackend.Vulkan: {
                    string resourceName = name + ".spv";
                    return this.GetEmbeddedResourceBytes(resourceName);
                }
                case GraphicsBackend.Metal: {
                    string resourceName = name + ".metallib";
                    return this.GetEmbeddedResourceBytes(resourceName);
                }
                default:
                    throw new NotImplementedException();
            }
        }

        private void CreateDeviceResources(GraphicsDevice gd, OutputDescription outputDescription, ColorSpaceHandling colorSpaceHandling) {
            this._gd                 = gd;
            this._colorSpaceHandling = colorSpaceHandling;
            ResourceFactory factory = gd.ResourceFactory;
            this._vertexBuffer      = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            this._vertexBuffer.Name = "ImGui.NET Vertex Buffer";
            this._indexBuffer       = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            this._indexBuffer.Name  = "ImGui.NET Index Buffer";

            this._projMatrixBuffer      = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            this._projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

            byte[] vertexShaderBytes   = this.LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-vertex", ShaderStages.Vertex,   this._colorSpaceHandling);
            byte[] fragmentShaderBytes = this.LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-frag",   ShaderStages.Fragment, this._colorSpaceHandling);
            this._vertexShader   = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex,   vertexShaderBytes,   this._gd.BackendType == GraphicsBackend.Vulkan ? "main" : "VS"));
            this._fragmentShader = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, this._gd.BackendType == GraphicsBackend.Vulkan ? "main" : "FS"));

            VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[] {
                new VertexLayoutDescription(new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2), new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2), new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
            };

            this._layout        = factory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer,   ShaderStages.Vertex), new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
            this._textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("MainTexture",            ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(BlendStateDescription.SingleAlphaBlend,
                                                                             new DepthStencilStateDescription(false, false, ComparisonKind.Always),
                                                                             new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                                                                             PrimitiveTopology.TriangleList,
                                                                             new ShaderSetDescription(vertexLayouts,
                                                                                                      new[] {
                                                                                                          this._vertexShader, this._fragmentShader
                                                                                                      },
                                                                                                      new[] {
                                                                                                          new SpecializationConstant(0, gd.IsClipSpaceYInverted), new SpecializationConstant(1, this._colorSpaceHandling == ColorSpaceHandling.Legacy),
                                                                                                      }),
                                                                             new ResourceLayout[] {
                                                                                 this._layout, this._textureLayout
                                                                             },
                                                                             outputDescription,
                                                                             ResourceBindingModel.Default);
            this._pipeline = factory.CreateGraphicsPipeline(ref pd);

            this._mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(this._layout, this._projMatrixBuffer, gd.PointSampler));

            this.RecreateFontDeviceTexture(gd);
        }

        /// <summary>
        /// Creates the texture used to render text.
        /// </summary>
        private unsafe void RecreateFontDeviceTexture(GraphicsDevice gd) {
            ImGuiIOPtr io = ImGui.GetIO();
            // Build
            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);

            // Store our identifier
            io.Fonts.SetTexID(this._fontAtlasID);

            this._fontTexture?.Dispose();
            this._fontTexture      = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            this._fontTexture.Name = "ImGui.NET Font Texture";
            gd.UpdateTexture(this._fontTexture, (IntPtr)pixels, (uint)(bytesPerPixel * width * height), 0, 0, 0, (uint)width, (uint)height, 1, 0, 0);

            this._fontTextureResourceSet?.Dispose();
            this._fontTextureResourceSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(this._textureLayout, this._fontTexture));

            io.Fonts.ClearTexData();
        }

        /// <summary>
        /// Frees all graphics resources used by the renderer.
        /// </summary>
        public void Dispose() {
            this._view.Resize      -= this.WindowResized;
            this._keyboard.KeyChar -= this.OnKeyChar;

            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
            this._projMatrixBuffer.Dispose();
            this._fontTexture.Dispose();
            this._vertexShader.Dispose();
            this._fragmentShader.Dispose();
            this._layout.Dispose();
            this._textureLayout.Dispose();
            this._pipeline.Dispose();
            this._mainResourceSet.Dispose();
            this._fontTextureResourceSet.Dispose();

            foreach (IDisposable resource in this._ownedResources) {
                resource.Dispose();
            }

            ImGui.DestroyContext(this._context);
        }

        private struct ResourceSetInfo {
            public readonly IntPtr      ImGuiBinding;
            public readonly ResourceSet ResourceSet;

            public ResourceSetInfo(IntPtr imGuiBinding, ResourceSet resourceSet) {
                this.ImGuiBinding = imGuiBinding;
                this.ResourceSet  = resourceSet;
            }
        }
    }
}
