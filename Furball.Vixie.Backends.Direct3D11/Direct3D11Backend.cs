using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Direct3D11.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Kettu;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Configuration = SixLabors.ImageSharp.Configuration;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace Furball.Vixie.Backends.Direct3D11;

public unsafe class Direct3D11Backend : GraphicsBackend {
    public ComPtr<ID3D11Debug>            DebugDevice       = null!;
    private ComPtr<ID3D11InfoQueue>       InfoQueue;
    public ComPtr<ID3D11Device>           Device            = null!;
    public ComPtr<ID3D11DeviceContext>    DeviceContext     = null!;
    public ComPtr<IDXGISwapChain1>        SwapChain         = null!;
    public ComPtr<ID3D11RenderTargetView> RenderTarget      = null!;
    public ComPtr<ID3D11Texture2D>        BackBuffer        = null!;
    public ComPtr<ID3D11BlendState>       DefaultBlendState = null!;

    private ComPtr<IDXGIFactory3> _dxgiFactory;

    private D3D11 d3d11;
    private DXGI  dxgi;

    private D3Dcolorvalue _clearColor;

    public Viewport  Viewport         { get; private set; }
    public Matrix4x4 ProjectionMatrix { get; private set; }

    internal ComPtr<ID3D11RenderTargetView> CurrentlyBoundTarget = null!;

#if USE_IMGUI
    private ImGuiControllerD3D11 _imGuiController = null!;
#endif

    private  VixieTextureD3D11 _privateWhitePixelVixieTexture = null!;
    internal VixieTextureD3D11 GetPrivateWhitePixelTexture() => this._privateWhitePixelVixieTexture;

    public override void Initialize(IView view, IInputContext inputContext) {
        this.d3d11 = D3D11.GetApi();
        this.dxgi  = DXGI.GetApi();

        D3DFeatureLevel  featureLevel = D3DFeatureLevel.Level110;
        CreateDeviceFlag deviceFlags  = CreateDeviceFlag.BgraSupport;

#if DEBUG
        deviceFlags |= CreateDeviceFlag.Debug;
#endif

        this.d3d11.CreateDevice(
            default(ComPtr<IDXGIAdapter>),
            D3DDriverType.Hardware,
            default(nint),
            (uint)deviceFlags,
            &featureLevel,
            1,
            D3D11.SdkVersion,
            ref this.Device,
            null,
            ref this.DeviceContext
        );

#if DEBUG
        try {
            this.DebugDevice = this.Device.QueryInterface<ID3D11Debug>();
            this.InfoQueue = this.Device.QueryInterface<ID3D11InfoQueue>();

            if (this.DebugDevice.Handle == null)
                throw new Exception("Unable to get Debug Device!");

            if (this.InfoQueue.Handle == null)
                throw new Exception("Unable to get info log");
        }
        catch {
            Logger.Log(
                "Creation of Debug Interface failed! Debug Layer may not work as intended.",
                LoggerLevelD3D11.InstanceWarning
            );
        }
#endif

        this._dxgiFactory = this.Device.QueryInterface<IDXGIDevice>().GetParent<IDXGIAdapter>()
                                .GetParent<IDXGIFactory3>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            goto skipAdapterPrint;

        try {
            ComPtr<IDXGIAdapter1> adapter = null;
            for (uint i = 0; i < this._dxgiFactory.EnumAdapters(i, ref adapter); i++) {
                AdapterDesc1 Desc = new AdapterDesc1();
                adapter.GetDesc1(&Desc);

#pragma warning disable CS0675
                long luid = Desc.AdapterLuid.Low | Desc.AdapterLuid.High;
#pragma warning restore CS0675

                string dedicatedSysMemMb = Math.Round((Desc.DedicatedSystemMemory / 1024.0) / 1024.0, 2)
                                               .ToString(CultureInfo.InvariantCulture);
                string dedicatedVidMemMb = Math.Round((Desc.DedicatedVideoMemory / 1024.0) / 1024.0, 2)
                                               .ToString(CultureInfo.InvariantCulture);
                string dedicatedShrMemMb = Math.Round((Desc.SharedSystemMemory / 1024.0) / 1024.0, 2)
                                               .ToString(CultureInfo.InvariantCulture);

                BackendInfoSection section = new($"Adapter [{i}]");
                section.Contents.Add(("Adapter Desc", SilkMarshal.PtrToString((nint)Desc.Description)));
                section.Contents.Add(("Revision", Desc.Revision.ToString()));
                section.Contents.Add(("PCI Vendor ID", Desc.VendorId.ToString()));
                section.Contents.Add(("PCI Device ID", Desc.DeviceId.ToString()));
                section.Contents.Add(("PCI Subsystem ID", Desc.SubSysId.ToString()));
                section.Contents.Add(("Locally Unique Identifier", luid.ToString()));
                section.Contents.Add(("Dedicated System Memory", $"{dedicatedSysMemMb}mb"));
                section.Contents.Add(("Dedicated Video Memory", $"{dedicatedVidMemMb}mb"));
                section.Contents.Add(("Dedicated Shared Memory", $"{dedicatedShrMemMb}mb"));
                this.InfoSections.Add(section);

                i++;
            }
        }
        catch {
            //this damn code breaks so much so lets shove it in a try/catch and cry about it
        }

    skipAdapterPrint:

        nint outputWindow = view.Native!.DXHandle!.Value;

        SwapChainDesc1 swapChainDesc = new() {
            Width  = (uint)view.FramebufferSize.X,
            Height = (uint)view.FramebufferSize.Y,
            Format = Format.FormatR8G8B8A8Unorm,
            SampleDesc = new SampleDesc {
                Count   = 1,
                Quality = 0
            },
            BufferUsage = DXGI.UsageRenderTargetOutput,
            BufferCount = 2,
            SwapEffect  = SwapEffect.FlipDiscard,
            Flags       = (uint)SwapChainFlag.None,
        };

        SwapChainFullscreenDesc fullscreenDesc = new() {
            Windowed = 1 //TODO: replace with true once https://github.com/dotnet/Silk.NET/pull/1157 is merged
        };

        this._dxgiFactory.CreateSwapChainForHwnd(
            this.Device,
            outputWindow,
            in swapChainDesc,
            in fullscreenDesc,
            ref Unsafe.NullRef<IDXGIOutput>(),
            ref this.SwapChain
        );

        this.CreateSwapchainResources();

        this._clearColor = new D3Dcolorvalue(0.0f, 0.0f, 0.0f, 1.0f);

        RasterizerDesc rasterizerDesc = new() {
            FillMode              = FillMode.Solid,
            CullMode              = CullMode.None,
            FrontCounterClockwise = 1, //true
            DepthClipEnable       = 0, //false
            ScissorEnable         = 1, //true
            MultisampleEnable     = 1, //true
            AntialiasedLineEnable = 1  //true
        };

        ComPtr<ID3D11RasterizerState> rasterizerState = null;
        this.Device.CreateRasterizerState(in rasterizerDesc, ref rasterizerState);

        this.DeviceContext.RSSetState(rasterizerState);

        BlendDesc blendDesc = new() {
            AlphaToCoverageEnable  = 0, //false
            IndependentBlendEnable = 0, //false
            RenderTarget = new BlendDesc.RenderTargetBuffer {
                Element0 = new() {
                    BlendEnable           = 1, //true
                    SrcBlend              = Blend.SrcAlpha,
                    DestBlend             = Blend.InvSrcAlpha,
                    BlendOp               = BlendOp.Add,
                    SrcBlendAlpha         = Blend.One,
                    DestBlendAlpha        = Blend.InvSrcAlpha,
                    BlendOpAlpha          = BlendOp.Add,
                    RenderTargetWriteMask = (byte)ColorWriteEnable.All,
                }
            }
        };

        this.Device.CreateBlendState(in blendDesc, ref this.DefaultBlendState);

        D3Dcolorvalue blendFactor = new D3Dcolorvalue(0, 0, 0, 0);
        this.DeviceContext.OMSetBlendState(this.DefaultBlendState, (float*)&blendFactor, 0xFFFFFFFF);
        // this.DeviceContext.OMSetBlendState(ref this.DefaultBlendState, blendColor);

#if USE_IMGUI
        this._imGuiController = new ImGuiControllerD3D11(this, view, inputContext, null);
#endif

        this._privateWhitePixelVixieTexture = (VixieTextureD3D11)this.CreateWhitePixelTexture();

        this.InfoSections.ForEach(x => x.Log(LoggerLevelD3D11.InstanceInfo));

        this.ScissorRect = new Rectangle(0, 0, view.FramebufferSize.X, view.FramebufferSize.Y);
    }

    public void PrintInfoLog() {
        ulong messages = this.InfoQueue.GetNumStoredMessages();
    
        for(ulong i = 0; i < messages; i++) {
            nuint length = 0;
            this.InfoQueue.GetMessageA(i, null, ref length);

            Message* message = (Message*)SilkMarshal.Allocate((int)length);

            this.InfoQueue.GetMessageA(i, message, ref length);

            Debug.WriteLine($"Debug Message: {SilkMarshal.PtrToString((nint)message->PDescription)}");

            SilkMarshal.Free((nint)message);
        }

        this.InfoQueue.ClearStoredMessages();
    }

    private void CreateSwapchainResources() {
        ComPtr<ID3D11Texture2D>        backBuffer = this.SwapChain.GetBuffer<ID3D11Texture2D>(0);
        ComPtr<ID3D11RenderTargetView> renderTarget = null;
        this.Device.CreateRenderTargetView(backBuffer, null, ref renderTarget);

        this.RenderTarget = renderTarget;
        this.BackBuffer   = backBuffer;

        this.DeviceContext.OMSetRenderTargets(1, this.RenderTarget, (ID3D11DepthStencilView*)null);
        this.CurrentlyBoundTarget = this.RenderTarget;
    }

    public void SetDefaultRenderTarget() {
        this.DeviceContext.OMSetRenderTargets(1, this.RenderTarget, (ID3D11DepthStencilView*)null);
        this.CurrentlyBoundTarget = this.RenderTarget;
    }

    public void ResetBlendState() {
        D3Dcolorvalue col = new D3Dcolorvalue(0, 0, 0, 0);
        this.DeviceContext.OMSetBlendState(this.DefaultBlendState, (float*)&col, 0xFFFFFFFF);
    }

    private void DestroySwapchainResources() {
        this.RenderTarget.Dispose();
        this.BackBuffer.Dispose();
    }

    public void ReportLiveObjects() {
        this.DebugDevice.ReportLiveDeviceObjects(RldoFlags.Detail);
    }

    public override void Cleanup() {
        this.Device.Dispose();
        this.DeviceContext.Dispose();
        this.SwapChain.Dispose();
        this.RenderTarget.Dispose();
        this.BackBuffer.Dispose();
        this.DefaultBlendState.Dispose();
    }

    public override void HandleFramebufferResize(int width, int height) {
        this.DeviceContext.Flush();

        this.DestroySwapchainResources();

        this.SwapChain.ResizeBuffers(2, (uint)width, (uint)height, Format.FormatB8G8R8A8Unorm, (uint)SwapChainFlag.None);

        this.Viewport = new Viewport(0, 0, width, height, 0, 1);

        this.DeviceContext.RSSetViewports(1, this.Viewport);

        this.ScissorRect = new Rectangle(0, 0, width, height);

        this.CreateSwapchainResources();

        this.SetProjectionMatrix(width, height, false);
    }

    public void SetProjectionMatrix(int width, int height, bool fbProjMatrix) {
        float right  = fbProjMatrix ? width : width / (float)height * 720f;
        float bottom = fbProjMatrix ? height : 720f;

        this.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, right, bottom, 0, 1f, 0f);
    }

    public override VixieRenderer CreateRenderer() => new Direct3D11VixieRenderer(this);

    //According to the docs, this is always 16384 for feature level 11 hardware: https://learn.microsoft.com/en-us/windows/win32/direct3d11/overviews-direct3d-11-resources-limits#resource-limits-for-feature-level-11-hardware
    public override Vector2D<int> MaxTextureSize => new Vector2D<int>(16384);

    public int QueryMaxTextureUnits() {
        return 128;
    }

    public override void Clear() {
        var col = this._clearColor;
        this.DeviceContext.ClearRenderTargetView(this.CurrentlyBoundTarget, (float*)&col);
    }

    private bool _takeScreenshot;
    public override void TakeScreenshot() {
        this._takeScreenshot = true;
    }

    public override VixieTextureRenderTarget CreateRenderTarget(uint width, uint height) {
        return new VixieTextureRenderTargetD3D11(this, width, height);
    }

    public override VixieTexture CreateTextureFromByteArray(byte[] imageData, TextureParameters parameters = default)
        => new VixieTextureD3D11(this, imageData, parameters);

    public override VixieTexture CreateTextureFromStream(Stream stream, TextureParameters parameters = default)
        => new VixieTextureD3D11(this, stream, parameters);

    public override VixieTexture CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default)
        => new VixieTextureD3D11(this, width, height, parameters);

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
    
    public static int CalculateMipSize(int mipLevel, int baseSize)
    {
        baseSize = baseSize >> mipLevel;
        return baseSize > 0 ? baseSize : 1;
    }

    public override unsafe void Present() {
        if (this._takeScreenshot) {
            this._takeScreenshot = false;

            Texture2DDesc desc = new Texture2DDesc();
            this.BackBuffer.GetDesc(ref desc);
            
            desc.Format         = Format.FormatB8G8R8A8UnormSrgb;
            // desc.Width          = this.BackBuffer.Desc.Width;
            // desc.Height         = this.BackBuffer.Desc.Height;
            desc.Usage          = Usage.Staging;
            desc.CPUAccessFlags = (uint)CpuAccessFlag.Read;
            desc.BindFlags      = (uint)BindFlag.None;

            ComPtr<ID3D11Texture2D> stagingTex = null;
            this.Device.CreateTexture2D(in desc, null, ref stagingTex);

            this.DeviceContext.CopyResource(stagingTex, this.BackBuffer);

            MappedSubresource mapped = new MappedSubresource();
            this.DeviceContext.Map(stagingTex, 0, Map.Read, 0, &mapped);
            Span<Bgra32> rawData = new Span<Bgra32>(mapped.PData, (int)(desc.Width * desc.Height));

            Bgra32[] data = new Bgra32[desc.Width * desc.Height];

            //Copy the data into a contiguous array
            for (int i = 0; i < desc.Height; i++)
                rawData.Slice((int)(i * (mapped.RowPitch / sizeof(Bgra32))), (int)desc.Width).CopyTo(data.AsSpan((int)(i * desc.Width)));

            Image<Bgra32> image = Image.LoadPixelData(Configuration.Default, data, (int)desc.Width, (int)desc.Height);

            stagingTex.Dispose();

            this.InvokeScreenshotTaken(image.CloneAs<Rgb24>());
        }

        this.SwapChain.Present(0, 0); //0 = PresentFlags.None
    }

    public override void BeginScene() {
        this.DeviceContext.OMSetRenderTargets(1, this.RenderTarget, (ID3D11DepthStencilView*)null);
        this.DeviceContext.RSSetViewports(1, this.Viewport);
        this.DeviceContext.RSSetScissorRects(1, new Box2D<int>(0, 0, (int)this.Viewport.Width, (int)this.Viewport.Height));
    }

    private Rectangle _currentScissorRect;
    public  bool      FbProjMatrix;

    public override Rectangle ScissorRect {
        get => this._currentScissorRect;
        set {
            this._currentScissorRect = value;

            this.DeviceContext.RSSetScissorRects(1, new Box2D<int>(value.X, value.Y, value.Width, value.Height));
        }
    }

    internal void ResetScissorRect() {
        ScissorRect = ScissorRect;
    }

    public override void SetFullScissorRect() {
        this.ScissorRect = new Rectangle(0, 0, (int)this.Viewport.Width, (int)this.Viewport.Height);
    }
    public override ulong GetVramUsage() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return 0;

        try {
            ComPtr<IDXGIAdapter3> adapter = null;
            this._dxgiFactory.EnumAdapters1(0, ref adapter);

            QueryVideoMemoryInfo info = new QueryVideoMemoryInfo();
            adapter.QueryVideoMemoryInfo(0u, MemorySegmentGroup.Local, ref info);

            return info.CurrentUsage;
        }
        catch {
            return 0;
        }
    }
    public override unsafe ulong GetTotalVram() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return 0;

        try {
            ComPtr<IDXGIAdapter4> adapter = null;
            this._dxgiFactory.EnumAdapters1(0, ref adapter);

            AdapterDesc3 desc = new AdapterDesc3();
            adapter.GetDesc3(ref desc);
            
            return desc.DedicatedVideoMemory;
        }
        catch {
            return 0;
        }
    }
}