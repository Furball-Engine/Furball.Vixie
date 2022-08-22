using System;
using System.Globalization;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Helpers.Helpers;
using Furball.Vixie.TestApplication.Tests;
#if USE_IMGUI
using ImGuiNET;
#endif
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

    public new void RunHeadless() {
        base.RunHeadless();
    }

    protected override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();

#if USE_IMGUI
        ImGui.Begin("Global Controls");
            
        ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                   $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
        );
#endif

        
        this.updateDelta += deltaTime;
        
        if (this.updateDelta > UPDATE_RATE) {
            this.alloccedMemory = GC.GetTotalMemory(true);
            this.updateDelta    = 0;
        }
    
#if USE_IMGUI
        ImGui.Text($"RAM Usage: {this.alloccedMemory}");
            
        if (ImGui.Button("Take Screenshot")) {
            GraphicsBackend.Current.TakeScreenshot();
        }
        
        if (ImGui.Button("Force GC Clear")) {
            GC.Collect();
        }

        if (ImGui.Button("Recreate Window")) {
            this.WindowManager.Backend = this.WindowManager.Backend == Backend.OpenGL ? Backend.Direct3D11 : Backend.OpenGL;
            this.RecreateWindow();
        }
        
        ImGui.End();
#endif
            
        base.Draw(deltaTime);
    }
}