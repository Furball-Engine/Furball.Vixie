using System.Diagnostics;
using Furball.Vixie.Backends.Shared.Renderers;
using Veldrid;

namespace Furball.Vixie.Backends.Veldrid; 

public class VeldridBufferMapper : BufferMapper {
    private readonly VeldridBackend _backend;
    private readonly BufferUsage    Usage;
    private          DeviceBuffer?  _buffer;

    public unsafe void* Ptr;

    public VeldridBufferMapper(VeldridBackend backend, uint byteSize, BufferUsage usage) {
        this._backend = backend;

        Debug.Assert((usage & ~(BufferUsage.IndexBuffer | BufferUsage.VertexBuffer)) == 0, "(usage & ~(BufferUsage.IndexBuffer | BufferUsage.VertexBuffer)) == 0");

        this.Usage = usage;
        
        this.SizeInBytes = byteSize;
    }

    public unsafe DeviceBuffer? Reset() {
        this.ReservedBytes = 0;
        
        DeviceBuffer? old = this._buffer;
        
        BufferDescription desc = new((uint)this.SizeInBytes, BufferUsage.Dynamic | this.Usage);
        this._buffer = this._backend.ResourceFactory.CreateBuffer(ref desc);
        MappedResource map = this._backend.GraphicsDevice.Map(this._buffer, MapMode.Write);
        
        this.Ptr = (void*)map.Data;
        
        if(old != null)
            this._backend.GraphicsDevice.Unmap(old);
        return old;
    }
    
    public override unsafe void Map() {
        this.Reset()?.Dispose();

        MappedResource map = this._backend.GraphicsDevice.Map(this._buffer, MapMode.Write);

        this.Ptr = (void*)map.Data;
    }
    
    public override void Unmap() {
        this._backend.GraphicsDevice.Unmap(this._buffer);
    }
    
    public override unsafe void* Reserve(nuint byteCount) {
        nuint ptr = (nuint)this.Ptr + this.ReservedBytes;

        //If this reserve will push us over the limit, return nullptr
        if (this.ReservedBytes + byteCount > this.SizeInBytes)
            return null;
        
        this.ReservedBytes += byteCount;

        return (void*)ptr;
    }
    
    protected override void DisposeInternal() {
        this._buffer?.Dispose();
    }
}