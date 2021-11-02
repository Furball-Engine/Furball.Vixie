using System;
using Silk.NET.OpenGL;
using Texture=Furball.Vixie.Gl.Texture;

namespace Furball.Vixie.Graphics {
    public class TextureRenderTarget {
        /// <summary>
        /// OpenGL API, used to shorten code.
        /// </summary>
        private GL gl;

        private uint _frameBufferId;
        private uint _textureId;
        private uint _depthRenderBufferId;

        private int[] _oldViewPort;
        private uint  _targetWidth;
        private uint  _targetHeight;

        public unsafe TextureRenderTarget(uint width, uint height) {
            this.gl = Global.Gl;

            this._frameBufferId = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, this._frameBufferId);

            this._textureId = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, this._textureId);
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);

            gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
            gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);

            this._depthRenderBufferId = gl.GenRenderbuffer();
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, this._depthRenderBufferId);
            gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent, width, height);
            gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, this._depthRenderBufferId);

            gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, this._textureId, 0);

            GLEnum[] drawBuffers = new GLEnum[1] {
                GLEnum.ColorAttachment0
            };
            gl.DrawBuffers(1, drawBuffers);

            if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete) {
                throw new Exception("Failed to create TextureRenderTarget!");
            }

            this._oldViewPort  = new int[4];
            this._targetWidth  = width;
            this._targetHeight = height;
        }

        public void Bind() {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, this._frameBufferId);
            //Store the old viewport for later
            gl.GetInteger(GetPName.Viewport, this._oldViewPort);
            gl.Viewport(0, 0, this._targetWidth, this._targetHeight);
        }

        public void Unbind() {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            gl.Viewport(this._oldViewPort[0], this._oldViewPort[1], (uint) this._oldViewPort[2], (uint) this._oldViewPort[3]);
        }

        public Texture GetTexture() => new Texture(this._textureId, this._targetWidth, this._targetHeight);
    }
}
