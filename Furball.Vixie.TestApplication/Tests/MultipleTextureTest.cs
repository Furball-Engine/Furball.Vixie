using System;
using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Helpers;
using ImGuiNET;

namespace Furball.Vixie.TestApplication.Tests {
    public class MultipleTextureTest : GameComponent {
        private Texture[]     _textures = new Texture[32];
        private IQuadRenderer _quadRenderer;

        public override void Initialize() {
            for (int i = 0; i != this._textures.Length; i++) {
                if (i % 2 == 0 && i != 0)
                    this._textures[i]  = Texture.Create(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));
                else this._textures[i] = Texture.Create();
            }

            this._quadRenderer = GraphicsBackend.Current.CreateTextureRenderer();

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            GraphicsBackend.Current.Clear();

            this._quadRenderer.Begin();

            int x = 0;
            int y = 0;

            for (int i = 0; i != this._textures.Length; i++) {
                this._quadRenderer.Draw(this._textures[i], new Vector2(x, y), new Vector2(0.5f, 0.5f), 50);
                if (i % 3 == 0 && i != 0) {
                    y += 64;
                    x =  0;
                }

                x += 256;
            }

            this._quadRenderer.End();

            #region ImGui menu

            //ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
            //           $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            //);
            //
            //if (ImGui.Button("Go back to test selector")) {
            //    this.BaseGame.Components.Add(new BaseTestSelector());
            //    this.BaseGame.Components.Remove(this);
            //}

            #endregion

            base.Draw(deltaTime);
        }
    }
}
