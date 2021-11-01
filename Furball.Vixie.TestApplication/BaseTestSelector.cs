using Furball.Vixie.Graphics;
using Furball.Vixie.ImGuiHelpers;
using Furball.Vixie.TestApplication.Tests;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Furball.Vixie.TestApplication {
    public class BaseTestSelector : GameComponent {
        private ImGuiController _imGuiController;
        private InstancedRenderer        _instancedRenderer;

        public BaseTestSelector(Game game) : base(game) {}

        public override void Initialize() {
            this._imGuiController   = ImGuiCreator.CreateController();
            this._instancedRenderer = new InstancedRenderer();

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            this._instancedRenderer.Clear();

            this._imGuiController.Update((float) deltaTime);

            if (ImGui.Button("Texture Drawing Test")) {
                this.BaseGame.Components.Add(new TestTextureDrawing(this.BaseGame));
                this.BaseGame.Components.Remove(this);
            }

            if (ImGui.Button("Batched Rendering Test")) {
                this.BaseGame.Components.Add(new TestBatchedRendering(this.BaseGame));
                this.BaseGame.Components.Remove(this);
            }

            this._imGuiController.Render();

            base.Draw(deltaTime);
        }
    }
}
