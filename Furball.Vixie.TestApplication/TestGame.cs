using Furball.Vixie.Helpers.Helpers;
using Furball.Vixie.TestApplication.Tests;
using ImGuiNET;
using Kettu;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;

namespace Furball.Vixie.TestApplication {
    public class TestGame : Game {
        public TestGame(WindowOptions options) {}

        protected override void Initialize() {
            this.Components.Add(new BaseTestSelector());

            GraphicsBackend.Current.ScreenshotTaken += delegate(object _, Image image) {
                Logger.Log("Writing screenshot!", LoggerLevelImageLoader.Instance);
                image.SaveAsPng("testoutput.png");
            };
            
            base.Initialize();
        }

        protected override void Draw(double deltaTime) {
            ImGui.Begin("Global Controls");
            
            if (ImGui.Button("Take Screenshot")) {
                GraphicsBackend.Current.TakeScreenshot();
            }
            
            ImGui.End();
            
            base.Draw(deltaTime);
        }
    }
}
