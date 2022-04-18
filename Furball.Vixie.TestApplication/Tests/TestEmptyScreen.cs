using System;
using System.Globalization;
using ImGuiNET;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestEmptyScreen : GameComponent {
        public override void Draw(double deltaTime) {
            GraphicsBackend.Current.Clear();

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
    }
}
