using System;
using System.Globalization;
using Furball.Vixie.Helpers.Helpers;
using Furball.Vixie.TestApplication.Tests;
using ImGuiNET;
using Kettu;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;

namespace Furball.Vixie.TestApplication {
    public class TestGame : Game {
        protected override void Initialize() {
            this.Components.Add(new TestQuadRendering());

            GraphicsBackend.Current.ScreenshotTaken += delegate(object _, Image image) {
                Logger.Log("Writing screenshot!", LoggerLevelImageLoader.Instance);
                image.SaveAsPng("testoutput.png");
            };
            
            base.Initialize();
        }

        private       double updateDelta = 5f;
        private const double UPDATE_RATE = 1f;
        private       long   alloccedMemory;
        
        protected override void Draw(double deltaTime) {
            // ImGui.Begin("Global Controls");
            //
            // ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
            //            $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            // );
            //
            // this.updateDelta += deltaTime;
            //
            // if (this.updateDelta > UPDATE_RATE) {
            //     this.alloccedMemory = GC.GetTotalMemory(true);
            //     this.updateDelta    = 0;
            // }
            //
            // ImGui.Text($"RAM Usage: {this.alloccedMemory}");
            //
            // if (ImGui.Button("Take Screenshot")) {
            //     GraphicsBackend.Current.TakeScreenshot();
            // }
            //
            // if (ImGui.Button("Force GC Clear")) {
            //     GC.Collect();
            // }
            //
            // ImGui.End();
            
            base.Draw(deltaTime);
        }
    }
}
