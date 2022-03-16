using System;

using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.Graphics.Renderers;
using ImGuiNET;


namespace Furball.Vixie.TestApplication.Tests {
    public class TextureRenderTargetTest : GameComponent {
        private ILineRenderer        _lineRendererGl;
        private TextureRenderTarget _renderTargetGl;
        private Texture             _resultTextureGl;
        private IQuadRenderer        _quadRendererGl;

        public override void Initialize() {
            this._lineRendererGl = GraphicsBackend.Current.CreateLineRenderer();
            this._renderTargetGl = TextureRenderTarget.Create(1280, 720);
            this._quadRendererGl = GraphicsBackend.Current.CreateTextureRenderer();

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            GraphicsBackend.Current.Clear();

            this._renderTargetGl.Bind();

            this._lineRendererGl.Begin();
            this._lineRendererGl.Draw(new Vector2(1280, 720), new Vector2(0, 0), 16f, Color.Red);
            this._lineRendererGl.End();

            this._renderTargetGl.Unbind();

            this._resultTextureGl = this._renderTargetGl.GetTexture();

            this._quadRendererGl.Begin();
            this._quadRendererGl.Draw(this._resultTextureGl, Vector2.Zero, new Vector2(1280, 720));
            this._quadRendererGl.End();

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
            this._quadRendererGl.Dispose();

            base.Dispose();
        }
    }
}
