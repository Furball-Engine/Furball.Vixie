using System.Drawing;
using System.Numerics;
using Furball.Vixie.Helpers.Helpers;
#if USE_IMGUI
using ImGuiNET;
#endif
using Color=Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie.TestApplication.Tests; 

public class TestSourceRect : Screen {
    private Renderer _vixieRenderer;
    private Texture       _texture;

    public override void Initialize() {
        this._vixieRenderer = Game.ResourceFactory.CreateRenderer();
        this._texture = Game.ResourceFactory.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png", typeof(TestGame)));

        base.Initialize();
    }

    private float _rotation = 1f;

    public override void Draw(double deltaTime) {
        this._vixieRenderer.Begin();
        this._vixieRenderer.AllocateRotatedTexturedQuadWithSourceRect(this._texture, new Vector2(1280 / 2, 720 / 2), Vector2.One, this._rotation, Vector2.Zero, new Rectangle(this._texture.Width / 2, 0, this._texture.Width / 2, this._texture.Height / 2), Color.White);
        this._vixieRenderer.End();
        
        this._vixieRenderer.Draw();

        #region ImGui menu
        #if USE_IMGUI
        ImGui.DragFloat("Rotation", ref this._rotation, 0.01f, 0f, 8f);
        if (ImGui.Button("Go back to test selector")) {
            TestGame.Instance.ChangeScreen(new BaseTestSelector());
        }
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