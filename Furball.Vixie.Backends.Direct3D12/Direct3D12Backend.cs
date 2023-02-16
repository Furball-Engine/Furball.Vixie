using System.Numerics;
using System.Runtime.CompilerServices;
using Furball.Vixie.Backends.Direct3D12.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Backends.Shared.TextureEffects.Blur;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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

    public ComPtr<ID3D12CommandQueue>     CommandQueue;
    public ComPtr<ID3D12CommandAllocator> CommandAllocator;

    public ComPtr<ID3D12GraphicsCommandList> CommandList;

    public uint                FrameIndex;
    public void*               FenceEvent;
    public ulong               FenceValue;
    public ComPtr<ID3D12Fence> Fence;

    private const uint BackbufferCount = 2;

    private ComPtr<ID3D12DescriptorHeap> _renderTargetViewHeap;
    private uint                         _rtvDescriptorSize;
    private Direct3D12BackBuffer[]       _renderTargets = new Direct3D12BackBuffer[BackbufferCount];

    private ComPtr<IDXGISwapChain3> _swapchain;
    private Viewport                _viewport;

    private Box2D<int>          CurrentScissorRect;
    private CpuDescriptorHandle _currentRtvHandle;

    public Direct3D12DescriptorHeap SamplerHeap;
    public Direct3D12DescriptorHeap CbvSrvUavHeap;
    public Direct3D12DescriptorHeap RtvHeap;

    public Stack<IDisposable> GraphicsItemsToGo = new Stack<IDisposable>();

    private Matrix4x4 _projectionMatrixValue;

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
            this.CreateSwapchain((uint)view.FramebufferSize.X, (uint)view.FramebufferSize.Y);
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

        //Find the largest heap we can create, going from 2^20, all the way down until we can create one
        Direct3D12DescriptorHeap? heap = null;
        int                       pow  = 20;
        while (heap == null && pow > 0) {
            try {
                uint sizeToTry = (uint)Math.Pow(2, pow);

                heap = new Direct3D12DescriptorHeap(this, DescriptorHeapType.Sampler, sizeToTry);

                heap.Dispose();

                break;
            }
            catch {
                heap = null;

                pow--;
            }
        }
        Direct3D12DescriptorHeap.DefaultSamplerSlotAmount = (uint)Math.Pow(2, pow);

        if (pow == 0) {
            throw new Exception("Unable to create *any* size of heap! Try updating your graphics drivers!");
        }

        pow = 20;
        while (heap == null && pow > 0) {
            try {
                uint sizeToTry = (uint)Math.Pow(2, pow);

                heap = new Direct3D12DescriptorHeap(this, DescriptorHeapType.CbvSrvUav, sizeToTry);

                heap.Dispose();

                break;
            }
            catch {
                heap = null;

                pow--;
            }
        }
        Direct3D12DescriptorHeap.DefaultCbvSrvUavSlotAmount = (uint)Math.Pow(2, pow);

        if (pow == 0) {
            throw new Exception("Unable to create *any* size of heap! Try updating your graphics drivers!");
        }

        pow = 20;
        while (heap == null && pow > 0) {
            try {
                uint sizeToTry = (uint)Math.Pow(2, pow);

                heap = new Direct3D12DescriptorHeap(this, DescriptorHeapType.Rtv, sizeToTry);

                heap.Dispose();

                break;
            }
            catch {
                heap = null;

                pow--;
            }
        }
        Direct3D12DescriptorHeap.DefaultRtvAmount = (uint)Math.Pow(2, pow);

        if (pow == 0) {
            throw new Exception("Unable to create *any* size of heap! Try updating your graphics drivers!");
        }

        this.SamplerHeap = new Direct3D12DescriptorHeap(
        this,
        DescriptorHeapType.Sampler,
        Direct3D12DescriptorHeap.DefaultSamplerSlotAmount
        );
        this.CbvSrvUavHeap = new Direct3D12DescriptorHeap(
        this,
        DescriptorHeapType.CbvSrvUav,
        Direct3D12DescriptorHeap.DefaultCbvSrvUavSlotAmount
        );
        this.RtvHeap = new Direct3D12DescriptorHeap(
        this,
        DescriptorHeapType.Rtv,
        Direct3D12DescriptorHeap.DefaultRtvAmount
        );

        this.SamplerHeap.Heap.SetName("SamplerHeap");
        this.CbvSrvUavHeap.Heap.SetName("CbvSrvUavHeap");

        this.CreateProjectionMatrixBuffers();

#if USE_IMGUI
        throw new NotImplementedException("ImGui is not implemented on Direct3D12!");
#endif

        this.PrintInfoQueue();
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
        byte[] vertexShaderDxil = ResourceHelpers.GetByteResource(
        @"Shaders/VertexShader.dxil",
        typeof(Direct3D12Backend)
        );
        byte[] pixelShaderDxil = ResourceHelpers.GetByteResource(
        @"Shaders/PixelShader.dxil",
        typeof(Direct3D12Backend)
        );

        ComPtr<ID3D10Blob> vertexShaderBlob = null;
        ComPtr<ID3D10Blob> pixelShaderBlob  = null;
        SilkMarshal.ThrowHResult(this.D3DCompiler.CreateBlob((nuint)vertexShaderDxil.Length, ref vertexShaderBlob));
        SilkMarshal.ThrowHResult(this.D3DCompiler.CreateBlob((nuint)pixelShaderDxil.Length,  ref pixelShaderBlob));

        //Copy the shader to the blob
        fixed (void* ptr = vertexShaderDxil)
            Buffer.MemoryCopy(
            ptr,
            vertexShaderBlob.GetBufferPointer(),
            vertexShaderDxil.Length,
            vertexShaderDxil.Length
            );
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

        desc.PRootSignature = this.RootSignature = this.Device.CreateRootSignature<ID3D12RootSignature>(
                              0,
                              vertexShaderBlob.GetBufferPointer(),
                              vertexShaderBlob.GetBufferSize()
                              );

        desc.RasterizerState = new RasterizerDesc {
            FillMode              = FillMode.Solid,
            CullMode              = CullMode.None,//TODO: this should not be None
            FrontCounterClockwise = false,
            DepthBias             = D3D12.DefaultDepthBias,
            DepthBiasClamp        = 0,//D3D12_DEFAULT_DEPTH_BIAS_CLAMP
            SlopeScaledDepthBias  = 0,//D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS
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
        desc.RTVFormats[0]    = Format.FormatB8G8R8A8Unorm;
        desc.SampleDesc.Count = 1;

        //Create the pipeline state
        this.PipelineState = this.Device.CreateGraphicsPipelineState<ID3D12PipelineState>(in desc);

        //Free the strings in the semantic names
        for (int i = 0; i < inputElementDescCount; i++) {
            SilkMarshal.FreeString((nint)inputElementDescriptors[i].SemanticName);
        }
    }

    private void CreateSwapchain(uint width, uint height) {
        this._viewport.TopLeftX = 0;
        this._viewport.TopLeftY = 0;
        this._viewport.Width    = width;
        this._viewport.Height   = height;
        this._viewport.MinDepth = 0;
        this._viewport.MaxDepth = 1;

        const uint swapchainFlags = (uint)SwapChainFlag.AllowTearing;
        if (this._swapchain.Handle != null) {
            this.EndAndExecuteCommandList();
            this.FenceCommandList();
            
            foreach (Direct3D12BackBuffer renderTarget in this._renderTargets) {
                renderTarget.Dispose();
            }

            this._renderTargetViewHeap.Dispose();

            SilkMarshal.ThrowHResult(
            this._swapchain.ResizeBuffers(BackbufferCount, width, height, Format.FormatB8G8R8A8Unorm, swapchainFlags)
            );
        } else {
            SwapChainDesc1 desc = new SwapChainDesc1 {
                BufferCount = BackbufferCount,
                Width       = width,
                Height      = height,
                Format      = Format.FormatB8G8R8A8Unorm,
                BufferUsage = DXGI.UsageRenderTargetOutput,
                SwapEffect  = SwapEffect.FlipDiscard,
                Flags       = swapchainFlags,
                SampleDesc = new SampleDesc {
                    Count = 1
                },
                Scaling = Scaling.None
            };

            ComPtr<IDXGISwapChain1> newSwapchain = null;
            SilkMarshal.ThrowHResult(
            this._view.CreateDxgiSwapchain(
            (IDXGIFactory2*)this.DXGIFactory.Handle,
            (IUnknown*)this.CommandQueue.Handle,
            &desc,
            pFullscreenDesc: null,
            pRestrictToOutput: null,
            newSwapchain.GetAddressOf()
            )
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
        CpuDescriptorHandle rtvHandle = this._renderTargetViewHeap.GetCPUDescriptorHandleForHeapStart();

        // Create a RTV for each frame.
        for (uint i = 0; i < BackbufferCount; i++) {
            this._renderTargets[i] = new Direct3D12BackBuffer(this, this._swapchain.GetBuffer<ID3D12Resource>(i));
            this.Device.CreateRenderTargetView(this._renderTargets[i].Resource, null, rtvHandle);
            rtvHandle.Ptr += 1 * this._rtvDescriptorSize;
        }

        if (this._swapchain.Handle != null)
            this.ResetCommandListAndAllocator();
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

        SilkMarshal.ThrowHResult(
        this.Device.CreateCommandList(
        0,
        CommandListType.Direct,
        this.CommandAllocator,
        this.PipelineState,
        out this.CommandList
        )
        );

        this.CommandList.SetName("cmdlist");

        // SilkMarshal.ThrowHResult(this.CommandList.Close());
    }

    public override void Cleanup() {
        //Release our device and DXGI factory
        this.DebugInfoQueue.Release();
        this.Debug.Release();
        this.Device.Release();
        this.DXGIFactory.Release();
    }

    public override void HandleFramebufferResize(int width, int height) {
        this.UpdateProjectionMatrix(width, height, false);

        this.CreateSwapchain((uint)width, (uint)height);
        this.SetFullScissorRect();
    }

    private void CreateProjectionMatrixBuffers() {
        uint size = Align((uint)sizeof(Matrix4x4), D3D12.ConstantBufferDataPlacementAlignment);

    }

    public static uint Align(uint uValue, uint uAlign) {
        // Assert power of 2 alignment
        Guard.Assert(0 == (uAlign & (uAlign - 1)));
        uint uMask   = uAlign - 1;
        uint uResult = (uValue + uMask) & ~uMask;
        Guard.Assert(uResult >= uValue);
        Guard.Assert(0 == (uResult % uAlign));
        return uResult;
    }

    public void UpdateProjectionMatrix(int width, int height, bool fbProjMatrix) {
        float right  = fbProjMatrix ? width : width / (float)height * 720f;
        float bottom = fbProjMatrix ? height : 720f;

        this._projectionMatrixValue = Matrix4x4.CreateOrthographicOffCenter(0, right, bottom, 0, 1f, 0f);
    }

    public override VixieRenderer CreateRenderer() => new Direct3D12Renderer(this);

    public override BoxBlurTextureEffect CreateBoxBlurTextureEffect(VixieTexture source) {
        try {
            return new OpenCLBoxBlurTextureEffect(this, source);
        }
        catch {
            return new CpuBoxBlurTextureEffect(this, source);
        }
    }

    /// <summary>
    /// This is a constant for feature level 11_0 hardware and above
    /// </summary>
    public override Vector2D<int> MaxTextureSize => new Vector2D<int>(16384);

    private bool _firstFrame = true;
    public override void BeginScene() {
        base.BeginScene();

        //Indicate that the back buffer will be used as a render target
        this._renderTargets[this.FrameIndex].BarrierTransition(
        ResourceStates.RenderTarget,
        D3D12.ResourceBarrierAllSubresources
        );

        //Reset the current render target view, at the beginning of the scene, we should always render to the swapchain
        this._currentRenderTargetView = default;
        this.SetCommandListProps();
    }

    /// <summary>
    /// The current render target view we are rendering to
    /// <remarks>
    /// If this == default, than it means render to the swapchain
    /// </remarks>
    /// </summary>
    private CpuDescriptorHandle _currentRenderTargetView;
    private bool _screenshotQueued;

    private void SetRenderTargetInternal(CpuDescriptorHandle handle) {
        this.CommandList.OMSetRenderTargets(1, in handle, false, null);
    }

    /// <summary>
    /// Sets the current render target we are rendering to
    /// </summary>
    /// <param name="handle">The CPU handle of the render target, `default` if we should to back to the swapchain</param>
    public void SetRenderTarget(CpuDescriptorHandle handle) {
        if (handle.Ptr == default) {
            this._currentRenderTargetView = default;
            this.SetRenderTargetInternal(this._currentRtvHandle);
            return;
        }

        //Set the current render target view
        this._currentRenderTargetView = handle;
        //Update the command list about the change
        this.SetRenderTargetInternal(handle);
    }

    public void SetCommandListProps() {
        this.CommandList.SetGraphicsRootSignature(this.RootSignature);

        //If no special render target view is bound, just go to the one for the current index of the swapchain
        //This should always be the case at the start of the frame
        if (this._currentRenderTargetView.Ptr == default) {
            this._currentRtvHandle     =  this._renderTargetViewHeap.GetCPUDescriptorHandleForHeapStart();
            this._currentRtvHandle.Ptr += this.FrameIndex * this._rtvDescriptorSize;
            this.SetRenderTargetInternal(this._currentRtvHandle);
        } else {
            //If we have a special render target bound, use it here
            this.SetRenderTargetInternal(this._currentRenderTargetView);
        }

        this.CommandList.RSSetViewports(1, in this._viewport);
        this.CommandList.RSSetScissorRects(1, in this.CurrentScissorRect);
        this.CommandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);

        Matrix4x4 projMatrix = this._projectionMatrixValue;
        this.CommandList.SetGraphicsRoot32BitConstants(0, 16, &projMatrix, 0);

        ID3D12DescriptorHeap** heaps = stackalloc ID3D12DescriptorHeap*[2];

        heaps[0] = this.CbvSrvUavHeap.Heap;
        heaps[1] = this.SamplerHeap.Heap;

        //Bind the 2 descriptor heaps
        this.CommandList.SetDescriptorHeaps(2, heaps);

        this.CommandList.SetGraphicsRootDescriptorTable(
        1,
        this.CbvSrvUavHeap.Heap.GetGPUDescriptorHandleForHeapStart()
        );
        this.CommandList.SetGraphicsRootDescriptorTable(2, this.SamplerHeap.Heap.GetGPUDescriptorHandleForHeapStart());
    }

    public override void Present() {
        base.Present();

        //Indicate that the back buffer will be used as a render target
        this._renderTargets[this.FrameIndex].BarrierTransition(
        ResourceStates.Present,
        D3D12.ResourceBarrierAllSubresources
        );

        //If there is a screenshot queued
        if (this._screenshotQueued) {
            Direct3D12BackBuffer target = this._renderTargets[this.FrameIndex];

            //Transition the backbuffer into a copy source
            target.BarrierTransition(ResourceStates.CopySource, D3D12.ResourceBarrierAllSubresources);

            //Create the subresource footprint of the backbuffer
            SubresourceFootprint footprint = new SubresourceFootprint {
                Format   = Format.FormatB8G8R8A8Unorm,
                Width    = (uint)this._viewport.Width,
                Height   = (uint)this._viewport.Height,
                Depth    = 1,
                RowPitch = Align((uint)(this._viewport.Width * sizeof(Rgba32)), D3D12.TextureDataPitchAlignment)
            };

            //The size of our download buffer
            ulong readbackBufferSize = footprint.RowPitch * footprint.Height;

            //The description of the upload buffer
            ResourceDesc readbackBufferDesc = new ResourceDesc {
                Dimension        = ResourceDimension.Buffer,
                Width            = readbackBufferSize,
                Height           = 1,
                Format           = Format.FormatUnknown,
                DepthOrArraySize = 1,
                Flags            = ResourceFlags.None,
                MipLevels        = 1,
                SampleDesc       = new SampleDesc(1, 0),
                Layout           = TextureLayout.LayoutRowMajor
            };

            //The heap properties of the upload buffer, being of type `Upload`
            HeapProperties readbackBufferHeapProperties = new HeapProperties {
                Type                 = HeapType.Readback,
                CPUPageProperty      = CpuPageProperty.None,
                CreationNodeMask     = 0,
                VisibleNodeMask      = 0,
                MemoryPoolPreference = MemoryPool.None
            };
            //Create the upload buffer
            ComPtr<ID3D12Resource> readbackBuffer = this.Device.CreateCommittedResource<ID3D12Resource>(
            &readbackBufferHeapProperties,
            HeapFlags.None,
            &readbackBufferDesc,
            ResourceStates.CopyDest,
            null
            );
            readbackBuffer.SetName("texture readback buffer");

            //Create the placed subresource footprint of the texture
            PlacedSubresourceFootprint placedTexture2D = new PlacedSubresourceFootprint {
                Offset    = 0,
                Footprint = footprint
            };

            //Copy the texture to the readback buffer
            this.CommandList.CopyTextureRegion(
            new TextureCopyLocation(
            readbackBuffer,
            TextureCopyType.PlacedFootprint,
            new TextureCopyLocationUnion(placedTexture2D),
            placedTexture2D
            ),
            0,
            0,
            0,
            new TextureCopyLocation(
            target.Resource,
            TextureCopyType.SubresourceIndex,
            new TextureCopyLocationUnion(null, 0),
            null,
            0
            ),
            null
            );
            
            //Transition the backbuffer back to `Present`
            target.BarrierTransition(ResourceStates.Present, D3D12.ResourceBarrierAllSubresources);

            //End and execute the command list
            this.EndAndExecuteCommandList();
            //Fence the command list to wait for its completion
            this.FenceCommandList();

            //Declare the pointer which will point to our mapped data
            void* mapBegin = null;

            //Map the resource
            SilkMarshal.ThrowHResult(readbackBuffer.Map(0, new Range(0, (nuint?)readbackBufferSize), &mapBegin));

            Bgra32[]     pixData     = new Bgra32[(int)(this._viewport.Width * this._viewport.Height)];
            Span<Bgra32> pixDataSpan = pixData;

            //Copy the data from the map into the buffer
            for (int y = 0; y < this._viewport.Height; y++) {
                new Span<Bgra32>(
                (void*)((nint)mapBegin + (nint)placedTexture2D.Offset + y * footprint.RowPitch),
                (int)this._viewport.Width
                ).CopyTo(pixDataSpan.Slice((int)(y * this._viewport.Width)));
            }

            //Unmap the readback buffer
            readbackBuffer.Unmap(0, new Range(0, 0));

            //Send the readback buffer to be disposed
            this.GraphicsItemsToGo.Push(readbackBuffer);

            //Tell all listeners that a screenshot has been taken
            this.InvokeScreenshotTaken(
            Image.LoadPixelData(pixData, (int)this._viewport.Width, (int)this._viewport.Height)
            );
        } 
        //If there is no screenshot queued,
        else {
            //End and execute the command list like normal
            this.EndAndExecuteCommandList();
            
            //Fence the command list to wait for its completion
            this.FenceCommandList();
        }

        SilkMarshal.ThrowHResult(this._swapchain.Present(0, this._view.VSync ? 0 : DXGI.PresentAllowTearing));

        this._screenshotQueued = false;

        //The new frame index after present
        this.FrameIndex = this._swapchain.GetCurrentBackBufferIndex();

        this.FrameReset?.Invoke(this, EventArgs.Empty);

        this.ResetCommandListAndAllocator();

#if DEBUG
        this.PrintInfoQueue();
#endif
    }

    internal event EventHandler? FrameReset;

    public void ResetCommandListAndAllocator() {
        this.CommandAllocator.Reset();
        this.CommandList.Reset(this.CommandAllocator, this.PipelineState);
    }

    public void FenceCommandList() {
        ulong fence = this.FenceValue;
        SilkMarshal.ThrowHResult(this.CommandQueue.Signal(this.Fence, fence));
        this.FenceValue++;

        if (this.Fence.GetCompletedValue() < fence) {
            this.Fence.SetEventOnCompletion(fence, this.FenceEvent);
            SilkMarshal.WaitWindowsObjects((nint)this.FenceEvent);
        }
    }

    public void EndAndExecuteCommandList() {
        SilkMarshal.ThrowHResult(this.CommandList.Close());

        this.CommandQueue.ExecuteCommandLists(1, ref this.CommandList);
    }

    public override void Clear() {
        D3Dcolorvalue clear = new D3Dcolorvalue(0, 0, 0, 0);

        this.CommandList.ClearRenderTargetView(this._currentRtvHandle, (float*)&clear, 1u, in this.CurrentScissorRect);
    }

    public override void TakeScreenshot() {
        this._screenshotQueued = true;
    }

    public override Rectangle ScissorRect {
        get => new Rectangle(
        this.CurrentScissorRect.Min.X,
        this.CurrentScissorRect.Min.Y,
        this.CurrentScissorRect.Size.X,
        this.CurrentScissorRect.Size.Y
        );
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

    public override VixieTextureRenderTarget CreateRenderTarget(uint width, uint height) =>
        new Direct3D12RenderTarget(this, (int)width, (int)height);

    public override VixieTexture CreateTextureFromByteArray(byte[] imageData, TextureParameters parameters = default) {
        Image<Rgba32> image;

        bool qoi = imageData.Length > 3 && imageData[0] == 'q' && imageData[1] == 'o' && imageData[2] == 'i' &&
                   imageData[3] == 'f';

        if (qoi) {
            (Rgba32[] pixels, QoiLoader.QoiHeader header) data = QoiLoader.Load(imageData);

            image = Image.LoadPixelData(data.pixels, (int)data.header.Width, (int)data.header.Height);
        } else {
            image = Image.Load<Rgba32>(imageData);
        }

        Direct3D12Texture texture = new Direct3D12Texture(this, image.Width, image.Height, parameters);

        Rgba32[] arr = new Rgba32[texture.Width * texture.Height];
        image.CopyPixelDataTo(arr);

        texture.SetData<Rgba32>(arr);

        image.Dispose();

        return texture;
    }

    public override VixieTexture CreateTextureFromStream(Stream stream, TextureParameters parameters = default) {
        using Image<Rgba32> image = Image.Load<Rgba32>(stream);

        Direct3D12Texture texture = new Direct3D12Texture(this, image.Width, image.Height, parameters);

        Rgba32[] arr = new Rgba32[texture.Width * texture.Height];
        image.CopyPixelDataTo(arr);

        texture.SetData<Rgba32>(arr);

        return texture;
    }

    public override VixieTexture CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default) =>
        new Direct3D12Texture(this, (int)width, (int)height, parameters);

    public override VixieTexture CreateWhitePixelTexture() {
        Direct3D12Texture tex = new Direct3D12Texture(this, 1, 1, default);
        tex.SetData<Rgba32>(
        new[] {
            new Rgba32(255, 255, 255, 255)
        }
        );
        return tex;
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