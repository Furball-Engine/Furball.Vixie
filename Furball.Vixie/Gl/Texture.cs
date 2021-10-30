using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Furball.Vixie.Gl {
    public class Texture : IDisposable {
        private GL            gl;
        private uint          _textureId;
        private Image<Rgba32> _localBuffer;

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

        private unsafe void Load(void* data, int width, int height) {
            this._textureId = gl.GenTexture();

            gl.BindTexture(TextureTarget.Texture2D, this._textureId);

            gl.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
            gl.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapS, (int) GLEnum.Repeat);
            gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapT, (int) GLEnum.Repeat);

            gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

            gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        public Texture Bind(TextureUnit textureSlot = TextureUnit.Texture0) {
            gl.ActiveTexture(textureSlot);
            gl.BindTexture(TextureTarget.Texture2D, this._textureId);

            return this;
        }

        public Texture Unbind() {
            gl.BindTexture(TextureTarget.Texture2D, 0);

            return this;
        }
        public void Dispose() {
            gl.DeleteTexture(this._textureId);
            this._localBuffer.Dispose();
        }
    }
}
