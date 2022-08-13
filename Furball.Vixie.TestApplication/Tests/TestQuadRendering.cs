using System.Numerics;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;
using SixLabors.ImageSharp;
using Color=Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie.TestApplication.Tests; 

public class TestQuadRendering : GameComponent {
    private Renderer _renderer;
    private Texture       _texture;

    public override void Initialize() {
        this._renderer = new Renderer();

        //Load the Texture
        this._texture = Resources.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

        base.Initialize();
    }

    /// <summary>
    /// Amount of Dons to draw on screen each frame
    /// </summary>
    private int _cirnoDons = 1024;
    private bool _scissorEnable = false;

    public override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();

        if (this._scissorEnable)
            GraphicsBackend.Current.ScissorRect = new Rectangle(100, 100, 400, 200);
            
        this._renderer.Begin();

        for (int i = 0; i != this._cirnoDons; i++) {
            this._renderer.Draw(this._texture, new Vector2(i % 1024, i % 2 == 0 ? 0 : 200), new Vector2(0.5f), 0, new Color(1f, 1f, 1f, 0.5f));
        }

        this._renderer.End();
            
        GraphicsBackend.Current.SetFullScissorRect();

        #region ImGui menu

        if (ImGui.Button("Go back to test selector")) {
            this.BaseGame.Components.Add(new BaseTestSelector());
            this.BaseGame.Components.Remove(this);
        }

        ImGui.SliderInt("Draws", ref this._cirnoDons, 0, 2048);
        ImGui.Checkbox("Scissor", ref this._scissorEnable);

        #endregion

        base.Draw(deltaTime);
    }

    public override void Dispose() {
        this._renderer.Dispose();
        this._texture.Dispose();

        base.Dispose();
    }
}