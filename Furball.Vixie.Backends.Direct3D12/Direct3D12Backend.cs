using System.Runtime.CompilerServices;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Buffer=System.Buffer;
using Rectangle=SixLabors.ImageSharp.Rectangle;

namespace Furball.Vixie.Backends.Direct3D12;

public unsafe class Direct3D12Backend : GraphicsBackend {
    public D3D12       D3D12       = null!;
    public DXGI        DXGI        = null!;
    public D3DCompiler D3DCompiler = null!;

    public  ComPtr<ID3D12Device>        Device;
    public  ComPtr<IDXGIFactory4>       DXGIFactory;
    public  ComPtr<ID3D12PipelineState> PipelineState;
    private ComPtr<ID3D12RootSignature> RootSignature;

    private IView _view = null!;

    public ComPtr<ID3D12Debug1>      Debug;
    public ComPtr<ID3D12DebugDevice> DebugDevice;
    public ComPtr<ID3D12InfoQueue>   DebugInfoQueue;

    public ComPtr<ID3D12CommandQueue>         CommandQueue;
    public ComPtr<ID3D12CommandAllocator>     CommandAllocator;
    public ComPtr<ID3D12GraphicsCommandList> CommandList;

    public uint                FrameIndex;
    public void*               FenceEvent;
    public ulong               FenceValue;
    public ComPtr<ID3D12Fence> Fence;

    const uint BackbufferCount = 2;

    private uint                         _currentBuffer = 0;
    private ComPtr<ID3D12DescriptorHeap> _renderTargetViewHeap;
    private uint                         _rtvDescriptorSize;
    private ComPtr<ID3D12Resource>[]     _renderTargets = new ComPtr<ID3D12Resource>[BackbufferCount];

    private ComPtr<IDXGISwapChain3> _swapchain;
    private Viewport                _viewport;
    private Box2D<long>             _surfaceSize;

    private Box2D<int>          CurrentScissorRect;
    private CpuDescriptorHandle _currentRtvHandle;

    public override void Initialize(IView view, IInputContext inputContext) {
        //Get the D3D12 and DXGI APIs
        this.D3D12       = D3D12.GetApi();
        this.DXGI        = DXGI.GetApi();
        this.D3DCompiler = D3DCompiler.GetApi();

        view.ShouldSwapAutomatically  = false;
        view.IsContextControlDisabled = true;

        this._view = view;

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

#if DEBUG
        //Get the devices debug device
        this.DebugDevice = this.Device.QueryInterface<ID3D12DebugDevice>();

        //Get the debug info queue
        this.DebugInfoQueue = this.Device.QueryInterface<ID3D12InfoQueue>();
#endif

        try {
            //Create the command queue we will use throughout the application
            this.CreateCommandQueueAndAllocator();
        }
        catch {
            this.PrintInfoQueue();
            throw;
        }

        try {
            //Create the fence to do CPU/GPU synchronization
            this.CreateFence();
        }
        catch {
            this.PrintInfoQueue();
            throw;
        }

        try {
            //Create the swapchain we render to
            this.CreateSwapchain();
        }
        catch {
            this.PrintInfoQueue();
            throw;
        }

        try {
            //Create the shader we use for rendering
            this.CreatePipelineState();
        }
        catch {
            this.PrintInfoQueue();
            throw;
        }

#if USE_IMGUI
        throw new NotImplementedException("ImGui is not implemented on Direct3D12!");
#endif
    }

    public void PrintInfoQueue() {
        if (this.DebugInfoQueue.Handle == null)
            return;

        ulong messages = this.DebugInfoQueue.GetNumStoredMessages();

        for (ulong i = 0; i < messages; i++) {
            nuint length = 0;
            this.DebugInfoQueue.GetMessageA(i, null, ref length);

            Message* message = (Message*)SilkMarshal.Allocate((int)length);

            this.DebugInfoQueue.GetMessageA(i, message, ref length);

            Console.WriteLine($"Debug Message: {SilkMarshal.PtrToString((nint)message->PDescription)}");

            SilkMarshal.Free((nint)message);
        }

        this.DebugInfoQueue.ClearStoredMessages();
    }

    private void CreatePipelineState() {
        byte[] vertexShaderDxil = ResourceHelpers.GetByteResource(@"Shaders/VertexShader.dxil", typeof(Direct3D12Backend));
        byte[] pixelShaderDxil  = ResourceHelpers.GetByteResource(@"Shaders/PixelShader.dxil", typeof(Direct3D12Backend));

        ComPtr<ID3D10Blob> vertexShaderBlob = null;
        ComPtr<ID3D10Blob> pixelShaderBlob  = null;
        SilkMarshal.ThrowHResult(this.D3DCompiler.CreateBlob((nuint)vertexShaderDxil.Length, ref vertexShaderBlob));
        SilkMarshal.ThrowHResult(this.D3DCompiler.CreateBlob((nuint)pixelShaderDxil.Length, ref pixelShaderBlob));

        //Copy the shader to the blob
        fixed (void* ptr = vertexShaderDxil)
            Buffer.MemoryCopy(ptr, vertexShaderBlob.GetBufferPointer(), vertexShaderDxil.Length, vertexShaderDxil.Length);
        fixed (void* ptr = pixelShaderDxil)
            Buffer.MemoryCopy(ptr, pixelShaderBlob.GetBufferPointer(), pixelShaderDxil.Length, pixelShaderDxil.Length);

        GraphicsPipelineStateDesc desc = new GraphicsPipelineStateDesc {
            VS = new ShaderBytecode {
                BytecodeLength  = vertexShaderBlob.GetBufferSize(),
                PShaderBytecode = vertexShaderBlob.GetBufferPointer()
            },
            PS = new ShaderBytecode {
                BytecodeLength  = pixelShaderBlob.GetBufferSize(),
                PShaderBytecode = pixelShaderBlob.GetBufferPointer()
            }
        };

        const int         inputElementDescCount   = 5;
        InputElementDesc* inputElementDescriptors = stackalloc InputElementDesc[inputElementDescCount];
        inputElementDescriptors[0] = new InputElementDesc {
            SemanticName         = (byte*)SilkMarshal.StringToPtr("POSITION"),
            AlignedByteOffset    = D3D12.AppendAlignedElement,
            Format               = Format.FormatR32G32Float,
            InputSlot            = 0,
            SemanticIndex        = 0,
            InputSlotClass       = InputClassification.PerVertexData,
            InstanceDataStepRate = 0
        };
        inputElementDescriptors[1] = new InputElementDesc {
            SemanticName         = (byte*)SilkMarshal.StringToPtr("TEXCOORD"),
            AlignedByteOffset    = D3D12.AppendAlignedElement,
            Format               = Format.FormatR32G32Float,
            InputSlot            = 0,
            SemanticIndex        = 0,
            InputSlotClass       = InputClassification.PerVertexData,
            InstanceDataStepRate = 0
        };
        inputElementDescriptors[2] = new InputElementDesc {
            SemanticName         = (byte*)SilkMarshal.StringToPtr("COLOR"),
            AlignedByteOffset    = D3D12.AppendAlignedElement,
            Format               = Format.FormatR32G32B32A32Float,
            InputSlot            = 0,
            SemanticIndex        = 0,
            InputSlotClass       = InputClassification.PerVertexData,
            InstanceDataStepRate = 0
        };
        inputElementDescriptors[3] = new InputElementDesc {
            SemanticName         = (byte*)SilkMarshal.StringToPtr("TEXID"),
            AlignedByteOffset    = D3D12.AppendAlignedElement,
            Format               = Format.FormatR32Uint,
            InputSlot            = 0,
            SemanticIndex        = 0,
            InputSlotClass       = InputClassification.PerVertexData,
            InstanceDataStepRate = 0
        };
        inputElementDescriptors[4] = new InputElementDesc {
            SemanticName         = (byte*)SilkMarshal.StringToPtr("TEXID"),
            AlignedByteOffset    = D3D12.AppendAlignedElement,
            Format               = Format.FormatR32Uint,
            InputSlot            = 0,
            SemanticIndex        = 1,
            InputSlotClass       = InputClassification.PerVertexData,
            InstanceDataStepRate = 0
        };

        desc.InputLayout.PInputElementDescs = inputElementDescriptors;
        desc.InputLayout.NumElements        = inputElementDescCount;

        desc.PRootSignature = this.RootSignature = this.Device.CreateRootSignature<ID3D12RootSignature>(0, vertexShaderBlob.GetBufferPointer(), vertexShaderBlob.GetBufferSize());

        desc.RasterizerState = new RasterizerDesc {
            FillMode              = FillMode.Solid,
            CullMode              = CullMode.None, //TODO: this should not be None
            FrontCounterClockwise = false,
            DepthBias             = D3D12.DefaultDepthBias,
            DepthBiasClamp        = 0, //D3D12_DEFAULT_DEPTH_BIAS_CLAMP
            SlopeScaledDepthBias  = 0, //D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS
            DepthClipEnable       = false,
            MultisampleEnable     = false,
            AntialiasedLineEnable = false,
            ForcedSampleCount     = 0,
            ConservativeRaster    = ConservativeRasterizationMode.Off
        };

        desc.PrimitiveTopologyType = PrimitiveTopologyType.Triangle;

        desc.BlendState = new BlendDesc {
            AlphaToCoverageEnable  = false,
            IndependentBlendEnable = false,
        };
        for (int i = 0; i < D3D12.SimultaneousRenderTargetCount; i++) {
            desc.BlendState.RenderTarget[i] = new RenderTargetBlendDesc {
                BlendEnable           = true,
                SrcBlend              = Blend.SrcAlpha,
                DestBlend             = Blend.InvSrcAlpha,
                BlendOp               = BlendOp.Add,
                SrcBlendAlpha         = Blend.One,
                DestBlendAlpha        = Blend.InvSrcAlpha,
                BlendOpAlpha          = BlendOp.Add,
                RenderTargetWriteMask = (byte)ColorWriteEnable.All,
            };
        }

        desc.DepthStencilState.DepthEnable   = false;
        desc.DepthStencilState.StencilEnable = false;

        desc.SampleMask = uint.MaxValue;

        desc.NumRenderTargets = 1;
        desc.RTVFormats[0]    = Format.FormatR8G8B8A8Unorm;
        desc.SampleDesc.Count = 1;

        //Create the pipeline state
        this.PipelineState = this.Device.CreateGraphicsPipelineState<ID3D12PipelineState>(in desc);

        //Free the strings in the semantic names
        for (int i = 0; i < inputElementDescCount; i++) {
            SilkMarshal.FreeString((nint)inputElementDescriptors[i].SemanticName);
        }
    }

    private void CreateSwapchain() {
        this._surfaceSize = new Box2D<long>(0, 0, this._view.FramebufferSize.X, this._view.FramebufferSize.Y);

        this._viewport.TopLeftX = 0;
        this._viewport.TopLeftY = 0;
        this._viewport.Width    = this._view.FramebufferSize.X;
        this._viewport.Height   = this._view.FramebufferSize.Y;
        this._viewport.MinDepth = 0;
        this._viewport.MaxDepth = 1;

        if (this._swapchain.Handle != null) {
            this._swapchain.ResizeBuffers(
                BackbufferCount,
                (uint)this._view.FramebufferSize.X,
                (uint)this._view.FramebufferSize.Y,
                Format.FormatR8G8B8A8Unorm,
                0
            );
        }
        else {
            SwapChainDesc1 desc = new SwapChainDesc1 {
                BufferCount = BackbufferCount,
                Width       = (uint)this._view.FramebufferSize.X,
                Height      = (uint)this._view.FramebufferSize.Y,
                Format      = Format.FormatR8G8B8A8Unorm,
                BufferUsage = DXGI.UsageRenderTargetOutput,
                SwapEffect  = SwapEffect.FlipDiscard,
                SampleDesc = new SampleDesc {
                    Count = 1
                }
            };

            ComPtr<IDXGISwapChain1> newSwapchain = null;
            this._view.CreateDxgiSwapchain(
                (IDXGIFactory2*)this.DXGIFactory.Handle,
                (IUnknown*)this.CommandQueue.Handle,
                &desc,
                pFullscreenDesc: null,
                pRestrictToOutput: null,
                newSwapchain.GetAddressOf()
            );

            this._swapchain = newSwapchain.QueryInterface<IDXGISwapChain3>();
        }

        this.FrameIndex = this._swapchain.GetCurrentBackBufferIndex();

        //Describe and create a render target view (RTV) descriptor heap.
        DescriptorHeapDesc rtvHeapDesc = new DescriptorHeapDesc {
            NumDescriptors = BackbufferCount,
            Type           = DescriptorHeapType.Rtv,
            Flags          = DescriptorHeapFlags.None
        };
        this._renderTargetViewHeap = this.Device.CreateDescriptorHeap<ID3D12DescriptorHeap>(rtvHeapDesc);

        this._rtvDescriptorSize = this.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);

        //Create frame resources

        //BROKEN
        // CpuDescriptorHandle rtvHandle = this._renderTargetViewHeap.GetCPUDescriptorHandleForHeapStart();

        //WORKING
        ID3D12DescriptorHeap* rtvHeap   = this._renderTargetViewHeap;
        CpuDescriptorHandle   rtvHandle = rtvHeap->GetCPUDescriptorHandleForHeapStart();

        // Create a RTV for each frame.
        for (uint i = 0; i < BackbufferCount; i++) {
            this._renderTargets[i] = this._swapchain.GetBuffer<ID3D12Resource>(i);
            this.Device.CreateRenderTargetView(this._renderTargets[i], null, rtvHandle);
            rtvHandle.Ptr += 1 * this._rtvDescriptorSize;
        }
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

        SilkMarshal.ThrowHResult(this.Device.CreateCommandList(
                                     0,
                                     CommandListType.Direct,
                                     this.CommandAllocator,
                                     this.PipelineState,
                                     out this.CommandList
                                 ));
    }

    public override void Cleanup() {
        //Release our device and DXGI factory
        this.Device.Release();
        this.Debug.Release();
        this.DebugInfoQueue.Release();
        this.DXGIFactory.Release();
    }

    public override void HandleFramebufferResize(int width, int height) {
        this.CreateSwapchain();
        this.SetFullScissorRect();
    }

    public override VixieRenderer CreateRenderer() {
        throw new NotImplementedException();
    }

    public override Vector2D<int> MaxTextureSize { get; }

    public override void BeginScene() {
        base.BeginScene();

        //Reset the command allocator and command list
        this.CommandAllocator.Reset();
        this.CommandList.Reset(this.CommandAllocator, this.PipelineState);

        this.CommandList.SetGraphicsRootSignature(this.RootSignature);
        // this.CommandList.SetDescriptorHeaps();
        // D3D12_GPU_DESCRIPTOR_HANDLE cbvHandle(constantBufferHeap->GetGPUDescriptorHandleForHeapStart());
        // this.CommandList.SetGraphicsRootDescriptorTable(0, cbvHandle);

        //Indicate that the back buffer will be used as a render target
        ResourceBarrier renderTargetBarrier = new ResourceBarrier {
            Type  = ResourceBarrierType.Transition,
            Flags = ResourceBarrierFlags.None
        };
        renderTargetBarrier.Anonymous.Transition.PResource   = this._renderTargets[this.FrameIndex];
        renderTargetBarrier.Anonymous.Transition.StateBefore = ResourceStates.Present;
        renderTargetBarrier.Anonymous.Transition.StateAfter  = ResourceStates.RenderTarget;
        renderTargetBarrier.Anonymous.Transition.Subresource = D3D12.ResourceBarrierAllSubresources;

        this.CommandList.ResourceBarrier(1, &renderTargetBarrier);

        ID3D12DescriptorHeap* rtvHeap = this._renderTargetViewHeap;
        this._currentRtvHandle     =  rtvHeap->GetCPUDescriptorHandleForHeapStart();
        this._currentRtvHandle.Ptr += this.FrameIndex * this._rtvDescriptorSize;
        this.CommandList.OMSetRenderTargets(
            1,
            in this._currentRtvHandle,
            false,
            null
        );

        this.CommandList.RSSetViewports(1, in this._viewport);
        this.CommandList.RSSetScissorRects(1, in this.CurrentScissorRect);
        this.CommandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
    }

    public override void Present() {
        base.Present();

        //Indicate that the back buffer will be used as a render target
        ResourceBarrier presentBarrier = new ResourceBarrier {
            Type  = ResourceBarrierType.Transition,
            Flags = ResourceBarrierFlags.None
        };
        presentBarrier.Anonymous.Transition.PResource   = this._renderTargets[this.FrameIndex];
        presentBarrier.Anonymous.Transition.StateBefore = ResourceStates.RenderTarget;
        presentBarrier.Anonymous.Transition.StateAfter  = ResourceStates.Present;
        presentBarrier.Anonymous.Transition.Subresource = D3D12.ResourceBarrierAllSubresources;

        this.CommandList.ResourceBarrier(1, &presentBarrier);

        SilkMarshal.ThrowHResult(this.CommandList.Close());

        this.CommandQueue.ExecuteCommandLists(1, ref this.CommandList);

        //TODO: check if vsync or not, if not vsync, then swap interval should be 0
        //else: it should be 1, but lets assume no Vsync for now.
        SilkMarshal.ThrowHResult(this._swapchain.Present(0, 0));

        ulong fence = this.FenceValue;
        SilkMarshal.ThrowHResult(this.CommandQueue.Signal(this.Fence, fence));
        this.FenceValue++;

        if (this.Fence.GetCompletedValue() < fence) {
            this.Fence.SetEventOnCompletion(fence, this.FenceEvent);
            SilkMarshal.WaitWindowsObjects((nint)this.FenceEvent);
        }

        this.FrameIndex = this._swapchain.GetCurrentBackBufferIndex();
    }

    public override void Clear() {
        D3Dcolorvalue clear = new D3Dcolorvalue(0, 0, 0, 0);

        this.CommandList.ClearRenderTargetView(this._currentRtvHandle, (float*)&clear, 1u, in this.CurrentScissorRect);
    }

    public override void TakeScreenshot() {
        throw new NotImplementedException();
    }

    public override Rectangle ScissorRect {
        get => new Rectangle(this.CurrentScissorRect.Min.X, this.CurrentScissorRect.Min.Y, this.CurrentScissorRect.Size.X, this.CurrentScissorRect.Size.Y);
        set => this.CurrentScissorRect = new Box2D<int>(value.X, value.Y, value.Right, value.Bottom);
    }

    public override void SetFullScissorRect() {
        this.CurrentScissorRect = new Box2D<int>(0, 0, this._view.FramebufferSize.X, this._view.FramebufferSize.Y);
    }

    public override ulong GetVramUsage() {
        return 0;
    }

    public override ulong GetTotalVram() {
        return 0;
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

#if USE_IMGUI
    public override void ImGuiUpdate(double deltaTime) {
        throw new NotImplementedException();
    }

    public override void ImGuiDraw(double deltaTime) {
        throw new NotImplementedException();
    }
#endif
}