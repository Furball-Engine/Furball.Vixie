using System.Numerics;
using Veldrid;
using Rectangle=System.Drawing.Rectangle;

namespace Furball.Vixie.Graphics.Backends.Veldrid.Abstractions {
    public class TextureVeldrid : Texture {
        public global::Veldrid.Texture Texture;
        
        public override Vector2 Size {
            get;
            protected set;
        }

        public        ResourceSet[]    ResourceSets    = new ResourceSet[VeldridBackend.MAX_TEXTURE_UNITS];
        public static ResourceLayout[] ResourceLayouts = new ResourceLayout[VeldridBackend.MAX_TEXTURE_UNITS];

        public ResourceSet GetResourceSet(VeldridBackend backend, int i) {
            return this.ResourceSets[i] ?? (this.ResourceSets[i] = backend.ResourceFactory.CreateResourceSet(new ResourceSetDescription(ResourceLayouts[i], this.Texture)));
        }
        
        public override Texture SetData <pDataType>(int level, pDataType[] data)                   => throw new System.NotImplementedException();
        public override Texture SetData <pDataType>(int level, Rectangle   rect, pDataType[] data) => throw new System.NotImplementedException();
    }
}
