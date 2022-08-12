using System.Numerics;
using Furball.Vixie.Backends.Shared;

namespace Furball.Vixie.Backends.Direct3D9; 

public class RenderTargetD3D9 : TextureRenderTarget {
    public RenderTargetD3D9(uint width, uint height) {
        throw new System.NotImplementedException();
    }
    
    public override Vector2 Size {
        get;
        protected set;
    }
    public override void Bind() {
        throw new System.NotImplementedException();
    }
    public override void Unbind() {
        throw new System.NotImplementedException();
    }
    public override Texture GetTexture() {
        throw new System.NotImplementedException();
    }
}