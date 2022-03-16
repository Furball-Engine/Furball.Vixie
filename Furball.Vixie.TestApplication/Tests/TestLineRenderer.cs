using System;

using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.Graphics.Renderers;
using ImGuiNET;


namespace Furball.Vixie.TestApplication.Tests {
    public class TestLineRenderer : GameComponent {
        private ILineRenderer _lineRendererGl;

        public override void Initialize() {
            this._lineRendererGl = GraphicsBackend.Current.CreateLineRenderer();

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            GraphicsBackend.Current.Clear();

            this._lineRendererGl.Begin();

            for (int i = 0; i != 1280; i++) {
                this._lineRendererGl.Draw(new Vector2(i, 0), new Vector2(1280 - i, 720), 4, Color.White);
            }

            this._lineRendererGl.End();

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
            this._lineRendererGl.Dispose();

            base.Dispose();
        }
    }
}

