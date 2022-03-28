using System;
using System.Globalization;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.Graphics.Renderers;
using ImGuiNET;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestLineSmiley : GameComponent {
        private ILineRenderer renderer;
        
        public override void Initialize() {
            this.renderer = GraphicsBackend.Current.CreateLineRenderer();
            
            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            GraphicsBackend.Current.Clear();

            this.renderer.Begin();
            
            this.renderer.Draw(new(100, 100), new(100, 200), 2, Color.Blue);
            this.renderer.Draw(new(200, 100), new(200, 200), 2, Color.Green);
            this.renderer.Draw(new(100, 300), new(200, 300), 2, Color.Orange);
            
            this.renderer.End();
            
            #region ImGui menu

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );
            
            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            #endregion
            
            base.Draw(deltaTime);
        }
    }
}
