using System;
using System.IO;
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
        private DeviceDebug      _debug;

        private RawColor4 _clearColor;

        internal Device GetDevice() => this._device;
        internal DeviceContext GetDeviceContext() => this._deviceContext;

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

            this._clearColor = new RawColor4(0.0f, 0.0f, 0.0f, 1.0f);

#if DEBUG
            DeviceDebug debug = new DeviceDebug(device);
            this._debug = debug;

            InfoQueue queue = debug.QueryInterface<InfoQueue>();

            if (queue != null) {
                queue.GetBreakOnSeverity(MessageSeverity.Corruption);
                queue.GetBreakOnSeverity(MessageSeverity.Error);
                queue.GetBreakOnSeverity(MessageSeverity.Information);
                queue.GetBreakOnSeverity(MessageSeverity.Message);
                queue.GetBreakOnSeverity(MessageSeverity.Warning);
            }
#endif
        }

        private void CreateSwapchainResources() {
            Texture2D backBuffer = Resource.FromSwapChain<Texture2D>(this._swapChain, 0);
            RenderTargetView renderTarget = new RenderTargetView(this._device, backBuffer);

            this._renderTarget = renderTarget;
            this._deviceContext.OutputMerger.SetRenderTargets(this._renderTarget);
        }

        private void DestroySwapchainResources() {
            this._renderTarget.Dispose();
        }

        public override void Cleanup() {

        }

        private bool _first = true;

        public override void HandleWindowSizeChange(int width, int height) {
            if (this._first) {
                this._first = false;
                return;
            }

            this._deviceContext.Flush();

            this.DestroySwapchainResources();

            this._swapChain.ResizeBuffers(2, width, height, Format.B8G8R8A8_UNorm, SwapChainFlags.None);
            this._deviceContext.Rasterizer.SetViewport(0, 0, width, height);

            this.CreateSwapchainResources();
        }

        public override void HandleFramebufferResize(int width, int height) {
            HandleWindowSizeChange(width, height);
        }

        public override IQuadRenderer CreateTextureRenderer() {
            return null;
        }

        public override ILineRenderer CreateLineRenderer() {
            return null;
        }

        public override int QueryMaxTextureUnits() {
            return 32;
        }

        public override void Clear() {
            this._deviceContext.ClearRenderTargetView(this._renderTarget, this._clearColor);
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
