using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;

namespace Furball.Vixie.TestApplication.Tests; 

public class TestFilteringMode : GameComponent {
    private Texture _pixelatedTexture;
    private Texture _smoothTexture;

    private Renderer _renderer;

    public override void Initialize() {
        this._renderer = GraphicsBackend.Current.CreateRenderer();
        
        this._pixelatedTexture = Texture.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));
        this._pixelatedTexture.FilterType = TextureFilterType.Pixelated;
        this._smoothTexture = Texture.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));
        this._smoothTexture.FilterType = TextureFilterType.Smooth;
        
        this._renderer.Begin();
        this._renderer.AllocateUnrotatedTexturedQuad(this._pixelatedTexture, Vector2.Zero, new Vector2(2), Color.White);
        this._renderer.AllocateUnrotatedTexturedQuad(this._smoothTexture, new Vector2(100, 0), new Vector2(2), Color.White);
        this._renderer.End();
        
        base.Initialize();
    }

    public override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();
        
        this._renderer.Draw();
        
        #region ImGui menu

        if (ImGui.Button("Go back to test selector")) {
            this.BaseGame.Components.Add(new BaseTestSelector());
            this.BaseGame.Components.Remove(this);
        }

        #endregion
        
        base.Draw(deltaTime);
    }
}