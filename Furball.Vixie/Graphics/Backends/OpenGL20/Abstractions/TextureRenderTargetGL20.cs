using System;
using System.Numerics;
using Furball.Vixie.Graphics.Backends.OpenGL41;
using Furball.Vixie.Graphics.Backends.OpenGL41.Abstractions;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy.Extensions.EXT;

namespace Furball.Vixie.Graphics.Backends.OpenGL20.Abstractions {
    public class TextureRenderTargetGL20 : TextureRenderTarget, IDisposable {
        /// <summary>
        /// Currently Bound TextureRenderTarget
        /// </summary>
        internal static TextureRenderTargetGL20 CurrentlyBound;
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
                      get => new Vector2(this.TargetWidth, this.TargetHeight);
            protected set => throw new Exception("Setting the size of TextureRenderTargets is currently unsupported.");
        }

        private OpenGL20Backend _backend;

        /// <summary>
        /// Creates a TextureRenderTarget
        /// </summary>
        /// <param name="width">Desired Width</param>
        /// <param name="height">Desired Width</param>
        /// <exception cref="Exception">Throws Exception if the Target didn't create properly</exception>
        public unsafe TextureRenderTargetGL20(OpenGL20Backend backend, uint width, uint height) {
            this._backend = backend;
            // this._backend.CheckThread();

            this.gl  = backend.GetOpenGL();
            this.ext = backend.GetOpenGLFramebufferEXT();

            //Generate and bind a FrameBuffer
            this._frameBufferId = this.ext.GenFramebuffer();
            this.ext.BindFramebuffer(FramebufferTarget.Framebuffer, this._frameBufferId);
            this._backend.CheckError();

            //Generate a Texture
            this._textureId = this.gl.GenTexture();
            this.gl.BindTexture(TextureTarget.Texture2D, this._textureId);
            //Set it to Empty
            this.gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
            //Set The Filtering to nearest (apperantly necessary, idk)
            this.gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
            this.gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
            this._backend.CheckError();

            //Generate the Depth buffer
            this._depthRenderBufferId = this.ext.GenRenderbuffer();
            this._backend.CheckError();
            this.ext.BindRenderbuffer(RenderbufferTarget.Renderbuffer, this._depthRenderBufferId);
            this._backend.CheckError();
            this.ext.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, width, height);
            this._backend.CheckError();
            this.ext.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, this._depthRenderBufferId);
            this._backend.CheckError();
            //Connect the bound texture to the FrameBuffer object
            this.ext.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, this._textureId, 0);
            this._backend.CheckError();

            GLEnum[] drawBuffers = new GLEnum[1] {
                GLEnum.ColorAttachment0
            };
            this.gl.DrawBuffers(1, drawBuffers);
            this._backend.CheckError();

            //Check if FrameBuffer created successfully
            if (this.ext.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != EXT.FramebufferCompleteExt) {
                throw new Exception("Failed to create TextureRenderTarget!");
            }

            this.ext.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            this._oldViewPort  = new int[4];
            this.TargetWidth  = width;
            this.TargetHeight = height;
        }

        ~TextureRenderTargetGL20() {
            DisposeQueue.Enqueue(this);
        }

        /// <summary>
        /// Binds the Target, from now on drawing will draw to this RenderTarget,
        /// </summary>
        public override void Bind() {
            // this._backend.CheckThread();
            
            if (this.Locked)
                return;

            this.ext.BindFramebuffer(FramebufferTarget.Framebuffer, this._frameBufferId);
            //Store the old viewport for later
            this.gl.GetInteger(GetPName.Viewport, this._oldViewPort);
            this.gl.Viewport(0, 0, this.TargetWidth, this.TargetHeight);
            this._backend.CheckError();

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
        internal TextureRenderTargetGL20 LockingBind() {
            this.Bind();
            this.Lock();

            return this;
        }
        /// <summary>
        /// Locks the Target so that other Targets cannot be bound/unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal TextureRenderTargetGL20 Lock() {
            this.Locked = true;

            return this;
        }
        /// <summary>
        /// Unlocks the Target, so that other Targets can be bound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal TextureRenderTargetGL20 Unlock() {
            this.Locked = false;

            return this;
        }
        /// <summary>
        /// Uninds and unlocks the Target so that other Targets can be bound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal TextureRenderTargetGL20 UnlockingUnbind() {
            this.Unlock();
            this.Unbind();

            return this;
        }

        /// <summary>
        /// Unbinds the Target and resets the Viewport, drawing is now back to normal
        /// </summary>
        public override void Unbind() {
            // this._backend.CheckThread();
            
            if (this.Locked)
                return;

            this.ext.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            this.gl.Viewport(this._oldViewPort[0], this._oldViewPort[1], (uint) this._oldViewPort[2], (uint) this._oldViewPort[3]);
            this._backend.CheckError();

            CurrentlyBound = null;
        }
        /// <summary>
        /// Retrieves the Texture from this RenderTarget
        /// </summary>
        /// <returns>Texture of this RenderTarget</returns>
        public override Texture GetTexture() => new TextureGL20(this._backend, this._textureId, this.TargetWidth, this.TargetHeight);

        private          bool                 _isDisposed = false;
        private readonly ExtFramebufferObject ext;

        public unsafe void Dispose() {
            // this._backend.CheckThread();
            
            if (this.Bound)
                this.UnlockingUnbind();

            if (this._isDisposed)
                return;

            this._isDisposed = true;

            try {
                this.ext.DeleteFramebuffer(this._frameBufferId);
                fixed(uint* ptr = &this._textureId)
                    this.gl.DeleteTextures(1, ptr);
                this.ext.DeleteRenderbuffer(this._depthRenderBufferId);
            }
            catch {

            }
            this._backend.CheckError();
        }
    }
}
