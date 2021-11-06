using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Numerics;
using FontStashSharp;
using Furball.Vixie.FontStashSharp;
using Furball.Vixie.Graphics.Renderers.OpenGL;
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
            this._system.AddFont(File.ReadAllBytes("font.ttf"));
            this._font = this._system.GetFont(72);

            this._batchedRenderer = new BatchedRenderer();
            this._renderer        = new VixieFontStashRenderer(this._batchedRenderer);
            this._imGuiController      = ImGuiCreator.CreateController();

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._batchedRenderer.Begin();
            this._font.DrawText(this._renderer, "i am in severe pain please make text rendering work", new Vector2(100, 100), Color.White);
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
    }
}
