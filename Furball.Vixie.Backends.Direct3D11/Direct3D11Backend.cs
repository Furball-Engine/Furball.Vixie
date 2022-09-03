using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Direct3D11.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Kettu;
using SharpGen.Runtime;
using Silk.NET.Input;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Debug;
using Vortice.DXGI;
using Vortice.Mathematics;
using Configuration=SixLabors.ImageSharp.Configuration;
using FeatureLevel=Vortice.Direct3D.FeatureLevel;

namespace Furball.Vixie.Backends.Direct3D11;

public class Direct3D11Backend : GraphicsBackend {
    public ID3D11Debug DebugDevice { get; private set; }
    public ID3D11Device Device { get; private set; }
    public ID3D11DeviceContext DeviceContext { get; private set; }
    public IDXGISwapChain SwapChain { get; private set; }
    public ID3D11RenderTargetView RenderTarget { get; private set; }
    public ID3D11Texture2D BackBuffer { get; private set; }
    public ID3D11Debug Debug { get; private set; }
    public ID3D11BlendState DefaultBlendState { get; private set; }

    private Color4 _clearColor;
    public Viewport Viewport { get; private set; }
    public Matrix4x4 ProjectionMatrix { get; private set; }

    internal ID3D11RenderTargetView CurrentlyBoundTarget;

#if USE_IMGUI
    private ImGuiControllerD3D11 _imGuiController;
#endif

    private VixieTextureD3D11 _privateWhitePixelVixieTexture = null!;
    internal VixieTextureD3D11 GetPrivateWhitePixelTexture() => this._privateWhitePixelVixieTexture;

    public override void Initialize(IView view, IInputContext inputContext) {
        FeatureLevel        featureLevel = FeatureLevel.Level_11_0;
        DeviceCreationFlags deviceFlags  = DeviceCreationFlags.BgraSupport;

#if DEBUG
        deviceFlags |= DeviceCreationFlags.Debug;
#endif

        D3D11.D3D11CreateDevice(
        null,
        DriverType.Hardware,
        deviceFlags,
        new[] {
            featureLevel
        },
        out ID3D11Device device,
        out ID3D11DeviceContext context
        );
        this.Device        = device;
        this.DeviceContext = context;

#if DEBUG
        try {
            this.DebugDevice = this.Device.QueryInterface<ID3D11Debug>();
        }
        catch (SharpGenException) {
            Logger.Log(
            "Creation of Debug Interface failed! Debug Layer may not work as intended.",
            LoggerLevelD3D11.InstanceWarning
            );
        }
#endif

        IDXGIFactory3 dxgiFactory = this.Device.QueryInterface<IDXGIDevice>().GetParent<IDXGIAdapter>()
            .GetParent<IDXGIFactory3>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            goto skipAdapterPrint;

        int adapterCount = dxgiFactory.GetAdapterCount1();
        for (int i = 0; i < adapterCount; i++) {
            AdapterDescription description = dxgiFactory.GetAdapter(i).Description;

            long luid = description.Luid.LowPart | description.Luid.HighPart;

            string dedicatedSysMemMb = Math.Round((description.DedicatedSystemMemory / 1024.0) / 1024.0, 2)
                .ToString(CultureInfo.InvariantCulture);
            string dedicatedVidMemMb = Math.Round((description.DedicatedVideoMemory / 1024.0) / 1024.0, 2)
                .ToString(CultureInfo.InvariantCulture);
            string dedicatedShrMemMb = Math.Round((description.SharedSystemMemory / 1024.0) / 1024.0, 2)
                .ToString(CultureInfo.InvariantCulture);

            BackendInfoSection section = new($"Adapter [{i}]");
            section.Contents.Add(("Adapter Description", description.Description));
            section.Contents.Add(("Revision", description.Revision.ToString()));
            section.Contents.Add(("PCI Vendor ID", description.VendorId.ToString()));
            section.Contents.Add(("PCI Device ID", description.DeviceId.ToString()));
            section.Contents.Add(("PCI Subsystem ID", description.SubsystemId.ToString()));
            section.Contents.Add(("Locally Unique Identifier", luid.ToString()));
            section.Contents.Add(("Dedicated System Memory", $"{dedicatedSysMemMb}mb"));
            section.Contents.Add(("Dedicated Video Memory", $"{dedicatedVidMemMb}mb"));
            section.Contents.Add(("Dedicated Shared Memory", $"{dedicatedShrMemMb}mb"));
            this.InfoSections.Add(section);

            i++;
        }

        skipAdapterPrint:

        IntPtr outputWindow = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                                  ? view.Handle
                                  : view.Native!.Win32!.Value.Hwnd;

        SwapChainDescription1 swapChainDescription = new SwapChainDescription1 {
            Width  = view.FramebufferSize.X,
            Height = view.FramebufferSize.Y,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription {
                Count   = 1,
                Quality = 0
            },
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            SwapEffect  = SwapEffect.FlipDiscard,
            Flags       = SwapChainFlags.None,
        };

        SwapChainFullscreenDescription fullscreenDescription = new SwapChainFullscreenDescription {
            Windowed = true
        };

        this.SwapChain = dxgiFactory.CreateSwapChainForHwnd(
        this.Device,
        outputWindow,
        swapChainDescription,
        fullscreenDescription
        );

        this.CreateSwapchainResources();

        this._clearColor = new Color4(0.0f, 0.0f, 0.0f, 1.0f);

        RasterizerDescription rasterizerDescription = new RasterizerDescription {
            FillMode              = FillMode.Solid,
            CullMode              = CullMode.None,
            FrontCounterClockwise = true,
            DepthClipEnable       = false,
            ScissorEnable         = true,
            MultisampleEnable     = true,
            AntialiasedLineEnable = true
        };

        ID3D11RasterizerState rasterizerState = this.Device.CreateRasterizerState(rasterizerDescription);

        this.DeviceContext.RSSetState(rasterizerState);

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
                    RenderTargetWriteMask = ColorWriteEnable.All,
                }
            }
        };

        ID3D11BlendState blendState = this.Device.CreateBlendState(blendDescription);

        this.DeviceContext.OMSetBlendState(blendState, new Color4(0, 0, 0, 0));

        this.DefaultBlendState = blendState;

#if USE_IMGUI
        this._imGuiController = new ImGuiControllerD3D11(this, view, inputContext, null);
#endif

        this._privateWhitePixelVixieTexture = (VixieTextureD3D11)this.CreateWhitePixelTexture();

        this.InfoSections.ForEach(x => x.Log(LoggerLevelD3D11.InstanceInfo));

        this.ScissorRect = new Rectangle(0, 0, view.FramebufferSize.X, view.FramebufferSize.Y);
    }

    private void CreateSwapchainResources() {
        ID3D11Texture2D        backBuffer   = this.SwapChain.GetBuffer<ID3D11Texture2D>(0);
        ID3D11RenderTargetView renderTarget = this.Device.CreateRenderTargetView(backBuffer);

        this.RenderTarget = renderTarget;
        this.BackBuffer   = backBuffer;

        this.DeviceContext.OMSetRenderTargets(this.RenderTarget);
        this.CurrentlyBoundTarget = this.RenderTarget;
    }

    public void SetDefaultRenderTarget() {
        this.DeviceContext.OMSetRenderTargets(this.RenderTarget);
        this.CurrentlyBoundTarget = this.RenderTarget;
    }

    public void ResetBlendState() {
        this.DeviceContext.OMSetBlendState(this.DefaultBlendState, new Color4(0, 0, 0, 0));
    }

    private void DestroySwapchainResources() {
        this.RenderTarget.Dispose();
        this.BackBuffer.Dispose();
    }

    public void ReportLiveObjects() {
        this.DebugDevice.ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Detail);
    }

    public override void Cleanup() {
        this.Device.Dispose();
        this.DeviceContext.Dispose();
        this.SwapChain.Dispose();
        this.RenderTarget.Dispose();
        this.BackBuffer.Dispose();
        this.DefaultBlendState.Dispose();

        this.Debug?.Dispose();
    }

    public override void HandleFramebufferResize(int width, int height) {
        this.DeviceContext.Flush();

        this.DestroySwapchainResources();

        this.SwapChain.ResizeBuffers(2, width, height, Format.B8G8R8A8_UNorm, SwapChainFlags.None);

        this.Viewport = new Viewport(0, 0, width, height, 0, 1);

        this.DeviceContext.RSSetViewport(this.Viewport);

        this.ScissorRect = new Rectangle(0, 0, width, height);

        this.CreateSwapchainResources();

        this.SetProjectionMatrix(width, height, false);
    }

    public void SetProjectionMatrix(int width, int height, bool fbProjMatrix) {
        float right  = fbProjMatrix ? width : width / (float)height * 720f;
        float bottom = fbProjMatrix ? height : 720f;

        this.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, right, bottom, 0, 1f, 0f);
    }

    public override Renderer CreateRenderer() => new Direct3D11Renderer(this);

    public override int QueryMaxTextureUnits() {
        return 128;
    }

    public override void Clear() {
        this.DeviceContext.ClearRenderTargetView(this.CurrentlyBoundTarget, this._clearColor);
    }

    private bool _takeScreenshot;
    public override void TakeScreenshot() {
        this._takeScreenshot = true;
    }

    public override VixieTextureRenderTarget CreateRenderTarget(uint width, uint height) {
        return new VixieTextureRenderTargetD3D11(this, width, height);
    }

    public override VixieTexture CreateTextureFromByteArray(byte[] imageData, TextureParameters parameters = default) =>
        new VixieTextureD3D11(this, imageData, parameters);

    public override VixieTexture CreateTextureFromStream(Stream stream, TextureParameters parameters = default) =>
        new VixieTextureD3D11(this, stream, parameters);

    public override VixieTexture CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default) =>
        new VixieTextureD3D11(this, width, height, parameters);

    public override VixieTexture CreateWhitePixelTexture() {
        return new VixieTextureD3D11(this);
    }

#if USE_IMGUI
    public override void ImGuiUpdate(double deltaTime) {
        this._imGuiController.Update((float)deltaTime);
    }

    public override void ImGuiDraw(double deltaTime) {
        this._imGuiController.Render();
    }
#endif

    public override unsafe void Present() {
        if (this._takeScreenshot) {
            this._takeScreenshot = false;

            Texture2DDescription desc = this.BackBuffer.Description;
            desc.Format         = Format.B8G8R8A8_UNorm_SRgb;
            desc.Width          = this.BackBuffer.Description.Width;
            desc.Height         = this.BackBuffer.Description.Height;
            desc.Usage          = ResourceUsage.Staging;
            desc.CPUAccessFlags = CpuAccessFlags.Read;
            desc.BindFlags      = BindFlags.None;

            ID3D11Texture2D stagingTex = this.Device.CreateTexture2D(desc);

            this.DeviceContext.CopyResource(stagingTex, this.BackBuffer);

            MappedSubresource mapped  = this.DeviceContext.Map(stagingTex, 0);
            Span<Bgra32>      rawData = mapped.AsSpan<Bgra32>(stagingTex, 0, 0);

            Bgra32[] data = new Bgra32[desc.Width * desc.Height];

            //Copy the data into a contiguous array
            for (int i = 0; i < desc.Height; i++)
                rawData.Slice(i * (mapped.RowPitch / sizeof(Bgra32)), desc.Width).CopyTo(data.AsSpan(i * desc.Width));

            Image<Bgra32> image = Image.LoadPixelData(Configuration.Default, data, desc.Width, desc.Height);

            stagingTex.Dispose();

            this.InvokeScreenshotTaken(image);
        }

        this.SwapChain.Present(0, PresentFlags.None);
    }

    public override void BeginScene() {
        this.DeviceContext.OMSetRenderTargets(this.RenderTarget);
        this.DeviceContext.RSSetViewport(this.Viewport);
        this.DeviceContext.RSSetScissorRect(0, 0, (int)this.Viewport.Width, (int)this.Viewport.Height);
    }

    private Rectangle _currentScissorRect;
    public  bool      FbProjMatrix;

    public override Rectangle ScissorRect {
        get => this._currentScissorRect;
        set {
            this._currentScissorRect = value;

            this.DeviceContext.RSSetScissorRect(value.X, value.Y, value.Width, value.Height);
        }
    }

    internal void ResetScissorRect() {
        ScissorRect = ScissorRect;
    }

    public override void SetFullScissorRect() {
        this.ScissorRect = new Rectangle(0, 0, (int)this.Viewport.Width, (int)this.Viewport.Height);
    }
}