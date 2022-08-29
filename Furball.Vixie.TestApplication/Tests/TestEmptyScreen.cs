#if USE_IMGUI
using ImGuiNET;
#endif

namespace Furball.Vixie.TestApplication.Tests; 

public class TestEmptyScreen : GameComponent {
    public override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();

        #region ImGui menu
        #if USE_IMGUI
        if (ImGui.Button("Go back to test selector")) {
            this.BaseGame.Components.Add(new BaseTestSelector());
            this.BaseGame.Components.Remove(this);
        }
        #endif
        #endregion

        base.Draw(deltaTime);
    }
}