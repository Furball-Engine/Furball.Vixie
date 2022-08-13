using System.Drawing;
using FontStashSharp.Interfaces;
using Silk.NET.Maths;
using Rectangle = System.Drawing.Rectangle;

namespace Furball.Vixie.FontStashSharp; 

public class VixieTexture2dManager : ITexture2DManager {
    public object CreateTexture(int width, int height) {
        Texture tex = Texture.CreateEmptyTexture(
            (uint)width,
            (uint)height
        );

        return tex;
    }

    public Point GetTextureSize(object texture) {
        // ReSharper disable once PossibleNullReferenceException
        Vector2D<int> size = ((Texture)texture).Size;

        return new Point(size.X, size.Y);
    }

    public void SetTextureData(object texture, Rectangle bounds, byte[] data) {
        // ReSharper disable once PossibleNullReferenceException
        ((Texture)texture).SetData(data, bounds);
    }
}