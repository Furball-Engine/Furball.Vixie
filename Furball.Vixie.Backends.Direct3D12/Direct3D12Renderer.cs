using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;

namespace Furball.Vixie.Backends.Direct3D12; 

public class Direct3D12Renderer : VixieRenderer {
    private readonly Direct3D12Backend _backend;
    
    private          CullFace          _cullFace;
    
    public Direct3D12Renderer(Direct3D12Backend backend) {
        this._backend = backend;
    }

    public override void Begin(CullFace cullFace = CullFace.CCW) {
        this._cullFace = cullFace;
    }
    
    public override void End() {
        
    }
    
    public override MappedData Reserve(ushort vertexCount, uint indexCount, VixieTexture tex) {
        return new MappedData();
    }
    
    public override void Draw() {
        
    }
    protected override void DisposeInternal() {
        
    }
}