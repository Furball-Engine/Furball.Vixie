using System;
using System.Diagnostics;
using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.TextureEffects.Blur;
using Furball.Vixie.Helpers.Helpers;

namespace Furball.Vixie.TestApplication.Tests;

public class TestTextureEffect : Screen {
    private Renderer _renderer;
    private Texture  _sourceTexture;

    private BoxBlurTextureEffect _blur;
    public override void Initialize() {
        base.Initialize();

        this._renderer = Game.ResourceFactory.CreateRenderer();
        this._sourceTexture = Game.ResourceFactory.CreateTextureFromByteArray(
            ResourceHelpers.GetByteResource("Resources/pippidonclear0.png", typeof(TestGame)));

        this._blur = new OpenCLBoxBlurTextureEffect(TestGame.Instance.WindowManager.GraphicsBackend, this._sourceTexture);

        const int n = 50;

        long start = Stopwatch.GetTimestamp();
        for (int i = 0; i < n; i++) {
            this._blur.UpdateTexture();
        }
        long end = Stopwatch.GetTimestamp();

        double length = (end - start) / (double)Stopwatch.Frequency;
        Console.WriteLine($"Blur took on average {length * 1000d / n} miliseconds over {n} runs");

        this._renderer.Begin();
        this._renderer.AllocateUnrotatedTexturedQuad(this._sourceTexture, Vector2.Zero, Vector2.One, Color.White);
        this._renderer.AllocateUnrotatedTexturedQuad(this._blur.Texture, new Vector2(this._sourceTexture.Width, 0),
                                                     Vector2.One, Color.White);
        this._renderer.End();
    }

    public override void Draw(double delta) {
        base.Draw(delta);

        this._renderer.Draw();
    }

    public override void Dispose() {
        base.Dispose();

        this._renderer.Dispose();
        this._blur.Dispose();
        this._sourceTexture.Dispose();
    }
}