using System;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using Furball.Vixie.Gl;
using Furball.Vixie.Graphics;
using Furball.Vixie.ImGuiHelpers;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Furball.Vixie.TestApplication.Tests {
    public class TextureRenderTargetTest : GameComponent {
        private BatchedLineRenderer _batchedLineRenderer;
        private TextureRenderTarget _renderTarget;
        private Texture             _resultTexture;
        private InstancedRenderer   _instancedRenderer;

        private ImGuiController _imGuiController;

        public override void Initialize() {
            this._batchedLineRenderer = new BatchedLineRenderer();
            this._renderTarget        = new TextureRenderTarget(1280, 720);
            this._instancedRenderer   = new InstancedRenderer();

            this._imGuiController = ImGuiCreator.CreateController();

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

            this._instancedRenderer.Begin();
            this._instancedRenderer.Draw(this._resultTexture, Vector2.Zero, Vector2.Zero, Color.White);
            this._instancedRenderer.End();




            #region ImGui menu

            this._imGuiController.Update((float) deltaTime);

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            this._imGuiController.Render();

            #endregion

            base.Draw(deltaTime);
        }
    }
}
