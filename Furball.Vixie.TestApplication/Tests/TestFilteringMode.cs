﻿using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers.Helpers;
#if USE_IMGUI
using ImGuiNET;
#endif

namespace Furball.Vixie.TestApplication.Tests;

public class TestFilteringMode : Screen {
    private Texture _pixelatedTexture;
    private Texture _smoothTexture;

    private Renderer _vixieRenderer;

    public override void Initialize() {
        this._vixieRenderer = Game.ResourceFactory.CreateRenderer();

        this._pixelatedTexture = Game.ResourceFactory.CreateTextureFromByteArray(
            ResourceHelpers.GetByteResource
                ("Resources/pippidonclear0.png", typeof(TestGame)));
        this._pixelatedTexture.FilterType = TextureFilterType.Pixelated;
        this._smoothTexture               = Game.ResourceFactory.CreateTextureFromByteArray(
                                                                 ResourceHelpers.GetByteResource(
                                                                     "Resources/pippidonclear0.png", typeof(TestGame)));
        this._smoothTexture.FilterType = TextureFilterType.Smooth;

        this._vixieRenderer.Begin();
        this._vixieRenderer.AllocateUnrotatedTexturedQuad(this._pixelatedTexture, Vector2.Zero, new Vector2(2),
                                                          Color.White);
        this._vixieRenderer.AllocateUnrotatedTexturedQuad(this._smoothTexture, new Vector2(100, 0), new Vector2(2),
                                                          Color.White);
        this._vixieRenderer.End();

        base.Initialize();
    }

    public override void Draw(double deltaTime) {
        this._vixieRenderer.Draw();

        #region ImGui menu

#if USE_IMGUI
        if (ImGui.Button("Go back to test selector"))
            TestGame.Instance.ChangeScreen(new BaseTestSelector());
#endif

        #endregion

        base.Draw(deltaTime);
    }
}