using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.TextureEffects.Blur;
using Furball.Vixie.Helpers.Helpers;
#if USE_IMGUI
using ImGuiNET;
using System.Diagnostics;
#endif

namespace Furball.Vixie.TestApplication.Tests;

public class TestTextureEffect : Screen {
    private Renderer _renderer;
    private Texture  _sourceTexture;

    private OpenCLBoxBlurTextureEffect _clBlur;
    private CpuBoxBlurTextureEffect    _cpuBlur;
    public override void Initialize() {
        base.Initialize();

        this._renderer = Game.ResourceFactory.CreateRenderer();
        this._sourceTexture = Game.ResourceFactory.CreateTextureFromByteArray(
            ResourceHelpers.GetByteResource("Resources/pippidonclear0.png", typeof(TestGame)));

        this._clBlur =
            new OpenCLBoxBlurTextureEffect(TestGame.Instance.WindowManager.GraphicsBackend, this._sourceTexture);
        this._cpuBlur =
            new CpuBoxBlurTextureEffect(TestGame.Instance.WindowManager.GraphicsBackend, this._sourceTexture);

        this._clBlur.Passes        = 20;
        this._clBlur.KernelRadius  = 4;
        this._cpuBlur.Passes       = 20;
        this._cpuBlur.KernelRadius = 4;

        //Update both once to prevent any first-time-startup hiccups
        this._clBlur.UpdateTexture();
        this._cpuBlur.UpdateTexture();

        // const int n = 25;
        //
        // long start = Stopwatch.GetTimestamp();
        // for (int i = 0; i < n; i++) {
        //     this._clBlur.UpdateTexture();
        // }
        // long end = Stopwatch.GetTimestamp();
        //
        // double length = (end - start) / (double)Stopwatch.Frequency;
        // Console.WriteLine($"CL Blur took on average {length * 1000d / n} miliseconds over {n} runs");
        //
        // start = Stopwatch.GetTimestamp();
        // for (int i = 0; i < n; i++) {
        //     this._cpuBlur.UpdateTexture();
        // }
        // end = Stopwatch.GetTimestamp();
        //
        // length = (end - start) / (double)Stopwatch.Frequency;
        // Console.WriteLine($"CPU Blur took on average {length * 1000d / n} miliseconds over {n} runs");

        this._renderer.Begin();
        this._renderer.AllocateUnrotatedTexturedQuad(this._sourceTexture, Vector2.Zero, Vector2.One, Color.White);
        this._renderer.AllocateUnrotatedTexturedQuad(this._clBlur.Texture, new Vector2(this._sourceTexture.Width, 0),
                                                     Vector2.One, Color.White);
        this._renderer.AllocateUnrotatedTexturedQuad(this._cpuBlur.Texture,
                                                     new Vector2(this._sourceTexture.Width * 2, 0),
                                                     Vector2.One, Color.White);
        this._renderer.End();
    }

    private double _clTimeTaken;
    private double _cpuTimeTaken;
    private int    _clBlurPasses        = 0;
    private int    _cpuBlurPasses       = 0;
    private int    _clBlurKernelRadius  = 0;
    private int    _cpuBlurKernelRadius = 0;
    public override void Draw(double delta) {
#if USE_IMGUI
        this._clBlurPasses        = this._clBlur.Passes;
        this._cpuBlurPasses       = this._cpuBlur.Passes;
        this._clBlurKernelRadius  = this._clBlur.KernelRadius;
        this._cpuBlurKernelRadius = this._cpuBlur.KernelRadius;


        ImGui.Begin("TestTextureEffect");

        ImGui.Text("OpenCL Blur");
        ImGui.SliderInt("OpenCL Passes", ref this._clBlur.Passes, 1, 100);
        ImGui.SliderInt("OpenCL Kernel Radius", ref this._clBlur.KernelRadius, 1, 10);

        ImGui.Text("CPU Blur");
        ImGui.SliderInt("CPU Passes", ref this._cpuBlur.Passes, 1, 100);
        ImGui.SliderInt("CPU Kernel Radius", ref this._cpuBlur.KernelRadius, 1, 10);

        ImGui.End();

        if (this._clBlurPasses != this._clBlur.Passes || this._clBlurKernelRadius != this._clBlur.KernelRadius) {
            //Track the amount of time it takes too
            long start = Stopwatch.GetTimestamp();
            this._clBlur.UpdateTexture();
            long end = Stopwatch.GetTimestamp();

            this._clTimeTaken = (end - start) / (double)Stopwatch.Frequency;
        }

        if (this._cpuBlurPasses != this._cpuBlur.Passes || this._cpuBlurKernelRadius != this._cpuBlur.KernelRadius) {
            long start = Stopwatch.GetTimestamp();
            this._cpuBlur.UpdateTexture();
            long end = Stopwatch.GetTimestamp();

            this._cpuTimeTaken = (end - start) / (double)Stopwatch.Frequency;
        }

        ImGui.Begin("Results");

        ImGui.Text($"OpenCL Blur took {this._clTimeTaken * 1000d} miliseconds");
        ImGui.Text($"CPU Blur took {this._cpuTimeTaken   * 1000d} miliseconds");

        ImGui.End();
#endif

        base.Draw(delta);

        this._renderer.Draw();
    }

    public override void Dispose() {
        base.Dispose();

        this._renderer.Dispose();
        this._cpuBlur.Dispose();
        this._clBlur.Dispose();
        this._sourceTexture.Dispose();
    }
}