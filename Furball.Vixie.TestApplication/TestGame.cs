#nullable enable
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

    private Screen? _runningScreen;
    public void ChangeScreen(Screen screen) {
        this._runningScreen?.Dispose();
        
        screen.Initialize();

        this._runningScreen = screen;
    }
    
    protected override void Initialize() {
        Instance = this;
        
        this.ChangeScreen(new TestNewRenderer());

        this.WindowManager.GraphicsBackend.ScreenshotTaken += delegate(object _, Image image) {
            Logger.Log("Writing screenshot!", LoggerLevelImageLoader.Instance);
            image.SaveAsPng("testoutput.png");
        };
        
        Logger.Log("Initializing TestGame!", LoggerLevelUnknown.Instance);
            
        base.Initialize();
    }

    private       double _updateDelta;
    private const double UPDATE_RATE = 1000f;
    private       long   _alloccedMemory;
        
    
    public void Run(Backend backend = Backend.None) {
        base.Run(backend);
    }

    public new void RunHeadless() {
        base.RunHeadless();
    }

    protected override void Update(double deltaTime) {
        this._runningScreen?.Update(deltaTime);
        
        base.Update(deltaTime);
    }

    protected override void Draw(double deltaTime) {
        this.WindowManager.GraphicsBackend.Clear();
        
        this._runningScreen?.Draw(deltaTime);
        
#if USE_IMGUI
        ImGui.Begin("Global Controls");
            
        ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                   $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
        );
#endif

        this._updateDelta += deltaTime;

        if (this._updateDelta > UPDATE_RATE) {
            this._alloccedMemory = GC.GetTotalMemory(true);
            this._updateDelta    -= UPDATE_RATE;
        }
    
#if USE_IMGUI
        ImGui.Text($"RAM Usage: {this._alloccedMemory}");
            
        if (ImGui.Button("Take Screenshot")) {
            this.WindowManager.GraphicsBackend.TakeScreenshot();
        }

        if (ImGui.Button("Force GC Clear")) {
            GC.Collect();
        }

        if (ImGui.Button("Recreate Window")) {
            throw new NotImplementedException();
        }
        
        ImGui.End();
#endif
            
        base.Draw(deltaTime);
    }
}