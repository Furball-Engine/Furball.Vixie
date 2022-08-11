using System;
using System.Drawing;
using System.Numerics;
using FontStashSharp.Interfaces;
using Furball.Vixie.Backends.Shared.Backends;

namespace Furball.Vixie.Backends.Shared.FontStashSharp; 

public class VixieTexture2dManager : ITexture2DManager {
    private IGraphicsBackend _backend;

    public VixieTexture2dManager(IGraphicsBackend backend) {
        this._backend = backend;
    }

    public object CreateTexture(int width, int height) {
        Texture tex = this._backend.CreateEmptyTexture(
        (uint)width,
        (uint)height,
        new TextureParameters(true, TextureFilterType.Pixelated)
        );

#if DEBUG
        Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(tex));
#endif

        return tex;
    }

    public Point GetTextureSize(object texture) {
        // ReSharper disable once PossibleNullReferenceException
        Vector2 size = (texture as Texture).Size;

        return new Point((int) size.X, (int) size.Y);
    }

    public void SetTextureData(object texture, Rectangle bounds, byte[] data) {
        // ReSharper disable once PossibleNullReferenceException
        (texture as Texture).SetData(data, bounds);
    }
}