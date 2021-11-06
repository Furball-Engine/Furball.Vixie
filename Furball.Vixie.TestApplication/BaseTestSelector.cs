using Furball.Vixie.Graphics.Renderers.OpenGL;
using Furball.Vixie.ImGuiHelpers;
using Furball.Vixie.TestApplication.Tests;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Furball.Vixie.TestApplication {
    public class BaseTestSelector : GameComponent {
        private ImGuiController _imGuiController;

        public BaseTestSelector()  {}

        public override void Initialize() {
            this._imGuiController   = ImGuiCreator.CreateController();

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._imGuiController.Update((float) deltaTime);

            ImGui.Begin("Test Selector");

            if (ImGui.Button("Texture Drawing Test")) {
                this.BaseGame.Components.Add(new TestTextureDrawing());
                this.BaseGame.Components.Remove(this);
            }

            if (ImGui.Button("Batched Rendering Test")) {
                this.BaseGame.Components.Add(new TestBatchedRendering());
                this.BaseGame.Components.Remove(this);
            }

            if (ImGui.Button("Immediate Rendering Test")) {
                this.BaseGame.Components.Add(new TestImmediateRendering());
                this.BaseGame.Components.Remove(this);
            }

            if (ImGui.Button("Line Rendering Test")) {
                this.BaseGame.Components.Add(new TestLineRenderer());
                this.BaseGame.Components.Remove(this);
            }

            if (ImGui.Button("Batched Line Rendering Test")) {
                this.BaseGame.Components.Add(new TestBatchedLineRendering());
                this.BaseGame.Components.Remove(this);
            }

            if (ImGui.Button("TextureRenderTarget Test")) {
                this.BaseGame.Components.Add(new TextureRenderTargetTest());
                this.BaseGame.Components.Remove(this);
            }

            if (ImGui.Button("Rotation Test")) {
                this.BaseGame.Components.Add(new TestRotation());
                this.BaseGame.Components.Remove(this);
            }

            if (ImGui.Button("Source Rectangle Rendering Test")) {
                this.BaseGame.Components.Add(new TestSourceRect());
                this.BaseGame.Components.Remove(this);
            }

            if (ImGui.Button("Test FontStashSharp")) {
                this.BaseGame.Components.Add(new TestFSS());
                this.BaseGame.Components.Remove(this);
            }


            ImGui.End();

            this._imGuiController.Render();

            base.Draw(deltaTime);
        }
    }
}
