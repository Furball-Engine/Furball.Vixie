using System.Numerics;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
#if USE_IMGUI
using ImGuiNET;
#endif
using SixLabors.ImageSharp;
using Color=Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie.TestApplication.Tests; 

public class TestQuadRendering : Screen {
    private Renderer _vixieRenderer;
    private Texture       _texture;

    public override void Initialize() {
        this._vixieRenderer = TestGame.Instance.ResourceFactory.CreateRenderer();

        //Load the Texture
        this._texture = TestGame.Instance.ResourceFactory.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png", typeof(TestGame)));

        base.Initialize();
    }

    /// <summary>
    /// Amount of Dons to draw on screen each frame
    /// </summary>
    private int _cirnoDons = 1024;
    private bool _scissorEnable = false;

    public override void Draw(double deltaTime) {
        if (this._scissorEnable)
            TestGame.Instance.WindowManager.GraphicsBackend.ScissorRect = new Rectangle(100, 100, 400, 200);
            
        this._vixieRenderer.Begin();

        for (int i = 0; i != this._cirnoDons; i++) {
            this._vixieRenderer.AllocateUnrotatedTexturedQuad(this._texture, new Vector2(i % 1024, i % 2 == 0 ? 0 : 200), new Vector2(0.5f), new Color(1f, 1f, 1f, 0.5f));
        }

        this._vixieRenderer.End();
        
        this._vixieRenderer.Draw();
            
        TestGame.Instance.WindowManager.GraphicsBackend.SetFullScissorRect();

        #region ImGui menu
        #if USE_IMGUI
        if (ImGui.Button("Go back to test selector")) {
            TestGame.Instance.ChangeScreen(new BaseTestSelector());
        }
        ImGui.SliderInt("Draws", ref this._cirnoDons, 0, 2048);
        ImGui.Checkbox("Scissor", ref this._scissorEnable);

        #endif
        #endregion

        base.Draw(deltaTime);
    }

    public override void Dispose() {
        this._vixieRenderer.Dispose();
        this._texture.Dispose();

        base.Dispose();
    }
}