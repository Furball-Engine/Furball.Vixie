using Furball.Vixie.Backends.Shared;
using Silk.NET.Maths;

namespace Furball.Vixie.Backends.Dummy;

public class DummyTextureRenderTarget : VixieTextureRenderTarget {
    public DummyTextureRenderTarget(int w, int h) {
        this.Size = new Vector2D<int>(w, h);
    }
    public sealed override Vector2D<int> Size {
        get;
        protected set;
    }
    public override void Bind() {
        // throw new System.NotImplementedException();
    }
    public override void Unbind() {
        // throw new System.NotImplementedException();
    }
    public override VixieTexture GetTexture() {
        return new DummyTexture(this.Size.X, this.Size.Y);
    }
}