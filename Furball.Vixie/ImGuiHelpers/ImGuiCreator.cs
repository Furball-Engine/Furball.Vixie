using Silk.NET.Input;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Furball.Vixie.ImGuiHelpers {
    public class ImGuiCreator {
        public static ImGuiController CreateController() =>
            new ImGuiController(Global.Gl,
                                Global.GameInstance.WindowManager.GameWindow,
                                Global.GameInstance.WindowManager.GameWindow.CreateInput()
            );
    }
}
