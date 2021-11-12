using System;

using System.Globalization;
using System.Numerics;
using FontStashSharp;
using Furball.Vixie.FontStashSharp;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Renderers.OpenGL;
using Furball.Vixie.Helpers;
using Furball.Vixie.ImGuiHelpers;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestFSS : GameComponent {
        private VixieFontStashRenderer _renderer;
        private BatchedRenderer        _batchedRenderer;
        private DynamicSpriteFont      _font;
        private ImGuiController        _imGuiController;

        public readonly FontSystem DEFAULT_FONT = new(new FontSystemSettings {
            FontResolutionFactor = 2f,
            KernelWidth          = 2,
            KernelHeight         = 2,
            Effect               = FontSystemEffect.None
        });

        public override void Initialize() {
            this.DEFAULT_FONT.AddFont(ResourceHelpers.GetByteResource("Resources/font.ttf"));
            this._font = this.DEFAULT_FONT.GetFont(48);

            this._batchedRenderer = new BatchedRenderer();
            this._renderer        = new VixieFontStashRenderer(this._batchedRenderer);
            this._imGuiController      = ImGuiCreator.CreateController();

            base.Initialize();
        }

        private float _scale = 1f;

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._batchedRenderer.Begin();
            this._batchedRenderer.DrawString(this._font, "VixieFontStashSharpRenderer Testing", new Vector2(10, 10), Color.White, 0f, new Vector2(_scale));
            this._batchedRenderer.End();



            #region ImGui menu

            this._imGuiController.Update((float) deltaTime);

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );

            ImGui.SliderFloat("Scale", ref this._scale, 0.1f, 5f);

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            this._imGuiController.Render();

            #endregion


            base.Draw(deltaTime);
        }

        public override void Dispose() {
            this.DEFAULT_FONT.Dispose();
            this._batchedRenderer.Dispose();
            this._imGuiController.Dispose();

            base.Dispose();
        }
    }
}
