using System;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;

namespace Furball.Vixie.Backends.OpenGL; 

public class FixedFunctionOpenGLRenderer : Renderer {
    public override void Begin() {
        Guard.Todo("Implement FFP Begin");
    }
    public override void End() {
        Guard.Todo("Implement FFP End");
    }
    public override MappedData Reserve(ushort vertexCount, uint indexCount) {
        Guard.Todo("Implement FFP Reserve");

        return new MappedData();
    }
    public override long GetTextureId(VixieTexture tex) {
        Guard.Todo("Implement FFP GetTextureId");

        return -1;
    }
    public override void Draw() {
        Guard.Todo("Implement FFP Draw");
    }
    protected override void DisposeInternal() {
        
    }
}