using System;

namespace Furball.Vixie.Backends.Shared.Renderers;

public abstract unsafe class BufferMapper : IDisposable {
    public nuint ReservedBytes {
        get;
        protected set;
    } = 0;
    public nuint SizeInBytes {
        get;
        protected set;
    }

    public abstract void Map();
    public abstract void Unmap();
    
    public abstract void* Reserve(nuint byteCount);

    private bool _isDisposed;
    public void Dispose() {
        if (this._isDisposed)
            return;
        
        this._isDisposed = true;
        
        this.DisposeInternal();
    }
    protected abstract void DisposeInternal();
}