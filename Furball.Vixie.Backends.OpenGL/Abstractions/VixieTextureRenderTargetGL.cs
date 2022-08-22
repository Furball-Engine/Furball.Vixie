using System;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Backends.OpenGL.Abstractions; 

internal sealed class VixieTextureRenderTargetGL : VixieTextureRenderTarget, IDisposable {
    /// <summary>
    /// Currently Bound TextureRenderTarget
    /// </summary>
    internal static VixieTextureRenderTargetGL CurrentlyBound;
    /// <summary>
    /// Getter for Checking whether this Target is bound
    /// </summary>
    public bool Bound => CurrentlyBound == this;

    private GL _gl;
    
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
    public int TargetWidth { get; private set; }
    /// <summary>
    /// The RenderTarget Height
    /// </summary>
    public int TargetHeight { get; private set; }

    private VixieTextureGL _vixieTexture;

    public override Vector2D<int> Size {
        get => new Vector2D<int>(this.TargetWidth, this.TargetHeight);
        protected set => throw new Exception("Setting the size of TextureRenderTargets is currently unsupported.");
    }

    private IGLBasedBackend _backend;

    /// <summary>
    /// Creates a TextureRenderTarget
    /// </summary>
    /// <param name="width">Desired Width</param>
    /// <param name="height">Desired Width</param>
    /// <exception cref="Exception">Throws Exception if the Target didn't create properly</exception>
    public unsafe VixieTextureRenderTargetGL(IGLBasedBackend backend, uint width, uint height) {
        this._backend = backend;
        this._backend.GlCheckThread();

        this._gl = backend.GetModernGL();
        
        //Generate and bind a FrameBuffer
        this._frameBufferId = this._backend.GenFramebuffer();
        this._backend.CheckError("gen framebuffer");
        this._backend.BindFramebuffer(FramebufferTarget.Framebuffer, this._frameBufferId);
        this._backend.CheckError("bind framebuffer");

        //Generate a Texture
        this._textureId = this._backend.GenTexture();
        this._backend.CheckError("gen fb tex");
        this._backend.BindTexture(TextureTarget.Texture2D, this._textureId);
        this._backend.CheckError("bind fb tex");
        //Set it to Empty
        this._backend.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
        this._backend.CheckError("fill fb tex");
        //Set The Filtering to nearest (apperantly necessary, idk)
        this._backend.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
        this._backend.CheckError("set nearest filtering for fb tex");
        this._backend.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
        this._backend.CheckError("set nearest filtering for fb tex 2");

        //Generate the Depth buffer
        this._depthRenderBufferId = this._backend.GenRenderbuffer();
        this._backend.CheckError("gen renderbuffer");
        this._backend.BindRenderbuffer(RenderbufferTarget.Renderbuffer, this._depthRenderBufferId);
        this._backend.CheckError("bind renderbuffer");
        this._backend.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, width, height);
        this._backend.CheckError("set renderbuffer storage");
        this._backend.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, this._depthRenderBufferId);
        this._backend.CheckError("set fb to renderbuffer");
        //Connect the bound texture to the FrameBuffer object
        this._backend.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, this._textureId, 0);
        this._backend.CheckError("framebuffer texture");

        GLEnum[] drawBuffers = new GLEnum[1] {
            GLEnum.ColorAttachment0
        };
        this._backend.DrawBuffers(1, drawBuffers);
        this._backend.CheckError("drawbuffers");

        //Check if FrameBuffer created successfully
        if (this._backend.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete) {
            throw new Exception("Failed to create TextureRenderTarget!");
        }

        this._backend.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        this._oldViewPort = new int[4];
        this.TargetWidth  = (int)width;
        this.TargetHeight = (int)height;

        this._vixieTexture = new VixieTextureGL(backend, this._textureId, width, height);
    }

    ~VixieTextureRenderTargetGL() {
        DisposeQueue.Enqueue(this);
    }

    /// <summary>
    /// Binds the Target, from now on drawing will draw to this RenderTarget,
    /// </summary>
    public override void Bind() {
        this._backend.GlCheckThread();
        if (this.Locked)
            return;

        this._backend.BindFramebuffer(FramebufferTarget.Framebuffer, this._frameBufferId);
        //Store the old viewport for later
        this._backend.GetInteger(GetPName.Viewport, ref this._oldViewPort);

        //Set the projection matrix and viewport
        this._backend.SetProjectionMatrixAndViewport(this.TargetWidth, this.TargetHeight, true);

        this._gl.Disable(EnableCap.CullFace);

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
    internal VixieTextureRenderTargetGL LockingBind() {
        this._backend.GlCheckThread();
        this.Bind();
        this.Lock();

        return this;
    }
    /// <summary>
    /// Locks the Target so that other Targets cannot be bound/unbound/rebound
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    internal VixieTextureRenderTargetGL Lock() {
        this._backend.GlCheckThread();
        this.Locked = true;

        return this;
    }
    /// <summary>
    /// Unlocks the Target, so that other Targets can be bound
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    internal VixieTextureRenderTargetGL Unlock() {
        this._backend.GlCheckThread();
        this.Locked = false;

        return this;
    }
    /// <summary>
    /// Uninds and unlocks the Target so that other Targets can be bound/rebound
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    internal VixieTextureRenderTargetGL UnlockingUnbind() {
        this._backend.GlCheckThread();
        this.Unlock();
        this.Unbind();

        return this;
    }

    /// <summary>
    /// Unbinds the Target and resets the Viewport, drawing is now back to normal
    /// </summary>
    public override void Unbind() {
        this._backend.GlCheckThread();
        if (this.Locked)
            return;

        this._backend.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        this._backend.CheckError("unbind rendertarget");
        
        this._backend.SetProjectionMatrixAndViewport(this._oldViewPort[2], this._oldViewPort[3], false);

        this._gl.Enable(EnableCap.CullFace);

        CurrentlyBound = null;
    }
    
    /// <summary>
    /// Retrieves the Texture from this RenderTarget
    /// </summary>
    /// <returns>Texture of this RenderTarget</returns>
    public override VixieTexture GetTexture() => this._vixieTexture;

    private bool _isDisposed = false;
    public override void Dispose() {
        this._backend.GlCheckThread();
        if (this.Bound)
            this.UnlockingUnbind();

        if (this._isDisposed)
            return;

        this._isDisposed = true;

        try {
            this._backend.DeleteFramebuffer(this._frameBufferId);
            this._backend.DeleteTexture(this._textureId);
            this._backend.DeleteRenderbuffer(this._depthRenderBufferId);
        }
        catch {

        }
        this._backend.CheckError("dispose render target");
    }
}