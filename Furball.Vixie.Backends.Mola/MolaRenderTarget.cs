using Furball.Vixie.Backends.Shared;
using Silk.NET.Maths;

namespace Furball.Vixie.Backends.Mola;

public sealed class MolaRenderTarget : VixieTextureRenderTarget {
    private readonly MolaBackend _backend;
    public readonly  MolaTexture Texture;
    public MolaRenderTarget(MolaBackend backend, int width, int height) {
        this._backend = backend;
        this.Texture  = new MolaTexture((uint)width, (uint)height);

        this.Size = new Vector2D<int>(width, height);
    }

    public override Vector2D<int> Size {
        get;
        protected set;
    }
    public override unsafe void Bind() {
        this._backend.BoundRenderTarget = this.Texture.RenderBitmap;
    }
    public override unsafe void Unbind() {
        this._backend.BoundRenderTarget = null;
    }
    public override VixieTexture GetTexture() {
        return this.Texture;
    }
}