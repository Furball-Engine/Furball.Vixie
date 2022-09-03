using System.Drawing;
using FontStashSharp.Interfaces;
using Furball.Vixie.Backends.Shared.Backends;
using Silk.NET.Maths;
using Rectangle = System.Drawing.Rectangle;

namespace Furball.Vixie.Backends.Shared.FontStashSharp; 

public class VixieTexture2dManager : ITexture2DManager {
    private readonly GraphicsBackend _backend;
    public VixieTexture2dManager(GraphicsBackend backend) {
        this._backend = backend;
    }
    
    public object CreateTexture(int width, int height) {
        VixieTexture tex = this._backend.CreateEmptyTexture(
            (uint)width,
            (uint)height
        );

        return tex;
    }

    public Point GetTextureSize(object texture) {
        // ReSharper disable once PossibleNullReferenceException
        Vector2D<int> size = ((VixieTexture)texture).Size;

        return new Point(size.X, size.Y);
    }

    public void SetTextureData(object texture, Rectangle bounds, byte[] data) {
        // ReSharper disable once PossibleNullReferenceException
        ((VixieTexture)texture).SetData<byte>(data, bounds);
    }
}