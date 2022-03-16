using System;
using System.Numerics;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGLES;

namespace Furball.Vixie.Graphics.Backends.OpenGL.Abstractions {
    public class TextureRenderTargetGL : TextureRenderTarget, IDisposable {
        /// <summary>
        /// Currently Bound TextureRenderTarget
        /// </summary>
        internal static TextureRenderTargetGL CurrentlyBound;
        /// <summary>
        /// Getter for Checking whether this Target is bound
        /// </summary>
        public bool Bound => CurrentlyBound == this;
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
        public uint  TargetWidth { get; protected set; }
        /// <summary>
        /// The RenderTarget Height
        /// </summary>
        public uint  TargetHeight { get; protected set; }

        public override Vector2 Size {
            get {
                return new Vector2(this.TargetWidth, this.TargetHeight);
            }
            protected set {
                //TODO: tbh you cant even set it rn, sure you can but it wont do fuck all
            }
        }

        private OpenGLESBackend _backend;

        /// <summary>
        /// Creates a TextureRenderTarget
        /// </summary>
        /// <param name="width">Desired Width</param>
        /// <param name="height">Desired Width</param>
        /// <exception cref="Exception">Throws Exception if the Target didn't create properly</exception>
        public unsafe TextureRenderTargetGL(OpenGLESBackend backend, uint width, uint height) {
            OpenGLHelper.CheckThread();

            this.gl       = backend.GetGlApi();
            this._backend = backend;

            //Generate and bind a FrameBuffer
            this._frameBufferId = this.gl.GenFramebuffer();
            this.gl.BindFramebuffer(FramebufferTarget.Framebuffer, this._frameBufferId);
            OpenGLHelper.CheckError();

            //Generate a Texture
            this._textureId = this.gl.GenTexture();
            this.gl.BindTexture(TextureTarget.Texture2D, this._textureId);
            //Set it to Empty
            this.gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
            //Set The Filtering to nearest (apperantly necessary, idk)
            this.gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
            this.gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
            OpenGLHelper.CheckError();

            //Generate the Depth buffer
            this._depthRenderBufferId = this.gl.GenRenderbuffer();
            this.gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, this._depthRenderBufferId);
            this.gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, width, height);
            this.gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, this._depthRenderBufferId);
            //Connect the bound texture to the FrameBuffer object
            this.gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, this._textureId, 0);
            OpenGLHelper.CheckError();

            GLEnum[] drawBuffers = new GLEnum[1] {
                GLEnum.ColorAttachment0
            };
            this.gl.DrawBuffers(1, drawBuffers);
            OpenGLHelper.CheckError();

            //Check if FrameBuffer created successfully
            if (this.gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete) {
                throw new Exception("Failed to create TextureRenderTarget!");
            }

            this._oldViewPort  = new int[4];
            this.TargetWidth  = width;
            this.TargetHeight = height;
        }

        /// <summary>
        /// Binds the Target, from now on drawing will draw to this RenderTarget,
        /// </summary>
        public override void Bind() {
            OpenGLHelper.CheckThread();
            
            if (this.Locked)
                return;

            this.gl.BindFramebuffer(FramebufferTarget.Framebuffer, this._frameBufferId);
            //Store the old viewport for later
            this.gl.GetInteger(GetPName.Viewport, this._oldViewPort);
            this.gl.Viewport(0, 0, this.TargetWidth, this.TargetHeight);
            OpenGLHelper.CheckError();

            CurrentlyBound = this;
        }

        /// <summary>
        /// Indicates whether Object is Locked or not,
        /// This is done internally to not be able to switch RenderTargets while some important operation is happening
        /// or really anything that would possibly get screwed over by switching RenderTargets
        /// </summary>
        internal bool Locked = false;

        /// <summary>
        /// Binds and sets a Lock so that the Target cannot be unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal TextureRenderTargetGL LockingBind() {
            this.Bind();
            this.Lock();

            return this;
        }
        /// <summary>
        /// Locks the Target so that other Targets cannot be bound/unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal TextureRenderTargetGL Lock() {
            this.Locked = true;

            return this;
        }
        /// <summary>
        /// Unlocks the Target, so that other Targets can be bound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal TextureRenderTargetGL Unlock() {
            this.Locked = false;

            return this;
        }
        /// <summary>
        /// Uninds and unlocks the Target so that other Targets can be bound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal TextureRenderTargetGL UnlockingUnbind() {
            this.Unlock();
            this.Unbind();

            return this;
        }

        /// <summary>
        /// Unbinds the Target and resets the Viewport, drawing is now back to normal
        /// </summary>
        public override void Unbind() {
            OpenGLHelper.CheckThread();
            
            if (this.Locked)
                return;

            this.gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            this.gl.Viewport(this._oldViewPort[0], this._oldViewPort[1], (uint) this._oldViewPort[2], (uint) this._oldViewPort[3]);
            OpenGLHelper.CheckError();

            CurrentlyBound = null;
        }
        /// <summary>
        /// Retrieves the Texture from this RenderTarget
        /// </summary>
        /// <returns>Texture of this RenderTarget</returns>
        public override Texture GetTexture() => new TextureGL(this._backend, this._textureId, this.TargetWidth, this.TargetHeight);

        public void Dispose() {
            OpenGLHelper.CheckThread();
            
            if (this.Bound)
                this.UnlockingUnbind();

            try {
                this.gl.DeleteFramebuffer(this._frameBufferId);
                this.gl.DeleteTexture(this._textureId);
                this.gl.DeleteRenderbuffer(this._depthRenderBufferId);
            }
            catch {

            }
            OpenGLHelper.CheckError();
        }
    }
}
