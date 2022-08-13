using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers;
using Silk.NET.Maths;
using Veldrid;

namespace Furball.Vixie.Backends.Veldrid.Abstractions; 

internal sealed class VixieTextureRenderTargetVeldrid : VixieTextureRenderTarget {
    private readonly VeldridBackend _backend;
    public override Vector2D<int> Size {
        get;
        protected set;
    }

    private VixieTextureVeldrid _tex;
    private Framebuffer    _fb;
        
    public VixieTextureRenderTargetVeldrid(VeldridBackend backend, uint width, uint height) {
        this._backend = backend;
        this._backend.CheckThread();

        this.Size = new((int)width, (int)height);

        this._tex = new VixieTextureVeldrid(backend, width, height, default);
            
        FramebufferDescription description = new() {
            ColorTargets = new[] {
                new FramebufferAttachmentDescription(this._tex.Texture, 0)
            }
        };

        this._fb = backend.ResourceFactory.CreateFramebuffer(description);

        if (!this._backend.GraphicsDevice.IsUvOriginTopLeft)
            this._tex.IsFbAndShouldFlip = true;
    }
        
    public override void Bind() {
        this._backend.CheckThread();
        this._backend.Flush();
            
        this._backend.CommandList.SetFramebuffer(this._fb);
        this._backend.CommandList.SetFullViewports();
            
        this._backend.SetProjectionMatrix(this._fb.Width, this._fb.Height);
    }
        
    public override void Unbind() {
        this._backend.CheckThread();
        this._backend.Flush();
            
        this._backend.CommandList.SetFramebuffer(this._backend.RenderFramebuffer);
            
        this._backend.CommandList.SetFullViewports();

        this._backend.SetProjectionMatrix(this._backend.RenderFramebuffer.Height, this._backend.RenderFramebuffer.Height);
    }

    private bool _isDisposed = false;

    public override void Dispose() {
        this._backend.CheckThread();
        if (this._isDisposed) return;
        this._isDisposed = true;
            
        DisposeQueue.Enqueue(this._fb);
        DisposeQueue.Enqueue(this._tex);
    }

    ~VixieTextureRenderTargetVeldrid() {
        this.Dispose();
    }

    public override VixieTexture GetTexture() => this._tex;
}