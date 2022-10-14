using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
#if USE_IMGUI
using ImGuiNET;
#endif


namespace Furball.Vixie.TestApplication.Tests; 

public class TestRenderTargets : Screen {
    private RenderTarget _renderTarget;
    private Renderer     _quadVixieRenderer;
    private Texture      _whitePixel;
    private float        _scale = 1f;
    private Texture      _don;

    public override void Initialize() {
        this._renderTarget = TestGame.Instance.ResourceFactory.CreateRenderTarget(200, 200);

        this._quadVixieRenderer = TestGame.Instance.ResourceFactory.CreateRenderer();
            
        this._whitePixel = TestGame.Instance.ResourceFactory.CreateWhitePixelTexture();
        this._don = TestGame.Instance.ResourceFactory.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png", typeof(TestGame)));

        base.Initialize();
    }

    public override void Draw(double deltaTime) {
        this._renderTarget.Bind();
        TestGame.Instance.WindowManager.GraphicsBackend.Clear();

        this._quadVixieRenderer.Begin();
        this._quadVixieRenderer.AllocateUnrotatedTexturedQuad(this._whitePixel, new Vector2(5, 5), new Vector2(128, 128), Color.Green);
        this._quadVixieRenderer.AllocateUnrotatedTexturedQuad(this._whitePixel, new Vector2(100, 100), new Vector2(100, 100), Color.Red);
        this._quadVixieRenderer.AllocateUnrotatedTexturedQuad(this._whitePixel, new Vector2(150, 150), new Vector2(100, 100), Color.Blue);
        this._quadVixieRenderer.End();
        
        this._quadVixieRenderer.Draw();

        this._renderTarget.Unbind();
            
        this._quadVixieRenderer.Begin();
        this._quadVixieRenderer.AllocateUnrotatedTexturedQuad(this._renderTarget, Vector2.Zero, new Vector2(this._scale), Color.White);
        this._quadVixieRenderer.End();
        
        this._quadVixieRenderer.Draw();

        #region ImGui menu
        #if USE_IMGUI
        ImGui.SliderFloat("Final Texture Scale", ref this._scale, 0f, 2f);
        if (ImGui.Button("Go back to test selector")) {
            TestGame.Instance.ChangeScreen(new BaseTestSelector());
        }
        #endif
        #endregion

        base.Draw(deltaTime);
    }

    public override void Dispose() {
        this._quadVixieRenderer.Dispose();
        this._renderTarget.Dispose();
        this._whitePixel.Dispose();
        this._don.Dispose();

        base.Dispose();
    }
}