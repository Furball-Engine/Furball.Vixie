// Direct3D11 + SharpDX implementation of a ImGuiController
// Adapted from Silk.NET's OpenGL ImGuiController
// License: https://github.com/dotnet/Silk.NET/blob/main/LICENSE.md

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Buffer=SharpDX.Direct3D11.Buffer;
using Device=SharpDX.Direct3D11.Device;
using MapFlags=SharpDX.Direct3D11.MapFlags;

namespace Furball.Vixie.Backends.Direct3D11 {
    public struct ImGuiFontConfig {
        public string FontPath;
        public int    FontSize;

        public ImGuiFontConfig(string fontPath, int fontSize) {
            this.FontPath = fontPath;
            this.FontSize = fontSize;
        }
    }

    public class ImGuiControllerD3D11 : IDisposable {
        private struct BackupDirect3D11State {
            public uint               ScissorRectsCount, ViewportsCount;
            public RawRectangle[]     ScissorRects;
            public RawViewport[]      Viewports;
            public RasterizerState    RasterizerState;
            public BlendState         BlendState;
            public RawColor4          BlendFactor;
            public int               SampleMask;
            public int                StencilRef;
            public DepthStencilState  DepthStencilState;
            public ShaderResourceView PixelShaderShaderResource;
            public SamplerState       PixelShaderSampler;
            public PixelShader        PixelShader;
            public VertexShader       VertexShader;
            public GeometryShader     GeometryShader;
            public uint               PixelShaderInstancesCount, VertexShaderInstancesCount, GeometryShaderInstancesCount;
            public ClassInstance[]    PixelShaderInstances;
            public ClassInstance[]    VertexShaderInstances;
            public ClassInstance[]    GeometryShaderInstances;
            public PrimitiveTopology  PrimitiveTopology;
            public Buffer             VertexBuffer,      IndexBuffer,        ConstantBuffer;
            public int               IndexBufferOffset, VertexBufferStride, VertexBufferOffset;
            public Format             IndexBufferFormat;
            public InputLayout        InputLayout;
        }

        private Direct3D11Backend _backend;
        private Device            _device;
        private DeviceContext     _deviceContext;

        private int _windowWidth;
        private int _windowHeight;

        private IView         _view;
        private IInputContext _inputContext;
        private IKeyboard     _keyboard;
        private List<char>    _pressedCharacters;

        private VertexShader       _vertexShader;
        private PixelShader        _pixelShader;
        private InputLayout        _inputLayout;
        private Buffer             _constantBuffer;
        private BlendState         _blendState;
        private RasterizerState    _rasterizerState;
        private DepthStencilState  _depthStencilState;
        private SamplerState       _samplerState;
        private ShaderResourceView _fontTextureShaderView;

        private Buffer _vertexBuffer;
        private int    _vertexBufferSize;

        private Buffer _indexBuffer;
        private int    _indexBufferSize;

        private bool _frameBegun;

        [StructLayout(LayoutKind.Sequential)]
        private struct ImGuiVertexData {
            public Vector2 Position;
            public Vector2 TexCoord;
            public Vector4 Color;
        }

        private struct ImGuiConstantBufferData {
            private Matrix4x4 ProjectionMatrix;
        }

        public ImGuiControllerD3D11(Direct3D11Backend backend, IView view, IInputContext context, ImGuiFontConfig? fontConfig, Action onConfigureIo = null) {
            this._backend       = backend;
            this._device        = backend.GetDevice();
            this._deviceContext = backend.GetDeviceContext();

            this._windowWidth = view.Size.X;
            this._windowHeight = view.Size.Y;

            this._pressedCharacters = new List<char>();

            ImGuiIOPtr io = ImGui.GetIO();

            if (fontConfig != null)
                io.Fonts.AddFontFromFileTTF(fontConfig.Value.FontPath, fontConfig.Value.FontSize);

            onConfigureIo?.Invoke();

            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

            this.SetKeyMappings();
            this.CreateObjects();

            this._view         = view;
            this._inputContext = context;
            this._windowWidth  = this._view.Size.X;
            this._windowHeight = this._view.Size.Y;

            IntPtr imGuiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(imGuiContext);
            ImGui.StyleColorsDark();

            ImGui.NewFrame();
            this._frameBegun = true;

            this._keyboard = this._inputContext.Keyboards[0];
            this._keyboard.KeyChar += this.OnKeyChar;

            this._view.Resize += this.OnViewResized;
        }

        private void OnKeyChar(IKeyboard keyboard, char character) {
            this._pressedCharacters.Add(character);
        }

        private void OnViewResized(Vector2D<int> newSize) {
            this._windowWidth  = this._view.Size.X;
            this._windowHeight = this._view.Size.Y;
        }

        private void SetPerFrameImGuiData(float delta) {
            ImGuiIOPtr io = ImGui.GetIO();

            io.DisplaySize = new Vector2(this._windowWidth, this._windowHeight);

            if (this._windowHeight > 0 && this._windowHeight > 0)
                io.DisplayFramebufferScale = new Vector2(this._view.FramebufferSize.X / this._windowWidth, this._view.FramebufferSize.Y / this._windowHeight);

            io.DeltaTime = delta;
        }

        private void UpdateImGuiInput() {
            ImGuiIOPtr io = ImGui.GetIO();

            MouseState mouse = this._inputContext.Mice[0].CaptureState();
            IKeyboard keyboard = this._inputContext.Keyboards[0];

            io.MouseDown[0] = mouse.IsButtonPressed(MouseButton.Left);
            io.MouseDown[1] = mouse.IsButtonPressed(MouseButton.Middle);
            io.MouseDown[2] = mouse.IsButtonPressed(MouseButton.Right);

            io.MousePos = mouse.Position;

            ScrollWheel wheel = mouse.GetScrollWheels()[0];

            io.MouseWheel  = wheel.Y;
            io.MouseWheelH = wheel.X;

            foreach (Key key in Enum.GetValues(typeof(Key))) {
                if(key == Key.Unknown)
                    continue;

                io.KeysDown[(int)key] = keyboard.IsKeyPressed(key);
            }

            for(int i = 0; i != this._pressedCharacters.Count; i++)
                io.AddInputCharacter(this._pressedCharacters[i]);

            this._pressedCharacters.Clear();

            io.KeyCtrl  = keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight);
            io.KeyAlt   = keyboard.IsKeyPressed(Key.AltLeft)     || keyboard.IsKeyPressed(Key.AltRight);
            io.KeyShift = keyboard.IsKeyPressed(Key.ShiftLeft)   || keyboard.IsKeyPressed(Key.ShiftRight);
            io.KeySuper = keyboard.IsKeyPressed(Key.SuperLeft)   || keyboard.IsKeyPressed(Key.SuperRight);
        }

        private unsafe void SetupRenderState(ImDrawDataPtr drawDataPtr, int frameBufferWidth, int frameBufferHeight) {
            RawViewportF viewport = new RawViewportF {
                Width    = drawDataPtr.DisplaySize.X,
                Height   = drawDataPtr.DisplaySize.Y,
                MinDepth = 0f,
                MaxDepth = 1f,
                X        = 0,
                Y        = 0
            };

            this._deviceContext.Rasterizer.SetViewport(viewport);

            this._deviceContext.InputAssembler.InputLayout = this._inputLayout;
            this._deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(this._vertexBuffer, sizeof(ImGuiVertexData), 0));
            this._deviceContext.InputAssembler.SetIndexBuffer(this._indexBuffer, Format.R32_UInt, 0);
            this._deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            this._deviceContext.VertexShader.Set(this._vertexShader);
            this._deviceContext.VertexShader.SetConstantBuffer(0, this._constantBuffer);

            this._deviceContext.PixelShader.Set(this._pixelShader);
            this._deviceContext.PixelShader.SetSampler(0, this._samplerState);

            RawColor4 blendFactor = new RawColor4(0.0f, 0.0f, 0.0f, 0.0f);

            this._deviceContext.OutputMerger.SetBlendState(this._blendState, blendFactor);
            this._deviceContext.OutputMerger.SetDepthStencilState(this._depthStencilState);
            this._deviceContext.Rasterizer.State = this._rasterizerState;
        }

        private unsafe void RenderImDrawData() {
            BackupDirect3D11State oldState = new BackupDirect3D11State();

            ImDrawDataPtr drawData = ImGui.GetDrawData();

            int framebufferWidth = (int) (drawData.DisplaySize.X  * drawData.FramebufferScale.X);
            int framebufferHeight = (int) (drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
            if (framebufferWidth <= 0 || framebufferHeight <= 0)
                return;

            if (drawData.DisplaySize.X <= 0.0f || drawData.DisplaySize.Y <= 0.0f)
                return;

            if (this._vertexBuffer == null || this._vertexBufferSize < drawData.TotalVtxCount) {
                this._vertexBuffer?.Dispose();

                this._vertexBufferSize = drawData.TotalVtxCount + 5000;

                BufferDescription vertexBufferDescription = new BufferDescription {
                    BindFlags = BindFlags.VertexBuffer,
                    Usage = ResourceUsage.Default,
                    SizeInBytes = this._vertexBufferSize * sizeof(ImGuiVertexData),
                };

                this._vertexBuffer = new Buffer(this._device, vertexBufferDescription);
            }

            if (this._indexBuffer == null || this._indexBufferSize < drawData.TotalIdxCount) {
                this._indexBuffer?.Dispose();

                this._indexBufferSize = drawData.TotalIdxCount + 10000;

                BufferDescription indexBufferDescription = new BufferDescription {
                    BindFlags = BindFlags.IndexBuffer,
                    Usage = ResourceUsage.Default,
                    SizeInBytes = this._indexBufferSize * sizeof(uint),
                };

                this._indexBuffer = new Buffer(this._device, indexBufferDescription);
            }

            for (int i = 0; i != drawData.CmdListsCount; i++) {
                ImDrawListPtr cmdListPtr = drawData.CmdListsRange[i];

                this._deviceContext.UpdateSubresource(new DataBox(cmdListPtr.VtxBuffer.Data), this._vertexBuffer);
                this._deviceContext.UpdateSubresource(new DataBox(cmdListPtr.IdxBuffer.Data), this._indexBuffer);
            }

            float l = drawData.DisplayPos.X;
            float r = drawData.DisplayPos.X + drawData.DisplaySize.X;
            float t = drawData.DisplayPos.Y;
            float b = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

            Matrix4x4 modelViewProjectionMatrix =
                new Matrix4x4(2.0f / (r - l),     0.0f,             0.0f, 0.0f,
                              0.0f,               2.0f / (t - b),   0.0f, 0.0f,
                              0.0f,               0.0f,             0.5f, 0.0f,
                              (r + l) / (l - r), (t + b) / (b - t), 0.5f, 1.0f);

            this._deviceContext.UpdateSubresource(ref modelViewProjectionMatrix, this._constantBuffer);

            oldState.ScissorRectsCount = oldState.ViewportsCount = 16;
            oldState.ScissorRects      = this._deviceContext.Rasterizer.GetScissorRectangles<RawRectangle>();
            oldState.Viewports         = this._deviceContext.Rasterizer.GetViewports<RawViewport>();
            oldState.RasterizerState   = this._deviceContext.Rasterizer.State;

            this._deviceContext.OutputMerger.GetBlendState(out oldState.BlendFactor, out oldState.SampleMask);

            oldState.DepthStencilState         = this._deviceContext.OutputMerger.GetDepthStencilState(out oldState.StencilRef);
            oldState.PixelShaderShaderResource = this._deviceContext.PixelShader.GetShaderResources(0, 1)[0];
            oldState.PixelShaderSampler        = this._deviceContext.PixelShader.GetSamplers(0, 1)[0];
            oldState.PixelShaderInstancesCount = oldState.VertexShaderInstancesCount = oldState.GeometryShaderInstancesCount = 256;
            oldState.PixelShader               = this._deviceContext.PixelShader.Get(oldState.PixelShaderInstances);
            oldState.VertexShader              = this._deviceContext.VertexShader.Get(oldState.VertexShaderInstances);
            oldState.GeometryShader            = this._deviceContext.GeometryShader.Get(oldState.GeometryShaderInstances);
            oldState.ConstantBuffer            = this._deviceContext.VertexShader.GetConstantBuffers(0, 1)[0];

            oldState.PrimitiveTopology = this._deviceContext.InputAssembler.PrimitiveTopology;

            this._deviceContext.InputAssembler.GetIndexBuffer(out oldState.IndexBuffer, out oldState.IndexBufferFormat, out oldState.IndexBufferOffset);

            Buffer[] vertexBuffers = Array.Empty<Buffer>();
            int[] strides = Array.Empty<int>();
            int[] offsets = Array.Empty<int>();

            this._deviceContext.InputAssembler.GetVertexBuffers(0, 1, vertexBuffers, strides, offsets);

            oldState.VertexBuffer       = vertexBuffers[0];
            oldState.VertexBufferStride = strides[0];
            oldState.VertexBufferOffset = offsets[0];

            oldState.InputLayout = this._deviceContext.InputAssembler.InputLayout;

            SetupRenderState(drawData, framebufferWidth, framebufferHeight);

            int indexOffset = 0, vertexOffset = 0;
            Vector2 clipOff = drawData.DisplayPos;

            for (int i = 0; i != drawData.CmdListsCount; i++) {
                ImDrawListPtr commandList = drawData.CmdListsRange[i];

                for(int cmd_i = 0; cmd_i < commandList.CmdBuffer.Size; cmd_i++) {
                    ImDrawCmdPtr pcmd = commandList.CmdBuffer[cmd_i];

                    if (pcmd.UserCallback != IntPtr.Zero)
                        throw new NotImplementedException();

                    Vector2 clipMin = new Vector2(pcmd.ClipRect.X - clipOff.X, pcmd.ClipRect.Y - clipOff.Y);
                    Vector2 clipMax = new Vector2(pcmd.ClipRect.Z - clipOff.X, pcmd.ClipRect.W - clipOff.Y);

                    if(clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y)
                        continue;

                    this._deviceContext.Rasterizer.SetScissorRectangle((int) clipMin.X, (int) clipMin.Y, (int) clipMax.X, (int) clipMax.Y);
                    //Sideeffect, only one font can be used, i dont wanna bother with this rn leave me alone thank you
                    this._deviceContext.PixelShader.SetShaderResource(0, this._fontTextureShaderView);
                    this._deviceContext.DrawIndexed((int) pcmd.ElemCount, (int) pcmd.IdxOffset + indexOffset, (int) pcmd.VtxOffset + vertexOffset);
                }

                indexOffset  += commandList.IdxBuffer.Size;
                vertexOffset += commandList.VtxBuffer.Size;
            }


        }

        public void Render() {
            if (this._frameBegun) {
                this._frameBegun = false;
                ImGui.Render();
                this.RenderImDrawData();
            }
        }

        public void Update(float delta) {
            if(this._frameBegun)
                ImGui.Render();

            this.SetPerFrameImGuiData(delta);
            this.UpdateImGuiInput();
        }

        ~ImGuiControllerD3D11() {
            DisposeQueue.Enqueue(this);
        }

        private void SetKeyMappings() {
            ImGuiIOPtr io = ImGui.GetIO();

            io.KeyMap[(int) ImGuiKey.Tab]        = (int) Key.Tab;
            io.KeyMap[(int) ImGuiKey.LeftArrow]  = (int) Key.Left;
            io.KeyMap[(int) ImGuiKey.RightArrow] = (int) Key.Right;
            io.KeyMap[(int) ImGuiKey.UpArrow]    = (int) Key.Up;
            io.KeyMap[(int) ImGuiKey.DownArrow]  = (int) Key.Down;
            io.KeyMap[(int) ImGuiKey.PageUp]     = (int) Key.PageUp;
            io.KeyMap[(int) ImGuiKey.PageDown]   = (int) Key.PageDown;
            io.KeyMap[(int) ImGuiKey.Home]       = (int) Key.Home;
            io.KeyMap[(int) ImGuiKey.End]        = (int) Key.End;
            io.KeyMap[(int) ImGuiKey.Delete]     = (int) Key.Delete;
            io.KeyMap[(int) ImGuiKey.Backspace]  = (int) Key.Backspace;
            io.KeyMap[(int) ImGuiKey.Enter]      = (int) Key.Enter;
            io.KeyMap[(int) ImGuiKey.Escape]     = (int) Key.Escape;
            io.KeyMap[(int) ImGuiKey.A]          = (int) Key.A;
            io.KeyMap[(int) ImGuiKey.C]          = (int) Key.C;
            io.KeyMap[(int) ImGuiKey.V]          = (int) Key.V;
            io.KeyMap[(int) ImGuiKey.X]          = (int) Key.X;
            io.KeyMap[(int) ImGuiKey.Y]          = (int) Key.Y;
            io.KeyMap[(int) ImGuiKey.Z]          = (int) Key.Z;
        }

        private unsafe void CreateObjects() {
            string shaderSourceCode = ResourceHelpers.GetStringResource("Shaders/ImGui/Shaders.hlsl");

            CompilationResult vertexShaderResult = ShaderBytecode.Compile(shaderSourceCode, "VS_Main", "vs_5_0", ShaderFlags.EnableStrictness, EffectFlags.None, Array.Empty<ShaderMacro>(), null, "VertexShader.hlsl");
            CompilationResult pixelShaderResult = ShaderBytecode.Compile(shaderSourceCode,  "PS_Main", "ps_5_0", ShaderFlags.EnableStrictness, EffectFlags.None, Array.Empty<ShaderMacro>(), null, "PixelShader.hlsl");

            VertexShader vertexShader = new VertexShader(this._device, vertexShaderResult.Bytecode.Data);
            PixelShader pixelShader = new PixelShader(this._device, pixelShaderResult.Bytecode.Data);

            InputElement[] inputLayoutDescription = new [] {
                new InputElement("POSITION", 0, Format.R32G32_Float, (int) Marshal.OffsetOf<ImGuiVertexData>("Position"), 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, (int) Marshal.OffsetOf<ImGuiVertexData>("TexCoord"), 0),
                new InputElement("COLOR",    0, Format.R32G32_Float, (int) Marshal.OffsetOf<ImGuiVertexData>("Color"),    0),
            };

            InputLayout inputLayout = new InputLayout(this._device, vertexShaderResult.Bytecode.Data, inputLayoutDescription);

            BufferDescription constantBufferDescription = new BufferDescription {
                BindFlags = BindFlags.ConstantBuffer,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.Write,
                SizeInBytes = sizeof(ImGuiConstantBufferData),
            };

            Buffer constantBuffer = new Buffer(this._device, constantBufferDescription);

            BlendStateDescription blendStateDescription = BlendStateDescription.Default();

            blendStateDescription.RenderTarget[0].IsBlendEnabled        = true;
            blendStateDescription.RenderTarget[0].SourceBlend           = BlendOption.SourceAlpha;
            blendStateDescription.RenderTarget[0].DestinationBlend      = BlendOption.InverseSourceAlpha;
            blendStateDescription.RenderTarget[0].BlendOperation        = BlendOperation.Add;
            blendStateDescription.RenderTarget[0].SourceAlphaBlend      = BlendOption.One;
            blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
            blendStateDescription.RenderTarget[0].AlphaBlendOperation   = BlendOperation.Add;
            blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

            BlendState blendState = new BlendState(this._device, blendStateDescription);

            RasterizerStateDescription rasterizerStateDescription = RasterizerStateDescription.Default();

            rasterizerStateDescription.FillMode           = FillMode.Solid;
            rasterizerStateDescription.CullMode           = CullMode.None;
            rasterizerStateDescription.IsScissorEnabled   = true;
            rasterizerStateDescription.IsDepthClipEnabled = true;

            RasterizerState rasterizerState = new RasterizerState(this._device, rasterizerStateDescription);

            DepthStencilStateDescription depthStencilStateDescription = new DepthStencilStateDescription {
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Always,
                IsStencilEnabled = false,
                FrontFace = new DepthStencilOperationDescription {
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Keep,
                },
                BackFace = new DepthStencilOperationDescription {
                    FailOperation      = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Keep,
                    PassOperation      = StencilOperation.Keep,
                }
            };

            DepthStencilState depthStencilState = new DepthStencilState(this._device, depthStencilStateDescription);

            SamplerStateDescription samplerStateDescription = new SamplerStateDescription {
                Filter             = Filter.MinMagMipLinear,
                AddressU           = TextureAddressMode.Wrap,
                AddressV           = TextureAddressMode.Wrap,
                AddressW           = TextureAddressMode.Wrap,
                MipLodBias         = 0.0f,
                ComparisonFunction = Comparison.Always,
                MinimumLod         = 0.0f,
                MaximumLod         = 0.0f
            };

            SamplerState samplerState = new SamplerState(this._device, samplerStateDescription);

            this._vertexShader      = vertexShader;
            this._pixelShader       = pixelShader;
            this._constantBuffer    = constantBuffer;
            this._blendState        = blendState;
            this._rasterizerState   = rasterizerState;
            this._depthStencilState = depthStencilState;
            this._inputLayout       = inputLayout;
            this._samplerState      = samplerState;

            this.CreateFontTexture();
        }

        private unsafe void CreateFontTexture() {
            ImGuiIOPtr io = ImGui.GetIO();

            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

            Texture2DDescription texture2DDescription = new Texture2DDescription {
                Width     = width,
                Height    = height,
                MipLevels = 1,
                ArraySize = 1,
                Format    = Format.R8G8B8A8_UNorm,
                SampleDescription = new SampleDescription {
                    Count = 1, Quality = 0
                },
                Usage     = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource
            };

            Texture2D fontTexture = new Texture2D(this._device, texture2DDescription);

            this._deviceContext.UpdateSubresource(new DataBox(pixels), fontTexture);

            ShaderResourceViewDescription shaderResourceViewDescription = new ShaderResourceViewDescription {
                Format = Format.R8G8B8A8_UNorm,
                Dimension = ShaderResourceViewDimension.Texture2D,
            };

            shaderResourceViewDescription.Texture2D.MipLevels       = 1;
            shaderResourceViewDescription.Texture2D.MostDetailedMip = 0;

            ShaderResourceView shaderResourceView = new ShaderResourceView(this._device, fontTexture, shaderResourceViewDescription);

            this._fontTextureShaderView = shaderResourceView;

            io.Fonts.SetTexID(shaderResourceView.NativePointer);
        }

        private bool _isDisposed = false;

        public void Dispose() {
            if(this._isDisposed)
                return;

            this._isDisposed = true;

            try {
                this._device?.Dispose();
                this._deviceContext?.Dispose();
                this._vertexShader?.Dispose();
                this._pixelShader?.Dispose();
                this._inputLayout?.Dispose();
                this._constantBuffer?.Dispose();
                this._blendState?.Dispose();
                this._rasterizerState?.Dispose();
                this._depthStencilState?.Dispose();
                this._samplerState?.Dispose();
            }
            catch {

            }
        }
    }
}
