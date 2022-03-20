using System;

using System.Globalization;
using System.Numerics;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Furball.Vixie.FontStashSharp;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Helpers;
using ImGuiNET;

namespace Furball.Vixie.TestApplication.Tests {
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

        private float _scale = 1f;
        private float _rotation = 0f;

        public override void Draw(double deltaTime) {
            GraphicsBackend.Current.Clear();

            this._quadRendererGl.Begin();
            this._quadRendererGl.DrawString(this._font, "VixieFontStashSharpRenderer Testing", new Vector2(10, 10), Color.White, this._rotation, new Vector2(_scale));
            this._quadRendererGl.End();

            //#region ImGui menu
//
            //ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
            //           $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            //);
            //
            //ImGui.SliderFloat("Scale",    ref this._scale,    0f, 5f);
            //ImGui.SliderFloat("Rotation", ref this._rotation, 0f, (float)(Math.PI * 2f));
            //
            //if (ImGui.Button("Go back to test selector")) {
            //    this.BaseGame.Components.Add(new BaseTestSelector());
            //    this.BaseGame.Components.Remove(this);
            //}
//
            //#endregion

            base.Draw(deltaTime);
        }

        public override void Dispose() {
            this._defaultFont.Dispose();
            this._quadRendererGl.Dispose();

            base.Dispose();
        }
    }
}
