using System;
using Buffer = Silk.NET.WebGPU.Buffer;

namespace Furball.Vixie.Backends.WebGPU.Abstractions;

public unsafe class WebGPUBuffer : IDisposable {
    private readonly Silk.NET.WebGPU.WebGPU _webgpu;
    public           Buffer*                Buffer;

    public WebGPUBuffer(Silk.NET.WebGPU.WebGPU webgpu, Buffer* buffer) {
        this._webgpu = webgpu;
        this.Buffer  = buffer;
    }

    public void Dispose() {
        this._webgpu.BufferDestroy(this.Buffer);
    }
}