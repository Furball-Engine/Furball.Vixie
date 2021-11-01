using System;
using System.Globalization;
using System.Numerics;
using Furball.Vixie.Gl;
using Furball.Vixie.Graphics;
using Furball.Vixie.Helpers;
using Furball.Vixie.ImGuiHelpers;
using Furball.Vixie.Shaders;
using ImGuiNET;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Texture=Furball.Vixie.Gl.Texture;
using UniformType=Furball.Vixie.Gl.UniformType;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestBatchedRendering : GameComponent {
        private BatchedRenderer _batchedRenderer;

        private BufferObject _vertexBuffer;
        private BufferObject  _indexBuffer;
        private BasicTexturedShader _shader;
        private Texture             _texture;

        private VertexArrayObject _vertexArrayObject;

        private ImGuiController _imGuiController;

        public TestBatchedRendering(Game game) : base(game) {}

        public override void Initialize() {
            this._batchedRenderer = new BatchedRenderer();

            //Load the Texture
            this._texture = new Texture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

            this._imGuiController = ImGuiCreator.CreateController();

            base.Initialize();
        }

        private int CirnoDons = 63;

        public override void Draw(double deltaTime) {
            this._texture.Bind();

            this._batchedRenderer.Begin();

            for (int i = 0; i != CirnoDons; i++) {
                this._batchedRenderer.Draw(this._texture, new Vector2(i, 0), new Vector2(371, 326));
            }

            this._batchedRenderer.End();

            #region ImGui menu

            this._imGuiController.Update((float) deltaTime);

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector(this.BaseGame));
                this.BaseGame.Components.Remove(this);
            }

            ImGui.Text($"Quads: {this._batchedRenderer.QuadsDrawn}");
            ImGui.Text($"Draws: {this._batchedRenderer.DrawCalls}");

            ImGui.SliderInt("Draws", ref this.CirnoDons, 0, 128);

            this._imGuiController.Render();

            #endregion

            base.Draw(deltaTime);
        }
    }
}
