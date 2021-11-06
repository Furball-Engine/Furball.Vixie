using System;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics.Renderers.OpenGL;
using Furball.Vixie.Helpers;
using Furball.Vixie.ImGuiHelpers;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;
using Texture=Furball.Vixie.Graphics.Texture;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestImmediateRendering : GameComponent {
        private ImmediateRenderer _immediateRenderer;
        private Texture           _texture;

        private ImGuiController _imGuiController;

        public override void Initialize() {
            this._immediateRenderer = new ImmediateRenderer();

            //Load the Texture
            this._texture = new Texture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

            this._imGuiController = ImGuiCreator.CreateController();

            base.Initialize();
        }

        private int CirnoDons = 1024;

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._immediateRenderer.Begin();

            for (int i = 0; i != this.CirnoDons; i++) {
                this._immediateRenderer.Draw(this._texture, new Vector2(i, 0), new Vector2(371, 326), null, 0, Color.White);
            }

            this._immediateRenderer.End();

            #region ImGui menu

            this._imGuiController.Update((float) deltaTime);

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            ImGui.SliderInt("Draws", ref this.CirnoDons, 0, 1024);


            this._imGuiController.Render();

            #endregion

            base.Draw(deltaTime);
        }

        public override void Dispose() {
            this._texture.Dispose();
            this._immediateRenderer.Dispose();
            this._imGuiController.Dispose();

            base.Dispose();
        }
    }
}
