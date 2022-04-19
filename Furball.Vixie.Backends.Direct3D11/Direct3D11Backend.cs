using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using Furball.Vixie.Backends.Direct3D11.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Kettu;
using SharpGen.Runtime;
using Silk.NET.Input;
using Silk.NET.Windowing;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Debug;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Furball.Vixie.Backends.Direct3D11 {
    public class Direct3D11Backend : IGraphicsBackend {
        private ID3D11Device           _device;
        private ID3D11DeviceContext    _deviceContext;
        private IDXGISwapChain         _swapChain;
        private ID3D11RenderTargetView _renderTarget;
        private ID3D11Texture2D        _backBuffer;
        private ID3D11Debug            _debug;
        private ID3D11BlendState       _defaultBlendState;

        private Color4    _clearColor;
        private Viewport _viewport;
        private Matrix4x4    _projectionMatrix;

        internal ID3D11RenderTargetView CurrentlyBoundTarget;

        internal ID3D11Device GetDevice() => this._device;
        internal ID3D11DeviceContext GetDeviceContext() => this._deviceContext;
        internal Matrix4x4 GetProjectionMatrix() => this._projectionMatrix;

        private ImGuiControllerD3D11 _imGuiController;

        private TextureD3D11 _privateWhitePixelTexture;
        internal TextureD3D11 GetPrivateWhitePixelTexture() => this._privateWhitePixelTexture;

        public override void Initialize(IWindow window, IInputContext inputContext) {
            FeatureLevel featureLevel = FeatureLevel.Level_11_0;
            DeviceCreationFlags deviceFlags = DeviceCreationFlags.BgraSupport;

#if DEBUG
            deviceFlags |= DeviceCreationFlags.Debug;
#endif

            D3D11.D3D11CreateDevice(null, DriverType.Hardware, deviceFlags, new[] { featureLevel }, out this._device, out this._deviceContext);

#if DEBUG
            try {
                this._device.QueryInterface<ID3D11Debug>().ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Detail);
            }
            catch (SharpGenException) {
                Logger.Log("Creation of Debug Interface failed! Debug Layer may not work as intended.", LoggerLevelD3D11.InstanceWarning);
            }
#endif

            IDXGIFactory dxgiFactory = this._device.QueryInterface<IDXGIDevice>().GetParent<IDXGIAdapter>().GetParent<IDXGIFactory>();

            int i = 0;
            try {
                while (dxgiFactory.GetAdapter(i) != null) {
                    AdapterDescription description = dxgiFactory.GetAdapter(i).Description;

                    long luid = description.Luid.LowPart | description.Luid.HighPart;

                    string dedicatedSysMemMb = Math.Round((description.DedicatedSystemMemory / 1024.0) / 1024.0, 2).ToString(CultureInfo.InvariantCulture);
                    string dedicatedVidMemMb = Math.Round((description.DedicatedVideoMemory  / 1024.0) / 1024.0, 2).ToString(CultureInfo.InvariantCulture);
                    string dedicatedShrMemMb = Math.Round((description.SharedSystemMemory    / 1024.0) / 1024.0, 2).ToString(CultureInfo.InvariantCulture);

                    BackendInfoSection section = new BackendInfoSection($"Adapter [{i}]");
                    section.Contents.Add(("Adapter Description", description.Description));
                    section.Contents.Add(("Revision", description.Revision.ToString()));
                    section.Contents.Add(("PCI Vendor ID", description.VendorId.ToString()));
                    section.Contents.Add(("PCI Device ID", description.DeviceId.ToString()));
                    section.Contents.Add(("PCI Subsystem ID", description.SubsystemId.ToString()));
                    section.Contents.Add(("Locally Unique Identifier", luid.ToString()));
                    section.Contents.Add(("Dedicated System Memory", $"{dedicatedSysMemMb}mb"));
                    section.Contents.Add(("Dedicated Video Memory",  $"{dedicatedVidMemMb}mb"));
                    section.Contents.Add(("Dedicated Shared Memory", $"{dedicatedShrMemMb}mb"));
                    this.InfoSections.Add(section);

                    i++;
                }
            }catch { /* This crashes if you go beyond what adapters it has, instead of sensibly just returning null like it claims to do */ }

            SwapChainDescription swapChainDescription = new SwapChainDescription {
                BufferDescription = new ModeDescription {
                    Width  = window.Size.X,
                    Height = window.Size.Y,
                    Format = Format.R8G8B8A8_UNorm,
                },
                SampleDescription = new SampleDescription {
                    Count = 1, Quality = 0
                },
                BufferUsage  = Usage.RenderTargetOutput,
                BufferCount  = 2,
                SwapEffect   = SwapEffect.FlipDiscard,
                Flags        = SwapChainFlags.None,
                OutputWindow = window.Native.Win32.Value.Hwnd,
                Windowed     = true
            };

            IDXGISwapChain swapChain = dxgiFactory.CreateSwapChain(this._device, swapChainDescription);
            this._swapChain = swapChain;

            dxgiFactory.MakeWindowAssociation(window.Native.Win32.Value.Hwnd, WindowAssociationFlags.IgnoreAll);

            this.CreateSwapchainResources();

            this._clearColor = new Color4(0.0f, 0.0f, 0.0f, 1.0f);

            RasterizerDescription rasterizerDescription = new RasterizerDescription {
                FillMode              = FillMode.Solid,
                CullMode              = CullMode.None,
                FrontCounterClockwise = true,
                DepthClipEnable       = false,
                ScissorEnable         = false,
                MultisampleEnable     = true,
                AntialiasedLineEnable = true
            };

            ID3D11RasterizerState rasterizerState = this._device.CreateRasterizerState(rasterizerDescription);

            this._deviceContext.RSSetState(rasterizerState);

            BlendDescription blendDescription = new BlendDescription {
                AlphaToCoverageEnable  = false,
                IndependentBlendEnable = false,
                RenderTarget = new RenderTargetBlendDescription[] {
                    new RenderTargetBlendDescription {
                        IsBlendEnabled        = true,
                        SourceBlend           = Blend.SourceAlpha,
                        DestinationBlend      = Blend.InverseSourceAlpha,
                        BlendOperation        = BlendOperation.Add,
                        SourceBlendAlpha      = Blend.One,
                        DestinationBlendAlpha = Blend.InverseSourceAlpha,
                        BlendOperationAlpha   = BlendOperation.Add,
                        RenderTargetWriteMask = ColorWriteEnable.All, }
                }
            };

            ID3D11BlendState blendState = this._device.CreateBlendState(blendDescription);

            this._deviceContext.OMSetBlendState(blendState, new Color4(0, 0, 0, 0));

            this._defaultBlendState = blendState;

            this._imGuiController = new ImGuiControllerD3D11(this, window, inputContext, null);

            this._privateWhitePixelTexture = (TextureD3D11) this.CreateWhitePixelTexture();

            this.InfoSections.ForEach(x => x.Log(LoggerLevelD3D11.InstanceInfo));
        }

        private void CreateSwapchainResources() {
            ID3D11Texture2D backBuffer = this._swapChain.GetBuffer<ID3D11Texture2D>(0);
            ID3D11RenderTargetView renderTarget = this._device.CreateRenderTargetView(backBuffer);

            this._renderTarget = renderTarget;
            this._backBuffer   = backBuffer;

            this._deviceContext.OMSetRenderTargets(this._renderTarget);
            this.CurrentlyBoundTarget = this._renderTarget;
        }

        public void SetDefaultRenderTarget() {
            this._deviceContext.OMSetRenderTargets(this._renderTarget);
            this.CurrentlyBoundTarget = this._renderTarget;
        }

        public void ResetBlendState() {
            this._deviceContext.OMSetBlendState(this._defaultBlendState, new Color4(0, 0, 0, 0));
        }

        private void DestroySwapchainResources() {
            this._renderTarget.Dispose();
            this._backBuffer.Dispose();
        }

        public override void Cleanup() {
            _device.Dispose();
            _deviceContext.Dispose();
            _swapChain.Dispose();
            _renderTarget.Dispose();
            _backBuffer.Dispose();
            _debug.Dispose();
            _defaultBlendState.Dispose();
        }

        public override void HandleWindowSizeChange(int width, int height) {
            this._deviceContext.Flush();

            this.DestroySwapchainResources();

            this._swapChain.ResizeBuffers(2, width, height, Format.B8G8R8A8_UNorm, SwapChainFlags.None);

            this._viewport = new Viewport(0, 0, width, height, 0, 1);

            this._deviceContext.RSSetViewport(this._viewport);

            this.CreateSwapchainResources();

            this._projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 1f, 0f);
        }

        public override void HandleFramebufferResize(int width, int height) {
            this.HandleWindowSizeChange(width, height);
        }

        public override IQuadRenderer CreateTextureRenderer() {
            return new QuadRendererD3D11(this);
        }

        public override ILineRenderer CreateLineRenderer() {
            return new LineRendererD3D11(this);
        }

        public override int QueryMaxTextureUnits() {
            return 128;
        }

        public override void Clear() {
            this._deviceContext.ClearRenderTargetView(this.CurrentlyBoundTarget, this._clearColor);
        }

        public override TextureRenderTarget CreateRenderTarget(uint width, uint height) {
            return new TextureRenderTargetD3D11(this, width, height);
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
            this._imGuiController.Update((float) deltaTime);
        }

        public override void ImGuiDraw(double deltaTime) {
            this._imGuiController.Render();
        }

        public override void Present() {
            this._swapChain.Present(0, PresentFlags.None);
        }

        public override void BeginScene() {
            this._deviceContext.OMSetRenderTargets(this._renderTarget);
            //this._deviceContext.ClearRenderTargetView(this._renderTarget, this._clearColor);
            this._deviceContext.RSSetViewport(this._viewport);
        }
    }
}
