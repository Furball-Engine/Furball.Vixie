using System;
using System.IO;
using System.Numerics;
using Furball.Vixie.Graphics.Backends.Direct3D11.Abstractions;
using Furball.Vixie.Graphics.Renderers;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Silk.NET.Windowing;
using Device=SharpDX.Direct3D11.Device;
using InfoQueue=SharpDX.Direct3D11.InfoQueue;
using Resource=SharpDX.Direct3D11.Resource;

namespace Furball.Vixie.Graphics.Backends.Direct3D11 {
    public unsafe class Direct3D11Backend : GraphicsBackend {
        private Factory2         _dxgiFactory;
        private Device           _device;
        private DeviceContext    _deviceContext;
        private SwapChain1       _swapChain;
        private RenderTargetView _renderTarget;
        private Texture2D        _backBuffer;
        private DeviceDebug      _debug;

        private RawColor4    _clearColor;
        private RawViewportF _viewport;
        private Matrix4x4    _projectionMatrix;

        internal Device GetDevice() => this._device;
        internal DeviceContext GetDeviceContext() => this._deviceContext;
        internal Matrix4x4 GetProjectionMatrix() => this._projectionMatrix;

        public override unsafe void Initialize(IWindow window) {
            Factory2 dxgiFactory2 = new Factory2();

            FeatureLevel featureLevel = FeatureLevel.Level_11_0;
            DeviceCreationFlags deviceFlags = DeviceCreationFlags.BgraSupport;

#if DEBUG
            deviceFlags |= DeviceCreationFlags.Debug;
#endif

            Device device = new Device(DriverType.Hardware, deviceFlags, featureLevel);
            DeviceContext deviceContext = device.ImmediateContext;

            this._device        = device;
            this._deviceContext = deviceContext;

            SwapChainDescription1 swapChainDescription = new SwapChainDescription1 {
                Width  = window.Size.X,
                Height = window.Size.Y,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription {
                    Count = 1, Quality = 0
                },
                Usage       = Usage.RenderTargetOutput,
                BufferCount = 2,
                SwapEffect  = SwapEffect.FlipDiscard,
                Flags       = SwapChainFlags.None
            };

            SwapChainFullScreenDescription fullScreenDescription = new SwapChainFullScreenDescription {
                Windowed = true
            };

            SwapChain1 swapChain = new SwapChain1(dxgiFactory2, device, window.Native.Win32.Value.Hwnd, ref swapChainDescription, fullScreenDescription);

            this._swapChain = swapChain;

            this.CreateSwapchainResources();

            this._clearColor = new RawColor4(0.1f, 0.1f, 0.1f, 1.0f);

            RasterizerStateDescription rasterizerStateDescription = new RasterizerStateDescription {
                FillMode                 = FillMode.Solid,
                CullMode                 = CullMode.None,
                IsFrontCounterClockwise  = true,
                IsDepthClipEnabled       = false,
                IsScissorEnabled         = false,
                IsMultisampleEnabled     = true,
                IsAntialiasedLineEnabled = true,
            };

            deviceContext.Rasterizer.State = new RasterizerState(device, rasterizerStateDescription);

            BlendStateDescription blendStateDescription = BlendStateDescription.Default();
            blendStateDescription.IndependentBlendEnable                = false;
            blendStateDescription.RenderTarget[0].BlendOperation        = BlendOperation.Add;
            blendStateDescription.RenderTarget[0].AlphaBlendOperation   = BlendOperation.Add;

            blendStateDescription.RenderTarget[0].SourceAlphaBlend      = BlendOption.Zero;
            blendStateDescription.RenderTarget[0].SourceBlend           = BlendOption.SourceAlpha;

            blendStateDescription.RenderTarget[0].DestinationBlend      = BlendOption.InverseSourceAlpha;
            blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;

            blendStateDescription.RenderTarget[0].IsBlendEnabled        = true;

            blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

            BlendState blendState = new BlendState(device, blendStateDescription);

            deviceContext.OutputMerger.SetBlendState(blendState, new RawColor4(0, 0, 0, 0));
        }

        private void CreateSwapchainResources() {
            Texture2D backBuffer = Resource.FromSwapChain<Texture2D>(this._swapChain, 0);
            RenderTargetView renderTarget = new RenderTargetView(this._device, backBuffer);

            this._renderTarget = renderTarget;
            this._backBuffer   = backBuffer;

            this._deviceContext.OutputMerger.SetRenderTargets(this._renderTarget);
        }

        public void SetDefaultRenderTarget() {
            this._deviceContext.OutputMerger.SetRenderTargets(this._renderTarget);
        }

        private void DestroySwapchainResources() {
            this._renderTarget.Dispose();
            this._backBuffer.Dispose();
        }

        public override void Cleanup() {

        }

        public override void HandleWindowSizeChange(int width, int height) {
            this._deviceContext.Flush();

            this.DestroySwapchainResources();

            this._swapChain.ResizeBuffers(2, width, height, Format.B8G8R8A8_UNorm, SwapChainFlags.None);

            this._viewport = new RawViewportF {
                X        = 0,
                Y        = 0,
                Width    = width,
                Height   = height,
                MinDepth = 0.0f,
                MaxDepth = 1.0f
            };

            this._deviceContext.Rasterizer.SetViewport(this._viewport);

            this.CreateSwapchainResources();

            this._projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 1f, 0f);
        }

        public override void HandleFramebufferResize(int width, int height) {
            HandleWindowSizeChange(width, height);
        }

        public override IQuadRenderer CreateTextureRenderer() {
            return new QuadRendererD3D11(this);
        }

        public override ILineRenderer CreateLineRenderer() {
            return null;
        }

        public override int QueryMaxTextureUnits() {
            return 32;
        }

        public override void Clear() {
            this._deviceContext.OutputMerger.SetRenderTargets(this._renderTarget);
            this._deviceContext.ClearRenderTargetView(this._renderTarget, this._clearColor);
            this._deviceContext.Rasterizer.SetViewport(this._viewport);
        }

        public override TextureRenderTarget CreateRenderTarget(uint width, uint height) {
            return null;
        }

        public override Texture CreateTexture(byte[] imageData, bool qoi = false) {
            return new TextureD3D11(this, imageData, qoi);
        }

        public override Texture CreateTexture(Stream stream) {
            return new TextureD3D11(this, stream);
        }

        public override Texture CreateTexture(uint width, uint height) {
            return new TextureD3D11(this, width, height);
        }

        public override Texture CreateTexture(string filepath) {
            return new TextureD3D11(this, filepath);
        }

        public override Texture CreateWhitePixelTexture() {
            return new TextureD3D11(this);
        }

        public override void ImGuiUpdate(double deltaTime) {

        }

        public override void ImGuiDraw(double deltaTime) {

        }

        public override void Present() {
            this._swapChain.Present(0, PresentFlags.None);
        }
    }
}
