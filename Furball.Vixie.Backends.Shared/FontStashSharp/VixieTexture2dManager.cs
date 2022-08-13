using System;
using System.Drawing;
using System.Numerics;
using FontStashSharp.Interfaces;
using Furball.Vixie.Backends.Shared.Backends;
using Silk.NET.Maths;
using Rectangle = System.Drawing.Rectangle;

namespace Furball.Vixie.Backends.Shared.FontStashSharp; 

public class VixieTexture2dManager : ITexture2DManager {
    private IGraphicsBackend _backend;

    public VixieTexture2dManager(IGraphicsBackend backend) {
        this._backend = backend;
    }

    public object CreateTexture(int width, int height) {
        VixieTexture tex = this._backend.CreateEmptyTexture(
        (uint)width,
        (uint)height,
        new TextureParameters(true, TextureFilterType.Pixelated)
        );

        //TODO: readd this support
// #if DEBUG
//         Global.TRACKED_TEXTURES.Add(new WeakReference<VixieTexture>(tex));
// #endif

        return tex;
    }

    public Point GetTextureSize(object texture) {
        // ReSharper disable once PossibleNullReferenceException
        Vector2D<int> size = (texture as VixieTexture)!.Size;

        return new Point(size.X, size.Y);
    }

    public void SetTextureData(object texture, Rectangle bounds, byte[] data) {
        // ReSharper disable once PossibleNullReferenceException
        (texture as VixieTexture).SetData(data, bounds);
    }
}