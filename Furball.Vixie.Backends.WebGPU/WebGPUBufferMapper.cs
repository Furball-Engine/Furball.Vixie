using System;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Silk.NET.Core.Native;
using Silk.NET.WebGPU;
using Buffer = Silk.NET.WebGPU.Buffer;

namespace Furball.Vixie.Backends.WebGPU;

public unsafe class WebGPUBufferMapper : BufferMapper {
    private readonly WebGPUBackend          _backend;
    private readonly BufferUsage            _usage;
    private readonly Silk.NET.WebGPU.WebGPU _webgpu;

    private void* Ptr;
    
    public WebGPUBufferMapper(WebGPUBackend backend, uint byteSize, BufferUsage usage) {
        this._backend = backend;
        this._usage   = usage;
        this._webgpu  = backend.WebGPU;

        Guard.Assert((usage & ~(BufferUsage.Index | BufferUsage.Vertex)) == 0,
                     "(usage & ~(BufferUsage.Index | BufferUsage.Vertex)) == 0");

        this.SizeInBytes = byteSize;

        this.Ptr = (void*)SilkMarshal.Allocate((int)byteSize);
    }

    public void CopyMappedDataToExistingBuffer(Buffer* buffer) {
        //TODO: readd this check once https://github.com/gfx-rs/wgpu-native/issues/227 is fixed
        // Guard.Assert((this._webgpu.BufferGetUsage(buffer) & BufferUsage.MapWrite) != 0,
        // "(this._webgpu.BufferGetUsage(buffer) | BufferUsage.MapWrite) != 0");
        
        this._webgpu.QueueWriteBuffer(this._backend.Queue, buffer, 0, this.Ptr, this.ReservedBytes);
        
        this.ReservedBytes = 0;
    }

    public Buffer* CopyMappedDataToNewBuffer() {
        Buffer* buffer = this._webgpu.DeviceCreateBuffer(this._backend.Device, new BufferDescriptor {
            Size             = this.SizeInBytes,
            Usage            = this._usage | BufferUsage.CopyDst,
            MappedAtCreation = false
        });

        this.CopyMappedDataToExistingBuffer(buffer);

        return buffer;
    }

    public override void Map() {
        //TODO: remap existing buffer, dont recreate
        // this._webgpu.BufferMapAsync(
            // this.MappedBuffer,
            // MapMode.Write,
            // 0,
            // this.SizeInBytes,
            // new PfnBufferMapCallback((status, ptr) => {
                // if (status != BufferMapAsyncStatus.Success)
                    // throw new Exception("Unable to map buffer!");

                // this.MappedBufferPtr = ptr;
            // }),
            // null
        // );

        // this._backend.WGPU.DevicePoll(this._backend.Device, true, null);

        // if(this.MappedBuffer != null)
            // this._backend.Disposal.Dispose(this.MappedBuffer);

        // this.MappedBuffer = null;
        
        // this.MappedBuffer = this._webgpu.DeviceCreateBuffer(this._backend.Device, new BufferDescriptor {
            // Size             = this.SizeInBytes,
            // MappedAtCreation = true,
            // Usage            = BufferUsage.MapWrite | BufferUsage.CopySrc
        // });

        // this.MappedBufferPtr = this._webgpu.BufferGetMappedRange(this.MappedBuffer, 0, this.SizeInBytes);
    }

    public override void Unmap() {
        // Guard.EnsureNonNull(this.MappedBuffer, "this.MappedBuffer");

        // this._webgpu.BufferUnmap(this.MappedBuffer);
        // this.MappedBufferPtr = null;

        //Unmap the buffer
        // this._webgpu.BufferUnmap(this.Buffer);

        // this._webgpu.BufferDestroy(this.Buffer);

        //Say we no longer are using any buffers
        // this.Buffer = null;
    }

    public override void* Reserve(nuint byteCount) {
        nuint ptr = (nuint)this.Ptr + this.ReservedBytes;

        //If this reserve will push us over the limit, return nullptr
        if (this.ReservedBytes + byteCount > this.SizeInBytes)
            return null;

        this.ReservedBytes += byteCount;

        return (void*)ptr;
    }

    private bool _isDisposed;
    protected override void DisposeInternal() {
        if (this._isDisposed)
            return;

        SilkMarshal.Free((nint)this.Ptr);
        
        this._isDisposed = true;
    }
}