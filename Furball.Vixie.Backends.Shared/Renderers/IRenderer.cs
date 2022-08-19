﻿using System;
using Furball.Vixie.Helpers;

namespace Furball.Vixie.Backends.Shared.Renderers; 

public abstract class IRenderer : IDisposable {
    public abstract void Begin();
    public abstract void End();

    public abstract MappedData Reserve(ushort vertexCount, uint indexCount);

    public abstract int GetTextureId(VixieTexture tex);
    
    public abstract void Draw();

    private bool _isDisposed;
    public void Dispose() {
        if (this._isDisposed)
            return;

        this._isDisposed = true;

        this.DisposeInternal();
    }

    protected abstract void DisposeInternal();

    ~IRenderer() {
        DisposeQueue.Enqueue(this);
    }
}