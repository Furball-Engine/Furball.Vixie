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
using Veldrid;
using Key=Silk.NET.Input.Key;
using MouseButton=Silk.NET.Input.MouseButton;
using Point=System.Drawing.Point;

namespace Furball.Vixie.Graphics.Backends.Veldrid {
    public class ImGuiController : IDisposable {
        private          IView         _view;
        private          IInputContext _input;
        private          bool          _frameBegun;
        private readonly List<char>    _pressedChars = new List<char>();
        private          IKeyboard     _keyboard;

        private int  _attribLocationTex;
        private int  _attribLocationProjMtx;
        private int  _attribLocationVtxPos;
        private int  _attribLocationVtxUV;
        private int  _attribLocationVtxColor;
        private uint _vboHandle;
        private uint _elementsHandle;
        private uint _vertexArrayObject;

        private int            _windowWidth;
        private int            _windowHeight;
        private GraphicsDevice _gd;

        // Device objects
        private DeviceBuffer            _vertexBuffer;
        private DeviceBuffer            _indexBuffer;
        private DeviceBuffer            _projMatrixBuffer;
        private global::Veldrid.Texture _fontTexture;
        private Shader                  _vertexShader;
        private Shader                  _fragmentShader;
        private ResourceLayout          _layout;
        private ResourceLayout          _textureLayout;
        private Pipeline                _pipeline;
        private ResourceSet             _mainResourceSet;
        private ResourceSet             _fontTextureResourceSet;
        private IntPtr                  _fontAtlasID = (IntPtr)1;

        // Image trackers
        private readonly Dictionary<TextureView, ResourceSetInfo> _setsByView         = new Dictionary<TextureView, ResourceSetInfo>();
        private readonly Dictionary<Texture, TextureView>         _autoViewsByTexture = new Dictionary<Texture, TextureView>();
        private readonly Dictionary<IntPtr, ResourceSetInfo>      _viewsById          = new Dictionary<IntPtr, ResourceSetInfo>();
        private readonly List<IDisposable>                        _ownedResources     = new List<IDisposable>();
        private          int                                      _lastAssignedID     = 100;
        private          ColorSpaceHandling                       _colorSpaceHandling = ColorSpaceHandling.Legacy;
        private          Assembly                                 _assembly;

        /// <summary>
        /// Constructs a new ImGuiController with font configuration and onConfigure Action.
        /// </summary>
        public ImGuiController(GraphicsDevice gd, OutputDescription outputDescription, IView view, IInputContext input) {
            this._assembly = typeof(ImGuiController).Assembly;

            this.Init(gd, view, input);

            var io = ImGuiNET.ImGui.GetIO();

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

            IntPtr context = ImGuiNET.ImGui.CreateContext();
            ImGuiNET.ImGui.SetCurrentContext(context);
            ImGuiNET.ImGui.StyleColorsDark();
        }

        private void BeginFrame() {
            ImGuiNET.ImGui.NewFrame();
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
                ImGuiNET.ImGui.Render();
                this.RenderImDrawData(ImGuiNET.ImGui.GetDrawData(), gd, cl);
            }
        }

        /// <summary>
        /// Updates ImGui input and IO configuration state.
        /// </summary>
        public void Update(float deltaSeconds) {
            if (this._frameBegun) {
                ImGuiNET.ImGui.Render();
            }

            this.SetPerFrameImGuiData(deltaSeconds);
            this.UpdateImGuiInput();

            this._frameBegun = true;
            ImGuiNET.ImGui.NewFrame();
        }

        /// <summary>
        /// Sets per-frame data based on the associated window.
        /// This is called by Update(float).
        /// </summary>
        private void SetPerFrameImGuiData(float deltaSeconds) {
            var io = ImGuiNET.ImGui.GetIO();
            io.DisplaySize = new Vector2(this._windowWidth, this._windowHeight);

            if (this._windowWidth > 0 && this._windowHeight > 0) {
                io.DisplayFramebufferScale = new Vector2(this._view.FramebufferSize.X / this._windowWidth, this._view.FramebufferSize.Y / this._windowHeight);
            }

            io.DeltaTime = deltaSeconds;// DeltaTime is in seconds.
        }

        private Key[] keyEnumValues = (Key[])Enum.GetValues(typeof(Key));
        private void UpdateImGuiInput() {
            var io = ImGuiNET.ImGui.GetIO();

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
            var io = ImGuiNET.ImGui.GetIO();
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

        public ResourceSet GetImageResourceSet(IntPtr imGuiBinding) {
            if (!_viewsById.TryGetValue(imGuiBinding, out ResourceSetInfo rsi)) {
                throw new InvalidOperationException("No registered ImGui binding with id " + imGuiBinding.ToString());
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
            if (totalVBSize > _vertexBuffer.SizeInBytes) {
                _vertexBuffer.Dispose();
                _vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            }

            uint totalIBSize = (uint)(draw_data.TotalIdxCount * sizeof(ushort));
            if (totalIBSize > _indexBuffer.SizeInBytes) {
                _indexBuffer.Dispose();
                _indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            }

            for (int i = 0; i < draw_data.CmdListsCount; i++) {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

                cl.UpdateBuffer(_vertexBuffer, vertexOffsetInVertices * (uint)sizeof(ImDrawVert), cmd_list.VtxBuffer.Data, (uint)(cmd_list.VtxBuffer.Size * sizeof(ImDrawVert)));

                cl.UpdateBuffer(_indexBuffer, indexOffsetInElements * sizeof(ushort), cmd_list.IdxBuffer.Data, (uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));

                vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
                indexOffsetInElements  += (uint)cmd_list.IdxBuffer.Size;
            }

            // Setup orthographic projection matrix into our constant buffer
            {
                var io = ImGui.GetIO();

                Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0.0f, -1.0f, 1.0f);

                this._gd.UpdateBuffer(_projMatrixBuffer, 0, ref mvp);
            }

            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(_pipeline);
            cl.SetGraphicsResourceSet(0, _mainResourceSet);

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
                    } else {
                        if (pcmd.TextureId != IntPtr.Zero) {
                            if (pcmd.TextureId == _fontAtlasID) {
                                cl.SetGraphicsResourceSet(1, _fontTextureResourceSet);
                            } else {
                                cl.SetGraphicsResourceSet(1, GetImageResourceSet(pcmd.TextureId));
                            }
                        }

                        cl.SetScissorRect(0, (uint)pcmd.ClipRect.X, (uint)pcmd.ClipRect.Y, (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X), (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                        cl.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)idx_offset, (int)(pcmd.VtxOffset + vtx_offset), 0);
                    }
                }

                idx_offset += cmd_list.IdxBuffer.Size;
                vtx_offset += cmd_list.VtxBuffer.Size;
            }
        }

        private string GetEmbeddedResourceText(string resourceName) {
            using (StreamReader sr = new StreamReader(_assembly.GetManifestResourceStream(resourceName))) {
                return sr.ReadToEnd();
            }
        }

        private byte[] GetEmbeddedResourceBytes(string resourceName) {
            using (Stream s = _assembly.GetManifestResourceStream(resourceName)) {
                byte[] ret = new byte[s.Length];
                s.Read(ret, 0, (int)s.Length);
                return ret;
            }
        }

        private byte[] LoadEmbeddedShaderCode(ResourceFactory factory, string name, ShaderStages stage, ColorSpaceHandling colorSpaceHandling) {
            switch (factory.BackendType) {
                case global::Veldrid.GraphicsBackend.Direct3D11: {
                    if (stage == ShaderStages.Vertex && colorSpaceHandling == ColorSpaceHandling.Legacy) { name += "-legacy"; }
                    string resourceName = name + ".hlsl.bytes";
                    return GetEmbeddedResourceBytes(resourceName);
                }
                case global::Veldrid.GraphicsBackend.OpenGL: {
                    if (stage == ShaderStages.Vertex && colorSpaceHandling == ColorSpaceHandling.Legacy) { name += "-legacy"; }
                    string resourceName = name + ".glsl";
                    return GetEmbeddedResourceBytes(resourceName);
                }
                case global::Veldrid.GraphicsBackend.OpenGLES: {
                    if (stage == ShaderStages.Vertex && colorSpaceHandling == ColorSpaceHandling.Legacy) { name += "-legacy"; }
                    string resourceName = name + ".glsles";
                    return GetEmbeddedResourceBytes(resourceName);
                }
                case global::Veldrid.GraphicsBackend.Vulkan: {
                    string resourceName = name + ".spv";
                    return GetEmbeddedResourceBytes(resourceName);
                }
                case global::Veldrid.GraphicsBackend.Metal: {
                    string resourceName = name + ".metallib";
                    return GetEmbeddedResourceBytes(resourceName);
                }
                default:
                    throw new NotImplementedException();
            }
        }

        private void CreateDeviceResources(GraphicsDevice gd, OutputDescription outputDescription, ColorSpaceHandling colorSpaceHandling) {
            _gd                 = gd;
            _colorSpaceHandling = colorSpaceHandling;
            ResourceFactory factory = gd.ResourceFactory;
            _vertexBuffer      = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            _vertexBuffer.Name = "ImGui.NET Vertex Buffer";
            _indexBuffer       = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            _indexBuffer.Name  = "ImGui.NET Index Buffer";

            _projMatrixBuffer      = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

            byte[] vertexShaderBytes   = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-vertex", ShaderStages.Vertex,   _colorSpaceHandling);
            byte[] fragmentShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-frag",   ShaderStages.Fragment, _colorSpaceHandling);
            _vertexShader   = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex,   vertexShaderBytes,   _gd.BackendType == global::Veldrid.GraphicsBackend.Vulkan ? "main" : "VS"));
            _fragmentShader = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, _gd.BackendType == global::Veldrid.GraphicsBackend.Vulkan ? "main" : "FS"));

            VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[] {
                new VertexLayoutDescription(new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2), new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2), new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
            };

            _layout        = factory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer,   ShaderStages.Vertex), new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
            _textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("MainTexture",            ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(BlendStateDescription.SingleAlphaBlend,
                                                                             new DepthStencilStateDescription(false, false, ComparisonKind.Always),
                                                                             new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                                                                             PrimitiveTopology.TriangleList,
                                                                             new ShaderSetDescription(vertexLayouts,
                                                                                                      new[] {
                                                                                                          _vertexShader, _fragmentShader
                                                                                                      },
                                                                                                      new[] {
                                                                                                          new SpecializationConstant(0, gd.IsClipSpaceYInverted), new SpecializationConstant(1, _colorSpaceHandling == ColorSpaceHandling.Legacy),
                                                                                                      }),
                                                                             new ResourceLayout[] {
                                                                                 _layout, _textureLayout
                                                                             },
                                                                             outputDescription,
                                                                             ResourceBindingModel.Default);
            _pipeline = factory.CreateGraphicsPipeline(ref pd);

            _mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_layout, _projMatrixBuffer, gd.PointSampler));

            RecreateFontDeviceTexture(gd);
        }

        /// <summary>
        /// Creates the texture used to render text.
        /// </summary>
        private unsafe void RecreateFontDeviceTexture(GraphicsDevice gd) {
            ImGuiIOPtr io = ImGui.GetIO();
            // Build
            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);

            // Store our identifier
            io.Fonts.SetTexID(_fontAtlasID);

            _fontTexture?.Dispose();
            _fontTexture      = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            _fontTexture.Name = "ImGui.NET Font Texture";
            gd.UpdateTexture(_fontTexture, (IntPtr)pixels, (uint)(bytesPerPixel * width * height), 0, 0, 0, (uint)width, (uint)height, 1, 0, 0);

            _fontTextureResourceSet?.Dispose();
            _fontTextureResourceSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_textureLayout, _fontTexture));

            io.Fonts.ClearTexData();
        }

        /// <summary>
        /// Frees all graphics resources used by the renderer.
        /// </summary>
        public void Dispose() {
            this._view.Resize      -= this.WindowResized;
            this._keyboard.KeyChar -= this.OnKeyChar;

            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _projMatrixBuffer.Dispose();
            _fontTexture.Dispose();
            _vertexShader.Dispose();
            _fragmentShader.Dispose();
            _layout.Dispose();
            _textureLayout.Dispose();
            _pipeline.Dispose();
            _mainResourceSet.Dispose();
            _fontTextureResourceSet.Dispose();

            foreach (IDisposable resource in _ownedResources) {
                resource.Dispose();
            }
        }

        private struct ResourceSetInfo {
            public readonly IntPtr      ImGuiBinding;
            public readonly ResourceSet ResourceSet;

            public ResourceSetInfo(IntPtr imGuiBinding, ResourceSet resourceSet) {
                ImGuiBinding = imGuiBinding;
                ResourceSet  = resourceSet;
            }
        }
    }
}
