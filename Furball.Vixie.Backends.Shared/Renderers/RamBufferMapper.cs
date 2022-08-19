using System;
using System.Runtime.InteropServices;

namespace Furball.Vixie.Backends.Shared.Renderers; 

public unsafe class RamBufferMapper : BufferMapper {
    public readonly void* Handle;
    
    public RamBufferMapper(nuint sizeInBytes) {
        this.SizeInBytes = sizeInBytes;

        this.Handle = (void*)Marshal.AllocHGlobal((int)sizeInBytes);
    }

    public void Reset() {
        this.ReservedBytes = 0;
    }
    public override void Map() {
        this.Reset();
    }
    public override void Unmap() {}
    
    public override void* Reserve(nuint byteCount) {
        nuint ptr = (nuint)this.Handle + this.ReservedBytes;

        //If this reserve will push us over the limit, return nullptr
        if (this.ReservedBytes + byteCount > this.SizeInBytes)
            return null;
        
        this.ReservedBytes += byteCount;

        return (void*)ptr;
    }

    protected override void DisposeInternal() {
        Marshal.FreeHGlobal((IntPtr)this.Handle);
    }

    ~RamBufferMapper() {
        this.Dispose();
    }
}