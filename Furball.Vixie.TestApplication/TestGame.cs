using System;
using System.Globalization;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;
using Kettu;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;

namespace Furball.Vixie.TestApplication; 

public class TestGame : Game {
    public TestGame() {}

    public static TestGame Instance;
    
    protected override void Initialize() {
        Instance = this;
        
        this.Components.Add(new BaseTestSelector());

        GraphicsBackend.Current.ScreenshotTaken += delegate(object _, Image image) {
            Logger.Log("Writing screenshot!", LoggerLevelImageLoader.Instance);
            image.SaveAsPng("testoutput.png");
        };
            
        base.Initialize();
    }

    private       double updateDelta = 5f;
    private const double UPDATE_RATE = 1f;
    private       long   alloccedMemory;
        
    public void Run(Backend backend = Backend.None) {
        var options = WindowOptions.Default;

        options.VSync = false;
            
        this.Run(options, backend);
    }
        
    protected override void Draw(double deltaTime) {
        ImGui.Begin("Global Controls");
            
        ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                   $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
        );

        this.updateDelta += deltaTime;

        if (this.updateDelta > UPDATE_RATE) {
            this.alloccedMemory = GC.GetTotalMemory(true);
            this.updateDelta    = 0;
        }
            
        ImGui.Text($"RAM Usage: {this.alloccedMemory}");
            
        if (ImGui.Button("Take Screenshot")) {
            GraphicsBackend.Current.TakeScreenshot();
        }

        if (ImGui.Button("Force GC Clear")) {
            GC.Collect();
        }

        if (ImGui.Button("Recreate Window")) {
            this.RecreateWindow();
        }
        
        ImGui.End();
            
        base.Draw(deltaTime);
    }
}