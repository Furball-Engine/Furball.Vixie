using System;
using System.Numerics;
using FontStashSharp;
using FontStashSharp.RichText;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
#if USE_IMGUI
using ImGuiNET;
#endif

namespace Furball.Vixie.TestApplication.Tests; 

public class TestFSS : Screen {
    private Renderer     _vixieRenderer;
    private DynamicSpriteFont _font;

    private readonly FontSystem _defaultFont = new(new FontSystemSettings {
        FontResolutionFactor = 2f,
        KernelWidth          = 2,
        KernelHeight         = 2,
        Effect               = FontSystemEffect.None
    });

    public override void Initialize() {
        this._defaultFont.AddFont(ResourceHelpers.GetByteResource("Resources/font.ttf", typeof(TestGame)));
        this._font = this._defaultFont.GetFont(48);

        this._vixieRenderer = Game.ResourceFactory.CreateRenderer();

        RichTextDefaults.FontResolver = s => {
            return int.TryParse(s, out int size) 
                ? this._defaultFont.GetFont(size) 
                : this._font;
        };
        
        this._rtl = new RichTextLayout {
            Font = this._font,
            Text = "This is a test of the /c[red]rich /c[white]text drawing!/n/f[60]Bigger!/f[30]Smaller/fdNormal!"
        };

        base.Initialize();
    }

    private float          _scale    = 1f;
    private float          _rotation = 0f;
    private RichTextLayout _rtl;

    public override void Draw(double deltaTime) {
        this._vixieRenderer.Begin();
        this._rtl.Draw(this._vixieRenderer.FontRenderer, new Vector2(10), Color.White);
        this._vixieRenderer.DrawString(this._font, "VixieFontStashSharpRenderer Testing",                new Vector2(10,  100), Color.White,      this._rotation,                         new Vector2(_scale), new Vector2(-50));
        this._vixieRenderer.DrawString(this._font, "More Quite Long Text, Foxes Are Cute",               new Vector2(10,  200), Color.LightBlue,  (float)(2f * Math.PI - this._rotation), new Vector2(_scale));
        this._vixieRenderer.DrawString(this._font, "Did You Know That A Bee Should Not Be Able To Fly?", new Vector2(500, 300), Color.LightGreen, (float)(Math.PI - this._rotation),       new Vector2(_scale));
        this._vixieRenderer.End();
        
        this._vixieRenderer.Draw();

        #region ImGui menu
        #if USE_IMGUI
        ImGui.SliderFloat("Scale",    ref this._scale,    0f, 5f);
        ImGui.SliderFloat("Rotation", ref this._rotation, 0f, (float)(Math.PI * 2f));

        if (ImGui.Button("Go back to test selector")) {
            TestGame.Instance.ChangeScreen(new BaseTestSelector());
        }
        #endif
        #endregion

        base.Draw(deltaTime);
    }

    public override void Dispose() {
        this._defaultFont.Dispose();
        this._vixieRenderer.Dispose();

        base.Dispose();
    }
}