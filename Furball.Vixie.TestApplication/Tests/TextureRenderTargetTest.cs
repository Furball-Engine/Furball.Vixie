using System;

using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Renderers.OpenGL;
using ImGuiNET;


namespace Furball.Vixie.TestApplication.Tests {
    public class TextureRenderTargetTest : GameComponent {
        private LineRenderer        _lineRenderer;
        private TextureRenderTarget _renderTarget;
        private Texture             _resultTexture;
        private QuadRenderer        quadRenderer;

        public override void Initialize() {
            this._lineRenderer    = new LineRenderer();
            this._renderTarget    = new TextureRenderTarget(1280, 720);
            this.quadRenderer = new QuadRenderer();

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._renderTarget.Bind();

            this._lineRenderer.Begin();
            this._lineRenderer.Draw(new Vector2(1280, 720), new Vector2(0, 0), 16f, Color.Red);
            this._lineRenderer.End();

            this._renderTarget.Unbind();

            this._resultTexture = this._renderTarget.GetTexture();

            this.quadRenderer.Begin();
            this.quadRenderer.Draw(this._resultTexture, Vector2.Zero, new Vector2(1280, 720));
            this.quadRenderer.End();

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
            this.quadRenderer.Dispose();
            this._renderTarget.Dispose();
            this._resultTexture.Dispose();

            base.Dispose();
        }
    }
}
