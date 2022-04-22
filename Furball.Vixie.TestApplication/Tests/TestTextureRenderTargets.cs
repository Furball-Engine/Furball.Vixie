using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using ImGuiNET;


namespace Furball.Vixie.TestApplication.Tests {
    public class TestTextureRenderTargets : GameComponent {
        private ILineRenderer       _lineRenderer;
        private TextureRenderTarget _renderTarget;
        private Texture             _resultTexture;
        private IQuadRenderer       _quadRenderer;
        private Texture             _whiteTexture;
        private float               _scale = 1f;

        public override void Initialize() {
            this._renderTarget = Resources.CreateTextureRenderTarget(200, 200);
            
            this._lineRenderer = GraphicsBackend.Current.CreateLineRenderer();
            this._quadRenderer = GraphicsBackend.Current.CreateTextureRenderer();
            
            this._whiteTexture = Resources.CreateTexture();

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            GraphicsBackend.Current.Clear();

            this._renderTarget.Bind();
            GraphicsBackend.Current.Clear();

            this._lineRenderer.Begin();
            this._lineRenderer.Draw(new Vector2(0, 0), new Vector2(1280, 720), 16f, Color.Red);
            this._lineRenderer.End();
            this._quadRenderer.Begin();
            this._quadRenderer.Draw(this._whiteTexture, new Vector2(5, 5), new Vector2(128, 128), Color.Green);
            this._quadRenderer.End();

            this._renderTarget.Unbind();

            this._resultTexture ??= this._renderTarget.GetTexture();
            
            this._quadRenderer.Begin();
            this._quadRenderer.Draw(this._resultTexture, Vector2.Zero, new(this._scale), 0, Color.White);
            this._quadRenderer.End();

            #region ImGui menu

            ImGui.SliderFloat("Final Texture Scale", ref this._scale, 0f, 2f);

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            #endregion

            base.Draw(deltaTime);
        }

        public override void Dispose() {
            this._quadRenderer.Dispose();
            this._lineRenderer.Dispose();
            this._renderTarget.Dispose();
            this._resultTexture.Dispose();
            this._whiteTexture.Dispose();

            base.Dispose();
        }
    }
}
