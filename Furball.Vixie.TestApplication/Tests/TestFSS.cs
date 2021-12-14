using System;

using System.Globalization;
using System.Numerics;
using FontStashSharp;
using Furball.Vixie.FontStashSharp;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Renderers.OpenGL;
using Furball.Vixie.Helpers;

using ImGuiNET;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestFSS : GameComponent {
        private VixieFontStashRenderer _renderer;
        private ImmediateRenderer        _immediateRenderer;
        private DynamicSpriteFont      _font;

        public readonly FontSystem DEFAULT_FONT = new(new FontSystemSettings {
            FontResolutionFactor = 2f,
            KernelWidth          = 2,
            KernelHeight         = 2,
            Effect               = FontSystemEffect.None
        });

        public override void Initialize() {
            this.DEFAULT_FONT.AddFont(ResourceHelpers.GetByteResource("Resources/font.ttf"));
            this._font = this.DEFAULT_FONT.GetFont(48);

            this._immediateRenderer = new ImmediateRenderer();
            this._renderer        = new VixieFontStashRenderer(this._immediateRenderer);

            base.Initialize();
        }

        private float _scale = 1f;

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._immediateRenderer.Begin();
            this._immediateRenderer.DrawString(this._font, "VixieFontStashSharpRenderer Testing", new Vector2(10, 10), Color.White, 0f, new Vector2(_scale));
            this._immediateRenderer.End();

            #region ImGui menu

            

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );

            ImGui.SliderFloat("Scale", ref this._scale, 0.1f, 5f);

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            

            #endregion

            base.Draw(deltaTime);
        }

        public override void Dispose() {
            this.DEFAULT_FONT.Dispose();
            this._immediateRenderer.Dispose();

            base.Dispose();
        }
    }
}
