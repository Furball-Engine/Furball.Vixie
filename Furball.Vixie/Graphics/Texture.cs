using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Furball.Vixie.Graphics {
    public class Texture : IDisposable {
        /// <summary>
        /// All the Currently Bound Textures
        /// </summary>
        internal static Dictionary<TextureUnit, uint> BoundTextures = new() {
            { TextureUnit.Texture0,  0 },
            { TextureUnit.Texture1,  0 },
            { TextureUnit.Texture2,  0 },
            { TextureUnit.Texture3,  0 },
            { TextureUnit.Texture4,  0 },
            { TextureUnit.Texture5,  0 },
            { TextureUnit.Texture6,  0 },
            { TextureUnit.Texture7,  0 },
            { TextureUnit.Texture8,  0 },
            { TextureUnit.Texture9,  0 },
            { TextureUnit.Texture10, 0 },
            { TextureUnit.Texture11, 0 },
            { TextureUnit.Texture12, 0 },
            { TextureUnit.Texture13, 0 },
            { TextureUnit.Texture14, 0 },
            { TextureUnit.Texture15, 0 },
            { TextureUnit.Texture16, 0 },
            { TextureUnit.Texture17, 0 },
            { TextureUnit.Texture18, 0 },
            { TextureUnit.Texture19, 0 },
            { TextureUnit.Texture20, 0 },
            { TextureUnit.Texture21, 0 },
            { TextureUnit.Texture22, 0 },
            { TextureUnit.Texture23, 0 },
            { TextureUnit.Texture24, 0 },
            { TextureUnit.Texture25, 0 },
            { TextureUnit.Texture26, 0 },
            { TextureUnit.Texture27, 0 },
            { TextureUnit.Texture28, 0 },
            { TextureUnit.Texture29, 0 },
            { TextureUnit.Texture30, 0 },
            { TextureUnit.Texture31, 0 },
        };

        public bool Bound => BoundTextures[this.BoundAt] == this.TextureId;

        internal TextureUnit BoundAt;

        /// <summary>
        /// OpenGL API, used to not write Global.Gl everytime
        /// </summary>
        private GL            gl;
        /// <summary>
        /// Unique ID which identifies this Texture
        /// </summary>
        internal uint          TextureId;
        /// <summary>
        /// Local Image, possibly useful to Sample on the CPU Side if necessary
        /// </summary>
        private Image<Rgba32> _localBuffer;
        /// <summary>
        /// Size of the Texture
        /// </summary>
        public Vector2 Size { get; private set; }

        /// <summary>
        /// Creates a Texture from a File
        /// </summary>
        /// <param name="filepath">Path to an Image</param>
        public unsafe Texture(string filepath) {
            this.gl = Global.Gl;

            Image<Rgba32> image = (Image<Rgba32>)Image.Load(filepath);
            //We need to flip our image as ImageSharps coordinates has origin 0, 0 in the top-left corner,
            //But OpenGL has it in the bottom left
            image.Mutate(x => x.Flip(FlipMode.Vertical));

            this._localBuffer = image;

            int width = image.Width;
            int height = image.Height;

            fixed (void* data = &MemoryMarshal.GetReference(image.GetPixelRowSpan(0))) {
                this.Load(data, width, height);
            }

            this.Size = new Vector2(width, height);
        }
        /// <summary>
        /// Creates a Texture from a byte array which contains Image Data
        /// </summary>
        /// <param name="imageData">Image Data</param>
        public unsafe Texture(byte[] imageData) {
            this.gl = Global.Gl;

            Image<Rgba32> image = Image.Load(imageData);
            //We need to flip our image as ImageSharps coordinates has origin 0, 0 in the top-left corner,
            //But OpenGL has it in the bottom left
            image.Mutate(x => x.Flip(FlipMode.Vertical));

            this._localBuffer = image;

            int width = image.Width;
            int height = image.Height;

            fixed (void* data = &MemoryMarshal.GetReference(image.GetPixelRowSpan(0))) {
                this.Load(data, width, height);
            }

            this.Size = new Vector2(width, height);
        }

        public unsafe Texture() {
            this.gl = Global.Gl;

            this.TextureId = this.gl.GenTexture();
            //Bind as we will be working on the Texture
            this.gl.BindTexture(TextureTarget.Texture2D, this.TextureId);
            //Apply Linear filtering, and make Image wrap around and repeat
            this.gl.TexParameter(GLEnum.Texture2D,        TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
            this.gl.TexParameter(GLEnum.Texture2D,        TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            this.gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapS,                   (int) GLEnum.Repeat);
            this.gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapT,                   (int) GLEnum.Repeat);
            //White color
            uint color = 0xffffffff;
            //Upload Image Data
            this.gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba8, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, &color);
            //Unbind as we have finished
            this.gl.BindTexture(TextureTarget.Texture2D, 0);

            this.Size = new Vector2(1, 1);
        }

        public unsafe Texture(uint width, uint height) {
            this.gl = Global.Gl;

            this.TextureId = this.gl.GenTexture();
            //Bind as we will be working on the Texture
            this.gl.BindTexture(TextureTarget.Texture2D, this.TextureId);
            //Apply Linear filtering, and make Image wrap around and repeat
            this.gl.TexParameter(GLEnum.Texture2D,        TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
            this.gl.TexParameter(GLEnum.Texture2D,        TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            this.gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapS,                   (int) GLEnum.Repeat);
            this.gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapT,                   (int) GLEnum.Repeat);
            //Upload Image Data
            this.gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba8, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            //Unbind as we have finished
            this.gl.BindTexture(TextureTarget.Texture2D, 0);

            this.Size = new Vector2(width, height);
        }

        /// <summary>
        /// Creates a Texture from a Stream which Contains Image Data
        /// </summary>
        /// <param name="stream">Image Data Stream</param>
        public unsafe Texture(Stream stream) {
            this.gl = Global.Gl;

            Image<Rgba32> image = (Image<Rgba32>) Image.Load(stream);
            //We need to flip our image as ImageSharps coordinates has origin 0, 0 in the top-left corner,
            //But OpenGL has it in the bottom left
            image.Mutate(x => x.Flip(FlipMode.Vertical));

            this._localBuffer = image;

            int width = image.Width;
            int height = image.Height;

            fixed (void* data = &MemoryMarshal.GetReference(image.GetPixelRowSpan(0))) {
                this.Load(data, width, height);
            }

            this.Size = new Vector2(width, height);
        }

        internal Texture(uint textureId, uint width, uint height) {
            this.gl = Global.Gl;

            this.TextureId = textureId;
            this.Size       = new Vector2(width, height);
        }

        ~Texture() {
            this.Dispose();
        }

        /// <summary>
        /// Generates the Texture on the GPU, Sets Parameters and Uploads the Image Data
        /// </summary>
        /// <param name="data">Image Data</param>
        /// <param name="width">Width of Image</param>
        /// <param name="height">Height of Imgae</param>
        private unsafe void Load(void* data, int width, int height) {
            this.TextureId = this.gl.GenTexture();
            //Bind as we will be working on the Texture
            this.gl.BindTexture(TextureTarget.Texture2D, this.TextureId);
            //Apply Linear filtering, and make Image wrap around and repeat
            this.gl.TexParameter(GLEnum.Texture2D,        TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
            this.gl.TexParameter(GLEnum.Texture2D,        TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            this.gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapS,                   (int) GLEnum.Repeat);
            this.gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapT,                   (int) GLEnum.Repeat);
            //Upload Image Data
            this.gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            //Unbind as we have finished
            this.gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        public unsafe Texture SetData<pDataType>(int level, pDataType[] data) where pDataType : unmanaged {
            this.LockingBind();

            fixed(void* d = data)
                this.gl.TexImage2D(TextureTarget.Texture2D, level, InternalFormat.Rgba, (uint) this.Size.X, (uint) this.Size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, d);

            this.gl.Finish();

            this.UnlockingUnbind();

            return this;
        }

        public unsafe Texture SetData<pDataType>(int level, System.Drawing.Rectangle rect, pDataType[] data) where pDataType : unmanaged {
            this.LockingBind();

            fixed(void* d = data)
                this.gl.TexSubImage2D(TextureTarget.Texture2D, level, rect.X, rect.Y, (uint) rect.Width, (uint) rect.Height, PixelFormat.Rgba, PixelType.UnsignedByte, d);

            this.UnlockingUnbind();

            return this;
        }

        /// <summary>
        /// Binds the Texture to a certain Texture Slot
        /// </summary>
        /// <param name="textureSlot">Desired Texture Slot</param>
        /// <returns>Self, used for chaining methods</returns>
        public Texture Bind(TextureUnit textureSlot = TextureUnit.Texture0) {
            if (this.Locked)
                return null;

            this.gl.ActiveTexture(textureSlot);
            this.gl.BindTexture(TextureTarget.Texture2D, this.TextureId);

            BoundTextures[textureSlot] = this.TextureId;
            this.BoundAt               = textureSlot;

            return this;
        }

        /// <summary>
        /// Indicates whether Object is Locked or not,
        /// This is done internally to not be able to switch Textures while a Batch is happening
        /// or really anything that would possibly get screwed over by switching Textures
        /// </summary>
        internal bool Locked = false;

        /// <summary>
        /// Binds and sets a Lock so that the Texture cannot be unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal Texture LockingBind() {
            this.Bind();
            this.Lock();

            return this;
        }
        /// <summary>
        /// Locks the Texture so that other Textures cannot be bound/unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal Texture Lock() {
            this.Locked = true;

            return this;
        }
        /// <summary>
        /// Unlocks the Texture, so that other Textures can be bound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal Texture Unlock() {
            this.Locked = false;

            return this;
        }
        /// <summary>
        /// Uninds and unlocks the Texture so that other Textures can be bound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal Texture UnlockingUnbind() {
            this.Unlock();
            this.Unbind();

            return this;
        }

        /// <summary>
        /// Unbinds the Texture
        /// </summary>
        /// <returns>Self, used for chaining methods</returns>
        public Texture Unbind() {
            if (this.Locked)
                return null;

            this.gl.ActiveTexture(this.BoundAt);
            this.gl.BindTexture(TextureTarget.Texture2D, 0);

            BoundTextures[this.BoundAt] = this.TextureId;

            return this;
        }
        /// <summary>
        /// Gets the OpenGL texture ID
        /// </summary>
        /// <returns>Texture ID</returns>
        internal uint GetTextureId() => this.TextureId;

        /// <summary>
        /// Disposes the Texture and the Local Image Buffer
        /// </summary>
        public void Dispose() {
            if (this.Bound)
                this.UnlockingUnbind();

            try {
                this.gl.DeleteTexture(this.TextureId);
                this._localBuffer.Dispose();
            }
            catch {

            }
        }
    }
}
