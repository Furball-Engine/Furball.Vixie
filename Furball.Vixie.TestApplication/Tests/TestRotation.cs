using System;
using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Renderers.OpenGL;
using Furball.Vixie.Helpers;

using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestRotation : GameComponent {
        private ImmediateRenderer _immediateRenderer;
        private BatchedRenderer   _batchedRenderer;
        private Texture           _whiteTexture;

        

        public override void Initialize() {
            this._immediateRenderer = new ImmediateRenderer();
            this._batchedRenderer   = new BatchedRenderer();
            this._whiteTexture      = new Texture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

            

            base.Initialize();
        }

        private float _rotation = 1f;

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._batchedRenderer.Begin();
            this._batchedRenderer.Draw(this._whiteTexture, new Vector2(1280/2, 720/2), new Vector2(371, 356), Vector2.Zero, _rotation);
            this._batchedRenderer.End();



            #region ImGui menu

            

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );

            ImGui.DragFloat("Rotation", ref this._rotation, 0.01f, 0f, 8f);

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            

            #endregion

            base.Draw(deltaTime);
        }

        public override void Dispose() {
            this._batchedRenderer.Dispose();
            this._immediateRenderer.Dispose();
            this._whiteTexture.Dispose();
            

            base.Dispose();
        }
    }
}
