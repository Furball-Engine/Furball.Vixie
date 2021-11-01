using System;
using System.IO;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Furball.Vixie.Gl {
    public class Texture : IDisposable {
        /// <summary>
        /// OpenGL API, used to not write Global.Gl everytime
        /// </summary>
        private GL            gl;
        /// <summary>
        /// Unique ID which identifies this Texture
        /// </summary>
        private uint          _textureId;
        /// <summary>
        /// Local Image, possibly useful to Sample on the CPU Side if necessary
        /// </summary>
        private Image<Rgba32> _localBuffer;

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

            fixed (void* data = &MemoryMarshal.GetReference(image.GetPixelRowSpan(0))) {
                this.Load(data, image.Width, image.Height);
            }
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

            fixed (void* data = &MemoryMarshal.GetReference(image.GetPixelRowSpan(0))) {
                this.Load(data, image.Width, image.Height);
            }
        }

        public unsafe Texture() {
            this.gl = Global.Gl;

            this._textureId = gl.GenTexture();
            //Bind as we will be working on the Texture
            gl.BindTexture(TextureTarget.Texture2D, this._textureId);
            //Apply Linear filtering, and make Image wrap around and repeat
            gl.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
            gl.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapS, (int) GLEnum.Repeat);
            gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapT, (int) GLEnum.Repeat);
            //White color
            uint color = 0xffffffff;
            //Upload Image Data
            gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba8, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, &color);
            //Unbind as we have finished
            gl.BindTexture(TextureTarget.Texture2D, 0);
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

            fixed (void* data = &MemoryMarshal.GetReference(image.GetPixelRowSpan(0))) {
                this.Load(data, image.Width, image.Height);
            }
        }
        /// <summary>
        /// Generates the Texture on the GPU, Sets Parameters and Uploads the Image Data
        /// </summary>
        /// <param name="data">Image Data</param>
        /// <param name="width">Width of Image</param>
        /// <param name="height">Height of Imgae</param>
        private unsafe void Load(void* data, int width, int height) {
            this._textureId = gl.GenTexture();
            //Bind as we will be working on the Texture
            gl.BindTexture(TextureTarget.Texture2D, this._textureId);
            //Apply Linear filtering, and make Image wrap around and repeat
            gl.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
            gl.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapS, (int) GLEnum.Repeat);
            gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapT, (int) GLEnum.Repeat);
            //Upload Image Data
            gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            //Unbind as we have finished
            gl.BindTexture(TextureTarget.Texture2D, 0);
        }
        /// <summary>
        /// Binds the Texture to a certain Texture Slot
        /// </summary>
        /// <param name="textureSlot">Desired Texture Slot</param>
        /// <returns>Self, used for chaining methods</returns>
        public Texture Bind(TextureUnit textureSlot = TextureUnit.Texture0) {
            gl.ActiveTexture(textureSlot);
            gl.BindTexture(TextureTarget.Texture2D, this._textureId);

            return this;
        }
        /// <summary>
        /// Unbinds the Texture
        /// </summary>
        /// <returns>Self, used for chaining methods</returns>
        public Texture Unbind() {
            gl.BindTexture(TextureTarget.Texture2D, 0);

            return this;
        }
        /// <summary>
        /// Gets the OpenGL texture ID
        /// </summary>
        /// <returns>Texture ID</returns>
        internal uint GetTextureId() => this._textureId;

        /// <summary>
        /// Disposes the Texture and the Local Image Buffer
        /// </summary>
        public void Dispose() {
            gl.DeleteTexture(this._textureId);
            this._localBuffer.Dispose();
        }
    }
}
