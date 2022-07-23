using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;

namespace Furball.Vixie.TestApplication.Tests; 

public class TestFilteringMode : GameComponent {
    private Texture _pixelatedTexture;
    private Texture _smoothTexture;

    private IQuadRenderer _quadRenderer;

    public override void Initialize() {
        this._quadRenderer = GraphicsBackend.Current.CreateTextureRenderer(); 
        
        this._pixelatedTexture = Resources.CreateTexture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));
        this._pixelatedTexture.FilterType = TextureFilterType.Pixelated;
        this._smoothTexture = Resources.CreateTexture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));
        this._smoothTexture.FilterType = TextureFilterType.Smooth;
        
        base.Initialize();
    }

    public override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();
        
        this._quadRenderer.Begin();
        
        this._quadRenderer.Draw(this._pixelatedTexture, Vector2.Zero, new Vector2(2, 2), 0, Color.White);
        this._quadRenderer.Draw(this._smoothTexture, new(100, 0), new Vector2(2, 2), 0, Color.White);
        
        this._quadRenderer.End();
        
        #region ImGui menu

        if (ImGui.Button("Go back to test selector")) {
            this.BaseGame.Components.Add(new BaseTestSelector());
            this.BaseGame.Components.Remove(this);
        }

        #endregion
        
        base.Draw(deltaTime);
    }
}