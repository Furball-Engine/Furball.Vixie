using Furball.Vixie.Backends.Shared;
using Silk.NET.Maths;

namespace Furball.Vixie.Backends.WebGL; 

// ReSharper disable once InconsistentNaming
public sealed class WebGLRenderTarget : VixieTextureRenderTarget {
    public WebGLRenderTarget(uint width, uint height) {
        this.Size = new Vector2D<int>((int)width, (int)height);
    }
    public override Vector2D<int> Size {
        get;
        protected set;
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