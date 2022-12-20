using System;
using FontStashSharp.Interfaces;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.FontStashSharp;

namespace Furball.Vixie; 

public class Renderer {
    private readonly GraphicsBackend _backend;
    internal         VixieRenderer   VixieRenderer;

    public IFontStashRenderer2 FontRenderer {
        get => this.VixieRenderer.FontRenderer;
        set => this.VixieRenderer.FontRenderer = value;
    }

    public Renderer(GraphicsBackend backend) {
        this._backend      = backend;
        this.VixieRenderer = backend.CreateRenderer();
        
        Global.TrackedRenderers.Add(new WeakReference<Renderer>(this));

        this.FontRenderer = new VixieFontStashRenderer(this._backend, this);
    }

    public void Begin(CullFace cullFace = CullFace.CCW) {
        this.VixieRenderer.Begin(cullFace);
    }
    
    public void End() {
        this.VixieRenderer.End();
    }

    public void Draw() {
        this.VixieRenderer.Draw();
    }

    public static ulong VertexCount;
    public static ulong IndexCount;
    
    public MappedData Reserve(ushort vertexCount, uint indexCount, VixieTexture tex) {
        //NOTE: this is in an unchecked block to prevent crashes when the vertex/index count is too high
        //it shouldnt EVER reach that point, but this is for safety
        unchecked {
            VertexCount += vertexCount;
            IndexCount  += indexCount;
        }
    
        return this.VixieRenderer.Reserve(vertexCount, indexCount, tex);
    }

    public void Dispose() {
        this.VixieRenderer.Dispose();
    }
    public void DisposeInternal() {
        this.VixieRenderer.Dispose();
    }
}