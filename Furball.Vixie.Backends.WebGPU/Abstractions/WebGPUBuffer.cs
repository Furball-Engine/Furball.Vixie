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

    private bool _isDisposed = false;
    public void Dispose() {
        if (this._isDisposed)
            return;
        
        this._isDisposed = true;
        
        // if(this.Buffer != null)
            // this._webgpu.BufferDestroy(this.Buffer);

        this.Buffer = null;
    }

    ~WebGPUBuffer() {
        this.Dispose();
    }
}