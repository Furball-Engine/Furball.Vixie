using System;

using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Helpers;
using ImGuiNET;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestQuadRendering : GameComponent {
        private IQuadRenderer _quadRendererGl;
        private Texture          _textureGl;

        public override void Initialize() {
            this._quadRendererGl = GraphicsBackend.Current.CreateTextureRenderer();

            //Load the Texture
            this._textureGl = Texture.Create(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

            base.Initialize();
        }

        /// <summary>
        /// Amount of Dons to draw on screen each frame
        /// </summary>
        private int CirnoDons = 1024;

        public override void Draw(double deltaTime) {
            GraphicsBackend.Current.Clear();

            this._quadRendererGl.Begin();

            for (int i = 0; i != this.CirnoDons; i++) {
                this._quadRendererGl.Draw(this._textureGl, new Vector2(i % 1024, 0));
            }

            this._quadRendererGl.End();

            #region ImGui menu

            // ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
            //            $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            // );
            //
            // if (ImGui.Button("Go back to test selector")) {
            //     this.BaseGame.Components.Add(new BaseTestSelector());
            //     this.BaseGame.Components.Remove(this);
            // }
            //
            // ImGui.SliderInt("Draws", ref this.CirnoDons, 0, 1024);

            #endregion

            base.Draw(deltaTime);
        }

        public override void Dispose() {
            this._quadRendererGl.Dispose();

            base.Dispose();
        }
    }
}
