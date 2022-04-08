using System.Drawing;
using System.IO;
using System.Numerics;
using Furball.Vixie.Graphics.Backends;

namespace Furball.Vixie.Graphics {
    public abstract class Texture {
        public abstract Vector2 Size { get; protected set; }
        public int Width => (int)Size.X;
        public int Height => (int)Size.Y;

        public abstract Texture SetData<pDataType>(int level, pDataType[] data) where pDataType : unmanaged;
        public abstract Texture SetData<pDataType>(int level, Rectangle rect, pDataType[] data) where pDataType : unmanaged;
    }
}
