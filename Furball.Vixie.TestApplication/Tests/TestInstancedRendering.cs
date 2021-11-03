using System;
using System.Globalization;
using System.Numerics;
using Furball.Vixie.Gl;
using Furball.Vixie.Graphics;
using Furball.Vixie.Helpers;
using Furball.Vixie.ImGuiHelpers;
using Furball.Vixie.Shaders;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;
using Texture=Furball.Vixie.Gl.Texture;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestInstancedRendering : GameComponent {
        private InstancedRenderer _instancedRenderer;
        private Texture           _texture;

        private ImGuiController _imGuiController;

        public override void Initialize() {
            this._instancedRenderer = new InstancedRenderer();

            //Load the Texture
            this._texture = new Texture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

            this._imGuiController = ImGuiCreator.CreateController();

            base.Initialize();
        }

        private int CirnoDons = 1024;

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._instancedRenderer.Begin();

            for (int i = 0; i != this.CirnoDons; i++) {
                this._instancedRenderer.Draw(this._texture, new Vector2(i, 0), new Vector2(371, 326));
            }

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

            ImGui.SliderInt("Draws", ref this.CirnoDons, 0, 1024);


            this._imGuiController.Render();

            #endregion

            base.Draw(deltaTime);
        }
    }
}
