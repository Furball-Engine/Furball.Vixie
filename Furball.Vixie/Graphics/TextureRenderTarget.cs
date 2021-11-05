using System;
using Silk.NET.OpenGL;
using Texture=Furball.Vixie.Graphics.Texture;

namespace Furball.Vixie.Graphics {
    public class TextureRenderTarget {
        /// <summary>
        /// OpenGL API, used to shorten code.
        /// </summary>
        private GL gl;

        /// <summary>
        /// Unique ID of this FrameBuffer
        /// </summary>
        private uint _frameBufferId;
        /// <summary>
        /// Texture ID of the Texture that this RenderTarget draws to
        /// </summary>
        private uint _textureId;
        /// <summary>
        /// Depth Buffer of this RenderTarget
        /// </summary>
        private uint _depthRenderBufferId;

        /// <summary>
        /// When binding, it saves the old viewport here so it can reset it upon Unbinding
        /// </summary>
        private int[] _oldViewPort;
        /// <summary>
        /// The RenderTarget Width
        /// </summary>
        private uint  _targetWidth;
        /// <summary>
        /// The RenderTarget Height
        /// </summary>
        private uint  _targetHeight;
        /// <summary>
        /// Creates a TextureRenderTarget
        /// </summary>
        /// <param name="width">Desired Width</param>
        /// <param name="height">Desired Width</param>
        /// <exception cref="Exception">Throws Exception if the Target didn't create properly</exception>
        public unsafe TextureRenderTarget(uint width, uint height) {
            this.gl = Global.Gl;

            //Generate and bind a FrameBuffer
            this._frameBufferId = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, this._frameBufferId);

            //Generate a Texture
            this._textureId = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, this._textureId);
            //Set it to Empty
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
            //Set The Filtering to nearest (apperantly necessary, idk)
            gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
            gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);

            //Generate the Depth buffer
            this._depthRenderBufferId = gl.GenRenderbuffer();
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, this._depthRenderBufferId);
            gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent, width, height);
            gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, this._depthRenderBufferId);
            //Connect the bound texture to the FrameBuffer object
            gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, this._textureId, 0);

            GLEnum[] drawBuffers = new GLEnum[1] {
                GLEnum.ColorAttachment0
            };
            gl.DrawBuffers(1, drawBuffers);

            //Check if FrameBuffer created successfully
            if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete) {
                throw new Exception("Failed to create TextureRenderTarget!");
            }

            this._oldViewPort  = new int[4];
            this._targetWidth  = width;
            this._targetHeight = height;
        }
        /// <summary>
        /// Binds the Target, from now on drawing will draw to this RenderTarget,
        /// </summary>
        public void Bind() {
            if (this._locked)
                return;

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, this._frameBufferId);
            //Store the old viewport for later
            gl.GetInteger(GetPName.Viewport, this._oldViewPort);
            gl.Viewport(0, 0, this._targetWidth, this._targetHeight);
        }

        private bool _locked = false;

        /// <summary>
        /// Binds and sets a Lock so that the Target cannot be unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal TextureRenderTarget LockingBind() {
            this.Bind();
            this.Lock();

            return this;
        }
        /// <summary>
        /// Locks the Target so that other Targets cannot be bound/unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal TextureRenderTarget Lock() {
            this._locked = true;

            return this;
        }
        /// <summary>
        /// Unlocks the Target, so that other Targets can be bound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal TextureRenderTarget Unlock() {
            this._locked = false;

            return this;
        }
        /// <summary>
        /// Uninds and unlocks the Target so that other Targets can be bound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal TextureRenderTarget UnlockingUnbind() {
            this.Unlock();
            this.Unbind();

            return this;
        }

        /// <summary>
        /// Unbinds the Target and resets the Viewport, drawing is now back to normal
        /// </summary>
        public void Unbind() {
            if (this._locked)
                return;

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            gl.Viewport(this._oldViewPort[0], this._oldViewPort[1], (uint) this._oldViewPort[2], (uint) this._oldViewPort[3]);
        }
        /// <summary>
        /// Retrieves the Texture from this RenderTarget
        /// </summary>
        /// <returns>Texture of this RenderTarget</returns>
        public Texture GetTexture() => new Texture(this._textureId, this._targetWidth, this._targetHeight);
    }
}
