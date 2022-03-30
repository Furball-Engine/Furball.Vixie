using System.IO;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Rectangle=System.Drawing.Rectangle;

namespace Furball.Vixie.Graphics.Backends.Veldrid.Abstractions {
    public class TextureVeldrid : Texture {
        public global::Veldrid.Texture Texture;
        
        public override Vector2 Size {
            get;
            protected set;
        }

        public bool IsFbAndShouldFlip = false;

        internal int UsedId = -1;

        public           ResourceSet[]    ResourceSets    = new ResourceSet[VeldridBackend.MAX_TEXTURE_UNITS];
        public static    ResourceLayout[] ResourceLayouts = new ResourceLayout[VeldridBackend.MAX_TEXTURE_UNITS];
        
        private readonly VeldridBackend _backend;
        private readonly Image<Rgba32>  _localBuffer;

        public ResourceSet GetResourceSet(VeldridBackend backend, int i) {
            return this.ResourceSets[i] ?? (this.ResourceSets[i] = backend.ResourceFactory.CreateResourceSet(new ResourceSetDescription(ResourceLayouts[i], this.Texture)));
        }
        
        public unsafe TextureVeldrid(VeldridBackend backend, string filepath) {
            this._backend = backend;

            Image<Rgba32> image = (Image<Rgba32>)Image.Load(filepath);

            this._localBuffer = image;

            int width = image.Width;
            int height = image.Height;

            this.Load(image);

            this.Size = new Vector2(width, height);
        }
        
        private unsafe void Load(Image<Rgba32> image) {
            TextureDescription textureDescription = TextureDescription.Texture2D((uint)image.Width, (uint)image.Height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled | TextureUsage.RenderTarget);

            this.Texture = this._backend.ResourceFactory.CreateTexture(textureDescription);
            
            image.ProcessPixelRows(accessor => {
                for (int i = 0; i < accessor.Height; i++)
                    this._backend.GraphicsDevice.UpdateTexture(this.Texture, accessor.GetRowSpan(i), 0, (uint) i, 0, (uint) image.Width, 1, 1, 0, 0);
            });
        }
        
        /// <summary>
        /// Creates a Texture from a byte array which contains Image Data
        /// </summary>
        /// <param name="imageData">Image Data</param>
        public unsafe TextureVeldrid(VeldridBackend backend, byte[] imageData, bool qoi = false) {
            this._backend = backend;

            Image<Rgba32> image;

            if(qoi) {
                (Rgba32[] pixels, QoiLoader.QoiHeader header) data  = QoiLoader.Load(imageData);

                image = Image.LoadPixelData(data.pixels, (int)data.header.Width, (int)data.header.Height);
            } else {
                image = Image.Load<Rgba32>(imageData);
            }

            this._localBuffer = image;

            int width = image.Width;
            int height = image.Height;

            this.Load(image);

            this.Size = new Vector2(width, height);
        }
        /// <summary>
        /// Creates a Texture with a single White Pixel
        /// </summary>
        public unsafe TextureVeldrid(VeldridBackend backend) {
            this._backend = backend;
            // this._backend.CheckThread();

            Image<Rgba32> px = new(1, 1, new Rgba32(255, 255, 255, 255));

            this._localBuffer = px;
            this.Load(px);
            
            this.Size = new Vector2(1, 1);
        }
        /// <summary>
        /// Creates a Empty texture given a width and height
        /// </summary>
        /// <param name="width">Desired Width</param>
        /// <param name="height">Desired Height</param>
        public unsafe TextureVeldrid(VeldridBackend backend, uint width, uint height) {
            this._backend = backend;
            // this._backend.CheckThread();
            
            Image<Rgba32> px = new((int)width, (int)height, new Rgba32(0, 0, 0, 0));

            this._localBuffer = px;
            this.Load(px);

            this.Size = new Vector2(width, height);
        }
        /// <summary>
        /// Creates a Texture from a Stream which Contains Image Data
        /// </summary>
        /// <param name="stream">Image Data Stream</param>
        public unsafe TextureVeldrid(VeldridBackend backend, Stream stream) {
            this._backend = backend;

            Image<Rgba32> image = Image.Load<Rgba32>(stream);

            this._localBuffer = image;

            int width = image.Width;
            int height = image.Height;

            this.Load(image);

            this.Size = new Vector2(width, height);
        }

        public override Texture SetData <pDataType>(int level, pDataType[] data) {
            this._backend.GraphicsDevice.UpdateTexture(this.Texture, data, 0, 0, 0, this.Texture.Width, this.Texture.Height, 1, 0, (uint)level);
            
            return this;
        }
        public override Texture SetData <pDataType>(int level, Rectangle rect, pDataType[] data) {
            this._backend.GraphicsDevice.UpdateTexture(this.Texture, data, (uint)rect.X, (uint)rect.Y, 0, (uint)rect.Width, (uint)rect.Height, 1, 0, (uint)level);
            
            return this;
        }

        ~TextureVeldrid() {
            this.Dispose();
        }
        
        private bool IsDisposed = false;
        public void Dispose() {
            if (this.IsDisposed) return;
            this.IsDisposed = true;
            
            this.Texture.Dispose();
            foreach (ResourceSet resourceSet in this.ResourceSets) {
                resourceSet?.Dispose();
            }
            
            this._localBuffer.Dispose();
        }
    }
}
