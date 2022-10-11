using System;
using FontStashSharp.Interfaces;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.FontStashSharp;
using Furball.Vixie.Backends.Shared.Renderers;

namespace Furball.Vixie; 

public class Renderer {
    internal VixieRenderer VixieRenderer;

    public VixieFontStashRenderer FontRenderer => this.VixieRenderer.FontRenderer;

    public Renderer() {
        this.VixieRenderer = GraphicsBackend.Current.CreateRenderer();
        
        Global.TrackedRenderers.Add(new WeakReference<Renderer>(this));
    }

    public void Begin() {
        this.VixieRenderer.Begin();
    }
    
    public void End() {
        this.VixieRenderer.End();
    }

    public void Draw() {
        this.VixieRenderer.Draw();
    }
    
    public MappedData Reserve(ushort vertexCount, uint indexCount) {
        return this.VixieRenderer.Reserve(vertexCount, indexCount);
    }

    public long GetTextureId(VixieTexture tex) {
        return this.VixieRenderer.GetTextureId(tex);
    }
    
    public void Recreate() {
        this.VixieRenderer = GraphicsBackend.Current.CreateRenderer();
    }
    
    public void Dispose() {
        this.VixieRenderer.Dispose();
    }
    public void DisposeInternal() {
        this.VixieRenderer.Dispose();
    }
}