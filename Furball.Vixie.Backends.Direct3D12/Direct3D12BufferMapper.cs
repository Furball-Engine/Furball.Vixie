using Furball.Vixie.Backends.Direct3D12.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Furball.Vixie.Backends.Direct3D12; 

public unsafe class Direct3D12BufferMapper : BufferMapper {
    private readonly Direct3D12Backend _backend;
    private readonly ResourceStates    _resourceState;
    private readonly void*             Ptr;
    public Direct3D12BufferMapper(Direct3D12Backend backend, uint byteSize, ResourceStates resourceState) {
        this._backend       = backend;
        this._resourceState = resourceState;

        // Guard.Assert((usage & ~(BufferUsage.Index | BufferUsage.Vertex)) == 0,
                     // "(usage & ~(BufferUsage.Index | BufferUsage.Vertex)) == 0");

        this.SizeInBytes = byteSize;

        this.Ptr = (void*)SilkMarshal.Allocate((int)byteSize);
    }
    
    public void CopyMappedDataToExistingBufferAndReset(Direct3D12Buffer buffer) {
        //Transition the buffer into a generic read state if it isnt already 
        if(buffer.CurrentResourceState != ResourceStates.GenericRead)
            buffer.BarrierTransition(ResourceStates.GenericRead);

        if (buffer.OffsetInBytes + this.ReservedBytes > this.SizeInBytes)
            throw new NotImplementedException("We dont cleanly handle this...... we need to create a *new* buffer instead here");
        
        //Map the buffer
        void* map = buffer.Map();
        //Copy our data in, starting at the buffers offset and ending at the end of the buffer
        Buffer.MemoryCopy(
            this.Ptr,
            (void*)((ulong)map + buffer.OffsetInBytes),
            this.ReservedBytes,
            this.ReservedBytes
        );
        //Unmap the buffer
        buffer.Unmap();
        
        //Transition said buffer into the resource state (generally will be a vertex or index buffer)
        buffer.BarrierTransition(this._resourceState);

        this.ReservedBytes = 0;
    }

    public Direct3D12Buffer CopyMappedDataToNewBuffer() {
        Direct3D12Buffer buffer = new Direct3D12Buffer(this._backend, this.SizeInBytes, HeapType.Upload);

        if (this._resourceState == ResourceStates.VertexAndConstantBuffer) {
            buffer.VertexBufferView = new VertexBufferView {
                BufferLocation = buffer.Resource.GetGPUVirtualAddress(), 
                StrideInBytes = (uint)sizeof(Vertex),
                SizeInBytes = (uint)this.SizeInBytes
            };
        }
        else if(this._resourceState == ResourceStates.IndexBuffer) {
            buffer.IndexBufferView = new IndexBufferView {
                BufferLocation = buffer.Resource.GetGPUVirtualAddress(), 
                SizeInBytes    = (uint)this.SizeInBytes, 
                Format         = Format.FormatR16Uint
            }; 
        }
        
        this.CopyMappedDataToExistingBufferAndReset(buffer);

        return buffer;
    }

    public override void Map() {
        
    }
    
    public override void Unmap() {
        
    }
    
    public override void* Reserve(nuint byteCount) {
        nuint ptr = (nuint)this.Ptr + this.ReservedBytes;

        //If this reserve will push us over the limit, return nullptr
        if (this.ReservedBytes + byteCount > this.SizeInBytes)
            return null;

        this.ReservedBytes += byteCount;

        return (void*)ptr;
    }
    
    protected override void DisposeInternal() {
        SilkMarshal.Free((nint)this.Ptr);
    }
}