using System;
using System.Numerics;
using FontStashSharp;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;

namespace Furball.Vixie.TestApplication.Tests; 

public class TestFSS : GameComponent {
    private IQuadRenderer     _quadRendererGl;
    private DynamicSpriteFont _font;

    private readonly FontSystem _defaultFont = new(new FontSystemSettings {
        FontResolutionFactor = 2f,
        KernelWidth          = 2,
        KernelHeight         = 2,
        Effect               = FontSystemEffect.None
    });

    public override void Initialize() {
        this._defaultFont.AddFont(ResourceHelpers.GetByteResource("Resources/font.ttf"));
        this._font = this._defaultFont.GetFont(48);

        this._quadRendererGl = GraphicsBackend.Current.CreateTextureRenderer();

        base.Initialize();
    }

    private float _scale    = 1f;
    private float _rotation = 0f;

    public override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();

        this._quadRendererGl.Begin();
        this._quadRendererGl.DrawString(this._font, "VixieFontStashSharpRenderer Testing",                new Vector2(10,  100), Color.White,      this._rotation,                         new Vector2(_scale), new(-50));
        this._quadRendererGl.DrawString(this._font, "More Quite Long Text, Foxes Are Cute",               new Vector2(10,  200), Color.LightBlue,  (float)(2f * Math.PI - this._rotation), new Vector2(_scale));
        this._quadRendererGl.DrawString(this._font, "Did You Know That A Bee Should Not Be Able To Fly?", new Vector2(500, 300), Color.LightGreen, (float)(Math.PI - this._rotation),       new Vector2(_scale));
        this._quadRendererGl.End();

        #region ImGui menu

        ImGui.SliderFloat("Scale",    ref this._scale,    0f, 5f);
        ImGui.SliderFloat("Rotation", ref this._rotation, 0f, (float)(Math.PI * 2f));

        if (ImGui.Button("Go back to test selector")) {
            this.BaseGame.Components.Add(new BaseTestSelector());
            this.BaseGame.Components.Remove(this);
        }

        #endregion

        base.Draw(deltaTime);
    }

    public override void Dispose() {
        this._defaultFont.Dispose();
        this._quadRendererGl.Dispose();

        base.Dispose();
    }
}