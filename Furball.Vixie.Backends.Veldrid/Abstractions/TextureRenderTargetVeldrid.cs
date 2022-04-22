using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers;
using Veldrid;
using Texture=Furball.Vixie.Backends.Shared.Texture;

namespace Furball.Vixie.Backends.Veldrid.Abstractions {
    public sealed class TextureRenderTargetVeldrid : TextureRenderTarget {
        private readonly VeldridBackend _backend;
        public override Vector2 Size {
            get;
            protected set;
        }

        private TextureVeldrid _tex;
        private Framebuffer    _fb;
        
        public TextureRenderTargetVeldrid(VeldridBackend backend, uint width, uint height) {
            this._backend = backend;

            this.Size = new(width, height);

            this._tex = new TextureVeldrid(backend, width, height);
            
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
            this._backend.Flush();
            
            this._backend.CommandList.SetFramebuffer(this._fb);
            this._backend.CommandList.SetFullViewports();
            
            this._backend.SetProjectionMatrix(this._fb.Width, this._fb.Height);
        }
        
        public override void Unbind() {
            this._backend.Flush();
            
            this._backend.CommandList.SetFramebuffer(this._backend.RenderFramebuffer);
            
            this._backend.CommandList.SetFullViewports();

            this._backend.SetProjectionMatrix(this._backend.RenderFramebuffer.Height, this._backend.RenderFramebuffer.Height);
        }

        private bool _isDisposed = false;

        public override void Dispose() {
            if (this._isDisposed) return;
            this._isDisposed = true;
            
            DisposeQueue.Enqueue(this._fb);
            DisposeQueue.Enqueue(this._tex);
        }

        ~TextureRenderTargetVeldrid() {
            this.Dispose();
        }

        public override Texture GetTexture() => this._tex;
    }
}
