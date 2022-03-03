using System;

using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Renderers.OpenGL;
using ImGuiNET;


namespace Furball.Vixie.TestApplication.Tests {
    public class TextureRenderTargetTest : GameComponent {
        private BatchedLineRenderer _batchedLineRenderer;
        private TextureRenderTarget _renderTarget;
        private Texture             _resultTexture;
        private BatchedRenderer     _batchedRenderer;

        public override void Initialize() {
            this._batchedLineRenderer = new BatchedLineRenderer();
            this._renderTarget        = new TextureRenderTarget(1280, 720);
            this._batchedRenderer     = new BatchedRenderer();

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._renderTarget.Bind();

            this._batchedLineRenderer.Begin();
            this._batchedLineRenderer.Draw(new Vector2(1280, 720), new Vector2(0, 0), 16f, Color.Red);
            this._batchedLineRenderer.End();

            this._renderTarget.Unbind();

            this._resultTexture = this._renderTarget.GetTexture();

            this._batchedRenderer.Begin();
            this._batchedRenderer.Draw(this._resultTexture, Vector2.Zero, new Vector2(1280, 720), Vector2.Zero);
            this._batchedRenderer.End();

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
            this._batchedRenderer.Dispose();
            this._renderTarget.Dispose();
            this._resultTexture.Dispose();
            this._batchedLineRenderer.Dispose();

            base.Dispose();
        }
    }
}
