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
        private FontSystem             _system;
        private VixieFontStashRenderer _renderer;
        private BatchedRenderer        _batchedRenderer;
        private DynamicSpriteFont      _font;
        private ImGuiController        _imGuiController;

        public override void Initialize() {
            this._system = new FontSystem();
            this._system.AddFont(ResourceHelpers.GetByteResource("Resources/font.ttf"));
            this._font = this._system.GetFont(48);

            this._batchedRenderer = new BatchedRenderer();
            this._renderer        = new VixieFontStashRenderer(this._batchedRenderer);
            this._imGuiController      = ImGuiCreator.CreateController();

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._batchedRenderer.Begin();
            this._batchedRenderer.DrawString(this._font, "VixieFontStashSharpRenderer Testing", new Vector2(10, 10), Color.White);
            this._batchedRenderer.End();



            #region ImGui menu

            this._imGuiController.Update((float) deltaTime);

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            this._imGuiController.Render();

            #endregion


            base.Draw(deltaTime);
        }

        public override void Dispose() {
            this._system.Dispose();
            this._batchedRenderer.Dispose();
            this._imGuiController.Dispose();

            base.Dispose();
        }
    }
}
