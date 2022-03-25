using System.Drawing;
using System.Numerics;

namespace Furball.Vixie.Graphics.Backends.Veldrid.Abstractions {
    public class TextureVeldrid : Texture {
        public override Vector2 Size {
            get;
            protected set;
        }
        
        public override Texture SetData <pDataType>(int level, pDataType[] data)                   => throw new System.NotImplementedException();
        public override Texture SetData <pDataType>(int level, Rectangle   rect, pDataType[] data) => throw new System.NotImplementedException();
    }
}
