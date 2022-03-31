using System;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Helpers;
using ImGuiNET;
using Color=Furball.Vixie.Graphics.Color;

namespace Furball.Vixie.TestApplication.Tests {
    public class MixedTest : GameComponent {
        private IQuadRenderer _quadRenderer;
        private ILineRenderer _lineRenderer;
        private Texture       _testTexture;

        public override void Initialize() {
            this._quadRenderer = GraphicsBackend.Current.CreateTextureRenderer();
            this._testTexture = Texture.Create(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

            this._lineRenderer = GraphicsBackend.Current.CreateLineRenderer();
            
            base.Initialize();
        }

        private float _rotation = 1f;

        public override void Draw(double deltaTime) {
            GraphicsBackend.Current.Clear();

            this._quadRenderer.Begin();
            this._quadRenderer.Draw(this._testTexture, new Vector2(1280 / 2, 720 / 2), new(0.5f), 0, Color.Red);
            this._quadRenderer.End();

            this._lineRenderer.Begin();
            this._lineRenderer.Draw(Vector2.Zero, new(1280, 720), 3, Color.Blue);
            this._lineRenderer.End();
            
            this._quadRenderer.Begin();
            this._quadRenderer.Draw(this._testTexture, new Vector2(1280 / 2 + 100, 720 / 2 + 100), new(0.5f), 0, Color.Green);
            this._quadRenderer.End();
            
            #region ImGui menu

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            #endregion

            base.Draw(deltaTime);
        }
    }
}
