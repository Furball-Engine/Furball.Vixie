using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Furball.Vixie.Backends.Direct3D12.Abstractions;

public unsafe class Direct3D12Buffer {
    private readonly Direct3D12Backend _backend;

    public ResourceStates CurrentResourceState { get; private set; }

    public readonly ComPtr<ID3D12Resource> Buffer;

    public VertexBufferView VertexBufferView;
    public IndexBufferView  IndexBufferView;
    
    public Direct3D12Buffer(Direct3D12Backend backend, ulong size, HeapType type) {
        this._backend = backend;
        //The description of the upload buffer
        ResourceDesc uploadBufferDesc = new ResourceDesc {
            Dimension        = ResourceDimension.Buffer,
            Width            = size,
            Height           = 1,
            Format           = Format.FormatUnknown,
            DepthOrArraySize = 1,
            Flags            = ResourceFlags.None,
            MipLevels        = 1,
            SampleDesc       = new SampleDesc(1, 0),
            Layout           = TextureLayout.LayoutRowMajor
        };

        //The heap properties of the upload buffer, being of type `Upload`
        HeapProperties uploadBufferHeapProperties = new HeapProperties {
            Type                 = type,
            CPUPageProperty      = CpuPageProperty.Unknown,
            CreationNodeMask     = 0,
            VisibleNodeMask      = 0,
            MemoryPoolPreference = MemoryPool.None
        };

        this.CurrentResourceState = ResourceStates.GenericRead;
        
        //Create the upload buffer
        this.Buffer = this._backend.Device.CreateCommittedResource<ID3D12Resource>(
            &uploadBufferHeapProperties,
            HeapFlags.None,
            &uploadBufferDesc,
            this.CurrentResourceState,
            null
        );

        this.Buffer.SetName("buffer waaaa");
    }

    /// <summary>
    /// Maps the buffer
    /// </summary>
    /// <param name="readRange">The range of data you plan to read from</param>
    /// <returns>A pointer to the mapped data</returns>
    public void* Map(Range readRange = default) {
        void* ptr = null;
        SilkMarshal.ThrowHResult(this.Buffer.Map(0, new Range(0, 0), &ptr));
        return ptr;
    }
    
    /// <summary>
    /// Unmaps the buffer
    /// </summary>
    /// <param name="writeRange">An optional range of how much data was actually written, only used for external tooling</param>
    public void Unmap(Range writeRange = default) {
        this.Buffer.Unmap(0, in writeRange);
    }

    public void BarrierTransition(ResourceStates stateTo) {
        //Dont barrier transition if we are *already* in said state
        if (this.CurrentResourceState == stateTo)
            return; //NOTE: should this be allowed? i dont see a reason but maybe there is
        
        //Tell the command list that this resource is now in use for `stateTo` purpose
        ResourceBarrier copyBarrier = new ResourceBarrier {
            Type = ResourceBarrierType.Transition
        };
        copyBarrier.Anonymous.Transition.PResource   = this.Buffer;
        copyBarrier.Anonymous.Transition.Subresource = 0;
        copyBarrier.Anonymous.Transition.StateAfter  = stateTo;
        copyBarrier.Anonymous.Transition.StateBefore = this.CurrentResourceState;
        this._backend.CommandList.ResourceBarrier(1, &copyBarrier);

        this.CurrentResourceState = stateTo;
    }
}