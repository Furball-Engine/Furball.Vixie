#if USE_IMGUI
using ImGuiNET;
#endif

namespace Furball.Vixie.TestApplication; 

public class BaseTestSelector : GameComponent {
    public override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();

#if USE_IMGUI
        ImGui.Begin("Test Selector");
            
        if (ImGui.Button("Batched Rendering Test")) {
            this.BaseGame.Components.Add(new TestQuadRendering());
            this.BaseGame.Components.Remove(this);
        }

        if (ImGui.Button("TextureRenderTarget Test")) {
            this.BaseGame.Components.Add(new TestRenderTargets());
            this.BaseGame.Components.Remove(this);
        }
            
        if (ImGui.Button("Rotation Test")) {
            this.BaseGame.Components.Add(new TestRotation());
            this.BaseGame.Components.Remove(this);
        }
            
        if (ImGui.Button("Source Rectangle Rendering Test")) {
            this.BaseGame.Components.Add(new TestSourceRect());
            this.BaseGame.Components.Remove(this);
        }
            
        if (ImGui.Button("FontStashSharp Test")) {
            this.BaseGame.Components.Add(new TestFSS());
            this.BaseGame.Components.Remove(this);
        }
            
        if (ImGui.Button("Multiple Textures Test")) {
            this.BaseGame.Components.Add(new TestMultipleTextures());
            this.BaseGame.Components.Remove(this);
        }
        
        if (ImGui.Button("Filtering Mode Test")) {
            this.BaseGame.Components.Add(new TestFilteringMode());
            this.BaseGame.Components.Remove(this);
        }

        if (ImGui.Button("Test Texture.GetData")) {
            this.BaseGame.Components.Add(new TestTextureGetData());
            this.BaseGame.Components.Remove(this);
        }
        
        if (ImGui.Button("Test New Renderer")) {
            this.BaseGame.Components.Add(new TestNewRenderer());
            this.BaseGame.Components.Remove(this);
        }
        
        if (ImGui.Button("Empty Screen")) {
            this.BaseGame.Components.Add(new TestEmptyScreen());
            this.BaseGame.Components.Remove(this);
        }
        
        ImGui.End();
#endif

        base.Draw(deltaTime);
    }
}