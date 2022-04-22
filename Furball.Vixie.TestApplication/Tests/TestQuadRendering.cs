using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestQuadRendering : GameComponent {
        private IQuadRenderer _quadRendererGl;
        private Texture       _texture;

        public override void Initialize() {
            this._quadRendererGl = GraphicsBackend.Current.CreateTextureRenderer();

            //Load the Texture
            this._texture = Resources.CreateTexture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

            base.Initialize();
        }

        /// <summary>
        /// Amount of Dons to draw on screen each frame
        /// </summary>
        private int _cirnoDons = 1024;

        public override void Draw(double deltaTime) {
            GraphicsBackend.Current.Clear();

            this._quadRendererGl.Begin();

            for (int i = 0; i != this._cirnoDons; i++) {
                this._quadRendererGl.Draw(this._texture, new Vector2(i % 1024, i % 2 == 0 ? 0 : 200), new Vector2(0.5f), 0, new Color(1f, 1f, 1f, 0.5f));
            }

            this._quadRendererGl.End();

            #region ImGui menu

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            ImGui.SliderInt("Draws", ref this._cirnoDons, 0, 2048);

            #endregion

            base.Draw(deltaTime);
        }

        public override void Dispose() {
            this._quadRendererGl.Dispose();
            this._texture.Dispose();

            base.Dispose();
        }
    }
}
