using System;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Silk.NET.WebGPU;
using Buffer = Silk.NET.WebGPU.Buffer;

namespace Furball.Vixie.Backends.WebGPU;

public unsafe class WebGPUBufferMapper : BufferMapper {
    private readonly WebGPUBackend          _backend;
    private readonly BufferUsage            _usage;
    private readonly Silk.NET.WebGPU.WebGPU _webgpu;

    public Buffer* Buffer;
    public void*   Ptr;

    public WebGPUBufferMapper(WebGPUBackend backend, uint byteSize, BufferUsage usage) {
        this._backend = backend;
        this._usage   = usage;
        this._webgpu  = backend.WebGPU;

        Guard.Assert((usage & ~(BufferUsage.Index | BufferUsage.Vertex)) == 0,
                     "(usage & ~(BufferUsage.Index | BufferUsage.Vertex)) == 0");

        this.SizeInBytes = byteSize;
    }

    public Buffer* ResetFromExistingBuffer(Buffer* buffer) {
        Guard.Assert((this._webgpu.BufferGetUsage(buffer) & BufferUsage.MapWrite) != 0,
                     "(this._webgpu.BufferGetUsage(buffer) | BufferUsage.MapWrite) != 0");

        this.ReservedBytes = 0;

        //Get the current buffer
        Buffer* old = this.Buffer;

        //Set the current buffer to the new one
        this.Buffer = buffer;

        //Unmap the old buffer
        if (old != null)
            this._webgpu.BufferUnmap(old);

        //Map the new buffer
        this._webgpu.BufferMapAsync(this.Buffer, MapMode.Write, 0, this.SizeInBytes, new PfnBufferMapCallback(
                                        (status, ptr) => {
                                            if (status != BufferMapAsyncStatus.Success)
                                                throw new Exception($"Failed to map buffer! Status: {status}");

                                            this.Ptr = ptr;
                                        }), null);

        return old;
    }

    public Buffer* ResetFromFreshBuffer() {
        Buffer* buffer = this._webgpu.DeviceCreateBuffer(this._backend.Device, new BufferDescriptor {
            Size             = this.SizeInBytes,
            Usage            = this._usage | BufferUsage.MapWrite,
            MappedAtCreation = false
        });

        return this.ResetFromExistingBuffer(buffer);
    }

    public override void Map() {
        Guard.Fail("We should not be here! Use the `Reset*` methods!");
    }

    public override void Unmap() {
        Guard.Assert(this.Buffer != null, "this.Buffer != null");

        //Unmap the buffer
        this._webgpu.BufferUnmap(this.Buffer);

        this._webgpu.BufferDestroy(this.Buffer);

        //Say we no longer are using any buffers
        this.Buffer = null;
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
        if (this.Buffer != null)
            this._webgpu.BufferDestroy(this.Buffer);
    }
}