using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
#if USE_IMGUI
using ImGuiNET;
#endif


namespace Furball.Vixie.TestApplication.Tests; 

public class TestRenderTargets : GameComponent {
    private RenderTarget _renderTarget;
    private Renderer     _quadRenderer;
    private Texture      _whitePixel;
    private float        _scale = 1f;
    private Texture      _don;

    public override void Initialize() {
        this._renderTarget = new RenderTarget(200, 200);

        this._quadRenderer = GraphicsBackend.Current.CreateRenderer();
            
        this._whitePixel = Texture.CreateWhitePixelTexture();
        this._don = Texture.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png", typeof(TestGame)));

        base.Initialize();
    }

    public override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();

        this._renderTarget.Bind();
        GraphicsBackend.Current.Clear();

        this._quadRenderer.Begin();
        this._quadRenderer.AllocateUnrotatedTexturedQuad(this._whitePixel, new Vector2(5, 5), new Vector2(128, 128), Color.Green);
        this._quadRenderer.AllocateUnrotatedTexturedQuad(this._whitePixel, new Vector2(100, 100), new Vector2(100, 100), Color.Red);
        this._quadRenderer.AllocateUnrotatedTexturedQuad(this._whitePixel, new Vector2(150, 150), new Vector2(100, 100), Color.Blue);
        this._quadRenderer.End();
        
        this._quadRenderer.Draw();

        this._renderTarget.Unbind();
            
        this._quadRenderer.Begin();
        this._quadRenderer.AllocateUnrotatedTexturedQuad(this._renderTarget, Vector2.Zero, new Vector2(this._scale), Color.White);
        this._quadRenderer.End();
        
        this._quadRenderer.Draw();

        #region ImGui menu
        #if USE_IMGUI
        ImGui.SliderFloat("Final Texture Scale", ref this._scale, 0f, 2f);
        if (ImGui.Button("Go back to test selector")) {
            this.BaseGame.Components.Add(new BaseTestSelector());
            this.BaseGame.Components.Remove(this);
        }
        #endif
        #endregion

        base.Draw(deltaTime);
    }

    public override void Dispose() {
        this._quadRenderer.Dispose();
        this._renderTarget.Dispose();
        this._whitePixel.Dispose();
        this._don.Dispose();

        base.Dispose();
    }
}