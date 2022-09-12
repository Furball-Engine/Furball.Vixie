using System;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;

namespace Furball.Vixie.Backends.Mola; 

public class MolaRenderer : Renderer {
    private readonly MolaBackend _backend;
    public MolaRenderer(MolaBackend backend) {
        this._backend = backend;
    }
    public override void Begin() {
        throw new NotImplementedException();
    }
    public override void End() {
        throw new NotImplementedException();
    }
    public override MappedData Reserve(ushort vertexCount, uint indexCount) {
        throw new NotImplementedException();
    }
    public override long GetTextureId(VixieTexture tex) {
        throw new NotImplementedException();
    }
    public override void Draw() {
        throw new NotImplementedException();
    }
    protected override void DisposeInternal() {
        throw new NotImplementedException();
    }
}