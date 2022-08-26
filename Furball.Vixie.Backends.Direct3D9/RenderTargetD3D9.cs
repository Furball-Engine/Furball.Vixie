using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Silk.NET.Maths;

namespace Furball.Vixie.Backends.Direct3D9; 

public class RenderTargetD3D9 : VixieTextureRenderTarget {
    public RenderTargetD3D9(uint width, uint height) {
        throw new System.NotImplementedException();
    }
    
    public override Vector2D<int> Size {
        get {
            return Vector2D<int>.One;
        }
        protected set {

        }
    }
    public override void Bind() {
        throw new System.NotImplementedException();
    }
    public override void Unbind() {
        throw new System.NotImplementedException();
    }
    public override VixieTexture GetTexture() {
        throw new System.NotImplementedException();
    }
}