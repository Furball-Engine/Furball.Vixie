using System;
using System.Numerics;
using FontStashSharp;
using Furball.Vixie.Backends.Shared.FontStashSharp;
using Furball.Vixie.Helpers;

namespace Furball.Vixie.Backends.Shared.Renderers; 

public abstract class Renderer : IDisposable {
    public abstract void Begin();
    public abstract void End();

    public abstract MappedData Reserve(ushort vertexCount, uint indexCount);

    public abstract long GetTextureId(VixieTexture tex);
    
    public abstract void Draw();

    private bool _isDisposed;
    public void Dispose() {
        if (this._isDisposed)
            return;

        this._isDisposed = true;

        this.DisposeInternal();
    }

    protected abstract void DisposeInternal();

    public VixieFontStashRenderer FontRenderer;

    ~Renderer() {
        DisposeQueue.Enqueue(this);
    }
}