using System.Numerics;
using Furball.Vixie.Backends.Shared;
using ImGuiNET;


namespace Furball.Vixie.TestApplication.Tests; 

public class TestTextureRenderTargets : GameComponent {
    private RenderTarget _renderTarget;
    private Renderer     _quadRenderer;
    private Texture      _whitePixel;
    private float        _scale = 1f;

    public override void Initialize() {
        this._renderTarget = new RenderTarget(200, 200);

        this._quadRenderer = new Renderer();
            
        this._whitePixel = Texture.CreateWhitePixelTexture();

        base.Initialize();
    }

    public override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();

        this._renderTarget.Bind();
        GraphicsBackend.Current.Clear();

        this._quadRenderer.Begin();
        this._quadRenderer.Draw(this._whitePixel, new Vector2(5, 5), new Vector2(128, 128), Color.Green);
        this._quadRenderer.End();

        this._renderTarget.Unbind();
            
        this._quadRenderer.Begin();
        this._quadRenderer.Draw(this._renderTarget, Vector2.Zero, new(this._scale), 0, Color.White);
        this._quadRenderer.End();

        #region ImGui menu

        ImGui.SliderFloat("Final Texture Scale", ref this._scale, 0f, 2f);

        if (ImGui.Button("Go back to test selector")) {
            this.BaseGame.Components.Add(new BaseTestSelector());
            this.BaseGame.Components.Remove(this);
        }

        #endregion

        base.Draw(deltaTime);
    }

    public override void Dispose() {
        this._quadRenderer.Dispose();
        this._renderTarget.Dispose();
        this._whitePixel.Dispose();

        base.Dispose();
    }
}