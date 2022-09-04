using Furball.Vixie.Backends.Shared;
using Kettu;
using Silk.NET.Maths;

namespace Furball.Vixie.Backends.Dummy;

public class DummyTextureRenderTarget : VixieTextureRenderTarget {
    public DummyTextureRenderTarget(int w, int h) {
        this.Size = new Vector2D<int>(w, h);
        Logger.Log($"Creating Dummy texture({w}x{h})", LoggerLevelDummy.InstanceInfo);
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
        return new DummyTexture(default, this.Size.X, this.Size.Y);
    }
}