using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers.Helpers;
#if USE_IMGUI
using ImGuiNET;
#endif


namespace Furball.Vixie.TestApplication.Tests; 

public class TestRotation : Screen {
    private Renderer _vixieRenderer;
    private Texture       _whiteTexture;

    public override void Initialize() {
        this._vixieRenderer = Game.ResourceFactory.CreateRenderer();
        this._whiteTexture =
            Game.ResourceFactory.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png", typeof(TestGame)));

        base.Initialize();
    }

    private float _rotation = 1f;

    public override void Draw(double deltaTime) {
        this._vixieRenderer.Begin();

        for(int i = 0; i != 360; i ++)
            this._vixieRenderer.AllocateRotatedTexturedQuad(this._whiteTexture, new Vector2(1280f / 2f, 720f / 2f), Vector2.One, (float) i * (3.1415f / 180f) + this._rotation, Vector2.Zero, Color.White);

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
        this._whiteTexture.Dispose();

        base.Dispose();
    }
}