using System;

using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Renderers.OpenGL;
using ImGuiNET;


namespace Furball.Vixie.TestApplication.Tests {
    public class TestBatchedLineRendering : GameComponent {
        private LineRenderer _lineRenderer;

        public override void Initialize() {
            this._lineRenderer = new LineRenderer();

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._lineRenderer.Begin();

            for (int i = 0; i != 1280; i++) {
                this._lineRenderer.Draw(new Vector2(i, 0), new Vector2(1280 - i, 720), 4, Color.White);
            }

            this._lineRenderer.End();

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

        public override void Dispose() {
            this._lineRenderer.Dispose();

            base.Dispose();
        }
    }
}
