using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;

namespace Furball.Vixie.Backends.WebGL; 

// ReSharper disable once InconsistentNaming
public class WebGLRenderer : VixieRenderer {
    public override void Begin() {
        throw new System.NotImplementedException();
    }
    public override void End() {
        throw new System.NotImplementedException();
    }
    public override MappedData Reserve(ushort vertexCount, uint indexCount) {
        throw new System.NotImplementedException();
    }
    public override long GetTextureId(VixieTexture tex) {
        throw new System.NotImplementedException();
    }
    public override void Draw() {
        throw new System.NotImplementedException();
    }
    protected override void DisposeInternal() {
        throw new System.NotImplementedException();
    }
}