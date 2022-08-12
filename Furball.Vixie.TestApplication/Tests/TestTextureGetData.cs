using System;
using System.Numerics;
using FontStashSharp;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie.TestApplication.Tests; 

public class TestTextureGetData : GameComponent {
    private bool _testPassed;
    
    private readonly FontSystem _defaultFont = new(new FontSystemSettings {
        FontResolutionFactor = 2f,
        KernelWidth          = 2,
        KernelHeight         = 2,
        Effect               = FontSystemEffect.None
    });
    private DynamicSpriteFont _font;
    private IQuadRenderer     _quadRendererGl;

    public override void Initialize() {
        base.Initialize();

        byte[] origData = ResourceHelpers.GetByteResource("Resources/pippidonclear0.png");

        Configuration config = Configuration.Default;
        config.PreferContiguousImageBuffers = true;
        
        Image<Rgba32> image  = Image.Load<Rgba32>(config, origData);
        if (!image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
            throw new Exception("Failed to get pixels data");

        Rgba32[] origPixels = pixels.ToArray();

        Texture tex = Resources.CreateTextureFromByteArray(origData);

        this._testPassed = true;
        
        Rgba32[] data = tex.GetData();
        for (int i = 0; i < data.Length; i++) {
            Rgba32 pixel = data[i];

            if (origPixels[i] != pixel)
                this._testPassed = false;
        }
        
        this._defaultFont.AddFont(ResourceHelpers.GetByteResource("Resources/font.ttf"));
        this._font = this._defaultFont.GetFont(48);

        this._quadRendererGl = GraphicsBackend.Current.CreateTextureRenderer();
    }

    public override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();

        this._quadRendererGl.Begin();
        this._quadRendererGl.DrawString(this._font, $"Result: {this._testPassed}", new Vector2(10), this._testPassed ? System.Drawing.Color.LightGreen : System.Drawing.Color.Red);
        this._quadRendererGl.End();
        
        #region ImGui menu

        if (ImGui.Button("Go back to test selector")) {
            this.BaseGame.Components.Add(new BaseTestSelector());
            this.BaseGame.Components.Remove(this);
        }

        #endregion
        
        base.Draw(deltaTime);
    }

    public override void Dispose() {
        base.Dispose();
        
        this._defaultFont.Dispose();
        this._quadRendererGl.Dispose();
    }
}