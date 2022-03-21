using System;

using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.Graphics.Renderers;
using ImGuiNET;


namespace Furball.Vixie.TestApplication.Tests {
    public class TextureRenderTargetTest : GameComponent {
        private ILineRenderer       _lineRenderer;
        private TextureRenderTarget _renderTarget;
        private Texture             _resultTexture;
        private IQuadRenderer       _quadRenderer;
        private Texture             _whiteTexture;

        public override void Initialize() {
            this._lineRenderer = GraphicsBackend.Current.CreateLineRenderer();
            this._renderTarget = TextureRenderTarget.Create(1280, 720);
            this._quadRenderer = GraphicsBackend.Current.CreateTextureRenderer();
            this._whiteTexture = Texture.Create();

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            GraphicsBackend.Current.Clear();

            this._renderTarget.Bind();
            GraphicsBackend.Current.Clear();

            //this._lineRenderer.Begin();
            //this._lineRenderer.Draw(new Vector2(1280, 720), new Vector2(0, 0), 16f, Color.Red);
            //this._lineRenderer.End();
            this._quadRenderer.Begin();
            this._quadRenderer.Draw(this._whiteTexture, new Vector2(5, 5), new Vector2(128, 128), Color.Red);
            this._quadRenderer.End();

            this._renderTarget.Unbind();

            this._resultTexture ??= this._renderTarget.GetTexture();
//
            this._quadRenderer.Begin();
            this._quadRenderer.Draw(this._resultTexture, Vector2.Zero, Vector2.One, 0, Color.Blue);
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

        public override void Dispose() {
            this._quadRenderer.Dispose();

            base.Dispose();
        }
    }
}
