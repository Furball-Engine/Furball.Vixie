using System;
using System.Globalization;
using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestMultipleTextures : GameComponent {
        private Texture[]     _textures = new Texture[32];
        private IQuadRenderer _quadRenderer;

        private float _scale = 0.5f;
        
        public override void Initialize() {
            for (int i = 0; i != this._textures.Length; i++) {
                if (i % 2 == 0 && i != 0)
                    this._textures[i]  = Resources.CreateTexture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));
                else this._textures[i] = Resources.CreateTexture(ResourceHelpers.GetByteResource("Resources/test.qoi"), true);
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
                this._quadRenderer.Draw(this._textures[i], new Vector2(x, y), new Vector2(this._scale) * (i % 2 == 0 ? 1f : 0.25f), i % 2 == 0 ? Color.White : new(1f, 1f, 1f, 0.7f), 0, i % 5 == 0 ? TextureFlip.FlipVertical : TextureFlip.FlipHorizontal);
                if (i % 3 == 0 && i != 0) {
                    y += 64;
                    x =  0;
                }

                x += 256;
            }

            this._quadRenderer.End();

            #region ImGui menu

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );

            ImGui.SliderFloat("Texture Scale", ref this._scale, 0f, 20f);

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            #endregion

            base.Draw(deltaTime);
        }

        public override void Dispose() {
            for(int i = 0; i != this._textures.Length; i++)
                this._textures[i].Dispose();

            this._quadRenderer.Dispose();

            base.Dispose();
        }
    }
}
