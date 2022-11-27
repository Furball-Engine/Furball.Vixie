using System.Runtime.CompilerServices;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Rectangle=SixLabors.ImageSharp.Rectangle;

namespace Furball.Vixie.Backends.Direct3D12;

public unsafe class Direct3D12Backend : GraphicsBackend {
    public D3D12                 D3D12 = null!;
    public DXGI                  DXGI  = null!;
    public ComPtr<ID3D12Device>  Device;
    public ComPtr<IDXGIFactory4> DXGIFactory;

    public ComPtr<ID3D12Debug1>      Debug;
    public ComPtr<ID3D12DebugDevice> DebugDevice;
    public ComPtr<ID3D12InfoQueue>   DebugInfoQueue;

    public  ComPtr<ID3D12CommandQueue>     CommandQueue;
    private ComPtr<ID3D12CommandAllocator> CommandAllocator;

    private ComPtr<ID3D12Fence> Fence;

    public override void Initialize(IView view, IInputContext inputContext) {
        //Get the D3D12 and DXGI APIs
        this.D3D12 = D3D12.GetApi();
        this.DXGI  = DXGI.GetApi();

        uint factoryFlags = 0;
#if DEBUG
        //If we are on debug, mark to create a debug factory
        factoryFlags |= DXGI.CreateFactoryDebug;
#endif

        this.DXGIFactory = this.DXGI.CreateDXGIFactory2<IDXGIFactory4>(factoryFlags);

#if DEBUG
        this.CreateDebugAndInfoQueue();

        //Enable the debug layer, and GPU based validation
        this.Debug.EnableDebugLayer();
        this.Debug.SetEnableGPUBasedValidation(true);
#endif

        //Create a new device with feature level 12.0, and no specified adapter
        this.Device = this.D3D12.CreateDevice<ID3D12Device>(ref Unsafe.NullRef<IUnknown>(), D3DFeatureLevel.Level120);

        //Get the devices debug device
        this.DebugDevice = this.Device.QueryInterface<ID3D12DebugDevice>();

        //Create the command queue we will use throughout the application
        this.CreateCommandQueueAndAllocator();

        //Create the fence to do CPU/GPU synchronization
        this.CreateFence();
    }

    private void CreateFence() {
        this.Fence = this.Device.CreateFence<ID3D12Fence>(0, FenceFlags.None);
    }

    private void CreateDebugAndInfoQueue() {
        this.Debug = this.D3D12.GetDebugInterface<ID3D12Debug1>();
        // this.DebugInfoQueue = this.D3D12.GetDebugInterface<ID3D12InfoQueue>();
    }

    private void CreateCommandQueueAndAllocator() {
        CommandQueueDesc commandQueueDesc = new CommandQueueDesc {
            Flags = CommandQueueFlags.None,
            Type  = CommandListType.Direct
        };
        this.CommandQueue = this.Device.CreateCommandQueue<ID3D12CommandQueue>(&commandQueueDesc);

        this.CommandAllocator = this.Device.CreateCommandAllocator<ID3D12CommandAllocator>(CommandListType.Direct);
    }

    public override void Cleanup() {
        //Release our device and DXGI factory
        this.Device.Release();
        this.Debug.Release();
        this.DebugInfoQueue.Release();
        this.DXGIFactory.Release();
    }

    public override void HandleFramebufferResize(int width, int height) {
        throw new NotImplementedException();
    }

    public override VixieRenderer CreateRenderer() {
        throw new NotImplementedException();
    }

    public override Vector2D<int> MaxTextureSize { get; }

    public override void Clear() {
        throw new NotImplementedException();
    }

    public override void TakeScreenshot() {
        throw new NotImplementedException();
    }

    public override Rectangle ScissorRect { get; set; }

    public override void SetFullScissorRect() {
        throw new NotImplementedException();
    }

    public override ulong GetVramUsage() {
        throw new NotImplementedException();
    }

    public override ulong GetTotalVram() {
        throw new NotImplementedException();
    }

    public override VixieTextureRenderTarget CreateRenderTarget(uint width, uint height) {
        throw new NotImplementedException();
    }

    public override VixieTexture CreateTextureFromByteArray(byte[] imageData, TextureParameters parameters = default) {
        throw new NotImplementedException();
    }

    public override VixieTexture CreateTextureFromStream(Stream stream, TextureParameters parameters = default) {
        throw new NotImplementedException();
    }

    public override VixieTexture CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default) {
        throw new NotImplementedException();
    }

    public override VixieTexture CreateWhitePixelTexture() {
        throw new NotImplementedException();
    }

    public override void ImGuiUpdate(double deltaTime) {
        throw new NotImplementedException();
    }

    public override void ImGuiDraw(double deltaTime) {
        throw new NotImplementedException();
    }
}