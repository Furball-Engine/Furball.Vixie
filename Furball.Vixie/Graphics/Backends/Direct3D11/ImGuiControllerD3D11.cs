// Direct3D11 + SharpDX implementation of a ImGuiController
// Adapted from Silk.NET's OpenGL ImGuiController
// License: https://github.com/dotnet/Silk.NET/blob/main/LICENSE.md

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Helpers;
using ImGuiNET;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Silk.NET.Input;
using Silk.NET.Windowing;
using Buffer=SharpDX.Direct3D11.Buffer;
using Device=SharpDX.Direct3D11.Device;

namespace Furball.Vixie.Graphics.Backends.Direct3D11 {
    public struct ImGuiFontConfig {
        public string FontPath;
        public int    FontSize;

        public ImGuiFontConfig(string fontPath, int fontSize) {
            this.FontPath = fontPath;
            this.FontSize = fontSize;
        }
    }

    public class ImGuiControllerD3D11 {
        private Direct3D11Backend _backend;
        private Device            _device;
        private DeviceContext     _deviceContext;

        private int _windowWidth;
        private int _windowHeight;

        private VertexShader      _vertexShader;
        private PixelShader       _pixelShader;
        private InputLayout       _inputLayout;
        private Buffer            _constantBuffer;
        private BlendState        _blendState;
        private RasterizerState   _rasterizerState;
        private DepthStencilState _depthStencilState;

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

            ImGuiIOPtr io = ImGui.GetIO();

            if (fontConfig != null)
                io.Fonts.AddFontFromFileTTF(fontConfig.Value.FontPath, fontConfig.Value.FontSize);

            onConfigureIo?.Invoke();

            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

            this.SetKeyMappings();
            this.CreateObjects();
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
            string shaderSourceCode = ResourceHelpers.GetStringResource("ShaderCode/Direct3D11/ImGui/Shaders.hlsl", true);

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

            this._vertexShader      = vertexShader;
            this._pixelShader       = pixelShader;
            this._constantBuffer    = constantBuffer;
            this._blendState        = blendState;
            this._rasterizerState   = rasterizerState;
            this._depthStencilState = depthStencilState;
            this._inputLayout       = inputLayout;

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

            io.Fonts.SetTexID(shaderResourceView.NativePointer);
        }
    }
}
