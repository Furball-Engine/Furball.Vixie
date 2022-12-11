
using Furball.Vixie.TestApplication.Tests;
#if USE_IMGUI
using ImGuiNET;
#endif

namespace Furball.Vixie.TestApplication; 

public class BaseTestSelector : Screen {
    public override void Draw(double deltaTime) {
#if USE_IMGUI
        ImGui.Begin("Test Selector");
        
        if (ImGui.Button("Batched Rendering Test")) {
            TestGame.Instance.ChangeScreen(new TestQuadRendering());
        }

        if (ImGui.Button("TextureRenderTarget Test")) {
            TestGame.Instance.ChangeScreen(new TestRenderTargets());
        }
            
        if (ImGui.Button("Rotation Test")) {
            TestGame.Instance.ChangeScreen(new TestRotation());
        }
            
        if (ImGui.Button("Source Rectangle Rendering Test")) {
            TestGame.Instance.ChangeScreen(new TestSourceRect());
        }
            
        if (ImGui.Button("FontStashSharp Test")) {
            TestGame.Instance.ChangeScreen(new TestFSS());
        }
            
        if (ImGui.Button("Multiple Textures Test")) {
            TestGame.Instance.ChangeScreen(new TestMultipleTextures());
        }
        
        if (ImGui.Button("Filtering Mode Test")) {
            TestGame.Instance.ChangeScreen(new TestFilteringMode());
        }

        if (ImGui.Button("Test Texture.GetData")) {
            TestGame.Instance.ChangeScreen(new TestTextureGetData());
        }
        
        if (ImGui.Button("Test Texture.CopyTo")) {
            TestGame.Instance.ChangeScreen(new TestCopyTexture());
        }
        
        if (ImGui.Button("Test Texture Effects")) {
            TestGame.Instance.ChangeScreen(new TestTextureEffect());
        }
        
        if (ImGui.Button("Test New Renderer")) {
            TestGame.Instance.ChangeScreen(new TestNewRenderer());
        }
        
        if (ImGui.Button("Empty Screen")) {
            TestGame.Instance.ChangeScreen(new TestEmptyScreen());
        }
        
        ImGui.End();
#endif

        base.Draw(deltaTime);
    }
}