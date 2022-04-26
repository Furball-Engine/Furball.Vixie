using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;
using Color=Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestMixedRendering : GameComponent {
        private IQuadRenderer _quadRenderer;
        private ILineRenderer _lineRenderer;
        private Texture       _testTexture;

        public override void Initialize() {
            this._quadRenderer = GraphicsBackend.Current.CreateTextureRenderer();
            this._testTexture = Resources.CreateTexture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

            this._lineRenderer = GraphicsBackend.Current.CreateLineRenderer();
            
            base.Initialize();
        }
        
        private float _lineWidth = 3f;

        public override void Draw(double deltaTime) {
            GraphicsBackend.Current.Clear();

            this._quadRenderer.Begin();
            this._quadRenderer.Draw(this._testTexture, new Vector2(1280 / 2, 720 / 2), new(0.5f), 0, Color.Red);
            this._quadRenderer.End();

            this._lineRenderer.Begin();
            this._lineRenderer.Draw(Vector2.Zero, new(1280, 720), _lineWidth, Color.Blue);
            this._lineRenderer.End();
            
            this._quadRenderer.Begin();
            this._quadRenderer.Draw(this._testTexture, new Vector2(1280 / 2 + 100, 720 / 2 + 100), new(0.5f), 0, Color.Green);
            this._quadRenderer.End();
            
            // #region ImGui menu
            //
            // ImGui.SliderFloat("Line Width", ref this._lineWidth, 0f, 20f);
            //
            // if (ImGui.Button("Go back to test selector")) {
            //     this.BaseGame.Components.Add(new BaseTestSelector());
            //     this.BaseGame.Components.Remove(this);
            // }
            //
            // #endregion

            base.Draw(deltaTime);
        }

        public override void Dispose() {
            this._testTexture.Dispose();
            this._lineRenderer.Dispose();
            this._quadRenderer.Dispose();

            base.Dispose();
        }
    }
}
