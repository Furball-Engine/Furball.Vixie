using System.Numerics;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;

namespace Furball.Vixie.Graphics.Backends.Direct3D11.Abstractions {
    public class TextureRenderTargetD3D11 : TextureRenderTarget {
        public override Vector2 Size { get; protected set; }

        private Direct3D11Backend  _backend;
        private DeviceContext      _deviceContext;
        private Texture2D          _renderTargetTexture;
        private RenderTargetView   _renderTarget;
        private ShaderResourceView _shaderResourceView;

        private RawViewportF[] _viewports;

        public TextureRenderTargetD3D11(Direct3D11Backend backend, uint width, uint height) {
            this._backend       = backend;
            this._deviceContext = backend.GetDeviceContext();

            Texture2DDescription renderTargetTextureDescription = new Texture2DDescription {
                Width     = (int)width,
                Height    = (int)height,
                MipLevels = 1,
                ArraySize = 1,
                Format    = Format.R8G8B8A8_UNorm,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                Usage     = ResourceUsage.Default,
                SampleDescription = new SampleDescription {
                    Count = 1, Quality = 0
                },
            };

            Texture2D renderTargetTexture = new Texture2D(backend.GetDevice(), renderTargetTextureDescription);

            RenderTargetViewDescription renderTargetDescription = new RenderTargetViewDescription {
                Format = renderTargetTextureDescription.Format,
                Dimension = RenderTargetViewDimension.Texture2D,
            };

            renderTargetDescription.Texture2D.MipSlice = 0;

            RenderTargetView renderTarget = new RenderTargetView(backend.GetDevice(), renderTargetTexture, renderTargetDescription);

            ShaderResourceViewDescription shaderResourceViewDescription = new ShaderResourceViewDescription {
                Format = renderTargetDescription.Format,
                Dimension = ShaderResourceViewDimension.Texture2D,
            };

            shaderResourceViewDescription.Texture2D.MostDetailedMip = 0;
            shaderResourceViewDescription.Texture2D.MipLevels = 1;

            ShaderResourceView shaderResourceView = new ShaderResourceView(backend.GetDevice(), renderTargetTexture, shaderResourceViewDescription);

            this._renderTargetTexture = renderTargetTexture;
            this._renderTarget        = renderTarget;
            this._shaderResourceView  = shaderResourceView;
            this.Size                 = new Vector2(width, height);
        }

        public override void Bind() {
            this._deviceContext.OutputMerger.SetRenderTargets(this._renderTarget);
            this._backend.CurrentlyBoundTarget = this._renderTarget;
            this._backend.ResetBlendState();

            this._deviceContext.Rasterizer.GetViewports(this._viewports);
            this._deviceContext.Rasterizer.SetViewport(new RawViewportF {
                X        = 0,
                Y        = 0,
                Width    = this.Size.X,
                Height   = this.Size.Y,
                MinDepth = 0.0f,
                MaxDepth = 1.0f
            });
        }

        public override void Unbind() {
            this._backend.SetDefaultRenderTarget();
            this._deviceContext.Rasterizer.SetViewports(this._viewports);
        }

        public override Texture GetTexture() => new TextureD3D11(this._backend, this._renderTargetTexture, this._shaderResourceView, Size);
    }
}
