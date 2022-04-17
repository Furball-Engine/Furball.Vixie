using System;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;
using Color=Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestSourceRect : GameComponent {
        private IQuadRenderer _quadRenderer;
        private Texture       _whiteTexture;

        public override void Initialize() {
            this._quadRenderer = GraphicsBackend.Current.CreateTextureRenderer();
            this._whiteTexture = Resources.CreateTexture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

            base.Initialize();
        }

        private float _rotation = 1f;

        public override void Draw(double deltaTime) {
            GraphicsBackend.Current.Clear();

            this._quadRenderer.Begin();
            this._quadRenderer.Draw(this._whiteTexture, new Vector2(1280 / 2, 720 / 2), Vector2.One, this._rotation, Color.White, new Rectangle(371 / 2, 0, 371 / 2, 326 / 2));
            this._quadRenderer.End();

            #region ImGui menu

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );
            
            ImGui.SliderFloat("Rotation", ref this._rotation, 0f, (float)(Math.PI * 2f));
            
            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            #endregion

            base.Draw(deltaTime);
        }
    }
}
