using System;
using System.Collections.Generic;
using System.Diagnostics;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Veldrid;

namespace Furball.Vixie.Backends.Veldrid; 

public class VeldridBufferMapper : BufferMapper {
    private readonly VeldridBackend _backend;
    private readonly BufferUsage    Usage;
    private          DeviceBuffer?  _buffer;

    public unsafe void* Ptr;

    public VeldridBufferMapper(VeldridBackend backend, uint byteSize, BufferUsage usage) {
        this._backend = backend;

        Guard.Assert((usage & ~(BufferUsage.IndexBuffer | BufferUsage.VertexBuffer)) == 0, "(usage & ~(BufferUsage.IndexBuffer | BufferUsage.VertexBuffer)) == 0");

        this.Usage = usage;
        
        this.SizeInBytes = byteSize;
    }

    public unsafe DeviceBuffer? ResetFromExistingBuffer(DeviceBuffer buffer) {
        this.ReservedBytes = 0;
        
        //Get the current buffer
        DeviceBuffer? old = this._buffer;
        
        Guard.Assert(buffer.Usage == (BufferUsage.Dynamic | this.Usage), "buffer.Usage == (BufferUsage.Dynamic | this.Usage)");
        
        //Set the current buffer to the new one
        this._buffer = buffer;
    
        //Unmap the old buffer
        if (old != null)
            this._backend.GraphicsDevice.Unmap(old);

        //Map the new buffer
        MappedResource map = this._backend.GraphicsDevice.Map(this._buffer, MapMode.Write);
        
        //Set the data ptr
        this.Ptr = (void*)map.Data;
    
        return old;
    }

    private int i = 0;
    public DeviceBuffer? ResetFromFreshBuffer() {
        BufferDescription desc = new((uint)this.SizeInBytes, BufferUsage.Dynamic | this.Usage);
        DeviceBuffer buf = this._backend.ResourceFactory.CreateBuffer(ref desc);

        buf.Name = this.i++.ToString();
    
        return this.ResetFromExistingBuffer(buf);
    }
    
    public override void Map() {
        Guard.Fail("We should not be here! Use the `Reset*` methods!");
    }
    
    public override void Unmap() {
        //Unmap the buffer
        this._backend.GraphicsDevice.Unmap(this._buffer);
        
        //Say we no longer are using any buffers
        this._buffer = null;
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