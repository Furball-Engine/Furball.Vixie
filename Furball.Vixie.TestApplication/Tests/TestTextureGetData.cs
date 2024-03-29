﻿using System.Numerics;
using FontStashSharp;
using Furball.Vixie.Helpers.Helpers;
#if USE_IMGUI
using ImGuiNET;
#endif
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie.TestApplication.Tests; 

public class TestTextureGetData : Screen {
    private bool _testPassed;
    
    private readonly FontSystem _defaultFont = new(new FontSystemSettings {
        FontResolutionFactor = 2f,
        KernelWidth          = 2,
        KernelHeight         = 2,
    });
    private DynamicSpriteFont _font;
    private Renderer     _vixieRenderer;

    public override void Initialize() {
        base.Initialize();

        byte[] origData = ResourceHelpers.GetByteResource("Resources/pippidonclear0.png", typeof(TestGame));

        Image<Rgba32> image  = Image.Load<Rgba32>(origData);

        Rgba32[] origPixels = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(origPixels);

        Texture tex = Game.ResourceFactory.CreateTextureFromByteArray(origData);

        this._testPassed = true;
        
        Rgba32[] data = tex.GetData();
        for (int i = 0; i < data.Length; i++) {
            Rgba32 pixel = data[i];

            if (origPixels[i] != pixel)
                this._testPassed = false;
        }
        
        this._defaultFont.AddFont(ResourceHelpers.GetByteResource("Resources/font.ttf", typeof(TestGame)));
        this._font = this._defaultFont.GetFont(48);

        this._vixieRenderer = Game.ResourceFactory.CreateRenderer();
        
        this._vixieRenderer.Begin();
        this._vixieRenderer.DrawString(this._font, $"Result: {this._testPassed}", new Vector2(10), this._testPassed ? Color.LightGreen : Color.Red);
        this._vixieRenderer.End();
    }

    public override void Draw(double deltaTime) {
        this._vixieRenderer.Draw();
        
        #region ImGui menu
        #if USE_IMGUI
        if (ImGui.Button("Go back to test selector")) {
            TestGame.Instance.ChangeScreen(new BaseTestSelector());
        }
        #endif
        #endregion
        
        base.Draw(deltaTime);
    }

    public override void Dispose() {
        base.Dispose();
        
        this._defaultFont.Dispose();
        this._vixieRenderer.Dispose();
    }
}