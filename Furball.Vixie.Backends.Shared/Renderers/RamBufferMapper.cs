using System;
using System.Runtime.InteropServices;

namespace Furball.Vixie.Backends.Shared.Renderers; 

public unsafe class RamBufferMapper : BufferMapper {
    public readonly void* Handle;
    
    public RamBufferMapper(nuint sizeInBytes) {
        this.SizeInBytes = sizeInBytes;

        this.Handle = (void*)Marshal.AllocHGlobal((int)sizeInBytes);
    }

    public override void Map() {
        this.ReservedBytes = 0;
    }
    public override void Unmap() {}
    
    public override void* Reserve(nuint byteCount) {
        nuint ptr = (nuint)this.Handle + this.ReservedBytes;

        this.ReservedBytes += byteCount;

        if (this.ReservedBytes > this.SizeInBytes)
            return null;

        return (void*)ptr;
    }

    protected override void DisposeInternal() {
        Marshal.FreeHGlobal((IntPtr)this.Handle);
    }

    ~RamBufferMapper() {
        this.Dispose();
    }
}