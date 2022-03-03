using System;

using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Renderers.OpenGL;
using Furball.Vixie.Helpers;
using ImGuiNET;
using Texture=Furball.Vixie.Graphics.Texture;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestBatchedRendering : GameComponent {
        private QuadRenderer _quadRenderer;
        private Texture      _texture;

        public override void Initialize() {
            this._quadRenderer = new QuadRenderer();

            //Load the Texture
            this._texture = new Texture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

            base.Initialize();
        }

        /// <summary>
        /// Amount of Dons to draw on screen each frame
        /// </summary>
        private int CirnoDons = 1024;

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._quadRenderer.Begin();

            for (int i = 0; i != this.CirnoDons; i++) {
                this._quadRenderer.Draw(this._texture, new Vector2(i % 1024, 0), null, null, 0, Color.White);
            }

            this._quadRenderer.End();

            #region ImGui menu

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            ImGui.SliderInt("Draws", ref this.CirnoDons, 0, 1024);

            #endregion

            base.Draw(deltaTime);
        }

        public override void Dispose() {
            this._texture.Dispose();
            this._quadRenderer.Dispose();

            base.Dispose();
        }
    }
}
