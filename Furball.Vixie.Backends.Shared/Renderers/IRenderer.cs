using System;
using Furball.Vixie.Backends.Shared.FontStashSharp;
using Furball.Vixie.Helpers;

namespace Furball.Vixie.Backends.Shared.Renderers; 

public abstract class Renderer : IDisposable {
    /// <summary>
    /// Begins collecting draw calls
    /// </summary>
    public abstract void Begin();
    /// <summary>
    /// Ends collecting draw calls
    /// </summary>
    public abstract void End();

    /// <summary>
    /// Reserves a set amount of vertexes and indices from the renderer
    /// </summary>
    /// <remarks>
    /// The pointers you get back are *NOT* bounds checked, they are raw pointers to data in memory, you should always
    /// make sure that you do not using data not assigned to you
    /// </remarks>
    /// <param name="vertexCount">The amount of vertexes to reserve</param>
    /// <param name="indexCount">Thn amount of indexes to reserve</param>
    /// <returns>The mapped data pointers</returns>
    public abstract MappedData Reserve(ushort vertexCount, uint indexCount);

    /// <summary>
    /// Gets the texture id to put in your vertex definition
    /// </summary>
    /// <param name="tex">The texture to get the ID for</param>
    /// <returns>The texture id of the texture</returns>
    public abstract long GetTextureId(VixieTexture tex);
    
    /// <summary>
    /// Draws the collected buffers to the screen, can be run as many times as you want
    /// </summary>
    public abstract void Draw();

    private bool _isDisposed;
    public void Dispose() {
        if (this._isDisposed)
            return;

        this._isDisposed = true;

        this.DisposeInternal();
        
        GC.SuppressFinalize(this);
    }

    protected abstract void DisposeInternal();

    public VixieFontStashRenderer? FontRenderer;

    ~Renderer() {
        DisposeQueue.Enqueue(this);
    }
}