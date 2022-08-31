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

public class TestFSS : GameComponent {
    private Renderer     _renderer;
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

        this._renderer = GraphicsBackend.Current.CreateRenderer();

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
        GraphicsBackend.Current.Clear();

        this._renderer.Begin();
        this._rtl.Draw(this._renderer.FontRenderer, new Vector2(10), System.Drawing.Color.White);
        this._renderer.DrawString(this._font, "VixieFontStashSharpRenderer Testing",                new Vector2(10,  100), Color.White,      this._rotation,                         new Vector2(_scale), new Vector2(-50));
        this._renderer.DrawString(this._font, "More Quite Long Text, Foxes Are Cute",               new Vector2(10,  200), Color.LightBlue,  (float)(2f * Math.PI - this._rotation), new Vector2(_scale));
        this._renderer.DrawString(this._font, "Did You Know That A Bee Should Not Be Able To Fly?", new Vector2(500, 300), Color.LightGreen, (float)(Math.PI - this._rotation),       new Vector2(_scale));
        this._renderer.End();
        
        this._renderer.Draw();

        #region ImGui menu
        #if USE_IMGUI
        ImGui.SliderFloat("Scale",    ref this._scale,    0f, 5f);
        ImGui.SliderFloat("Rotation", ref this._rotation, 0f, (float)(Math.PI * 2f));

        if (ImGui.Button("Go back to test selector")) {
            this.BaseGame.Components.Add(new BaseTestSelector());
            this.BaseGame.Components.Remove(this);
        }
        #endif
        #endregion

        base.Draw(deltaTime);
    }

    public override void Dispose() {
        this._defaultFont.Dispose();
        this._renderer.Dispose();

        base.Dispose();
    }
}