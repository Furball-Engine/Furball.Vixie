using System;
using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Renderers.OpenGL;
using Furball.Vixie.Helpers;
using ImGuiNET;


namespace Furball.Vixie.TestApplication.Tests {
    public class TestRotation : GameComponent {
        private QuadRenderer _quadRenderer;
        private Texture      _whiteTexture;

        public override void Initialize() {
            this._quadRenderer = new QuadRenderer();
            this._whiteTexture = new Texture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

            base.Initialize();
        }

        private float _rotation = 1f;

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._quadRenderer.Begin();
            this._quadRenderer.Draw(this._whiteTexture, new Vector2(1280 /2, 720 /2), new Vector2(this._whiteTexture.Width, this._whiteTexture.Height), Vector2.Zero, _rotation);
            this._quadRenderer.End();

            #region ImGui menu

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );

            ImGui.DragFloat("Rotation", ref this._rotation, 0.01f, 0f, 8f);

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            #endregion

            base.Draw(deltaTime);
        }

        public override void Dispose() {
            this._quadRenderer.Dispose();
            this._quadRenderer.Dispose();
            this._whiteTexture.Dispose();

            base.Dispose();
        }
    }
}
