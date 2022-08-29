using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
#if USE_IMGUI
using ImGuiNET;
#endif


namespace Furball.Vixie.TestApplication.Tests; 

public class TestRotation : GameComponent {
    private Renderer _renderer;
    private Texture       _whiteTexture;

    public override void Initialize() {
        this._renderer = GraphicsBackend.Current.CreateRenderer();
        this._whiteTexture =
            Texture.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

        base.Initialize();
    }

    private float _rotation = 1f;

    public override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();

        this._renderer.Begin();

        for(int i = 0; i != 360; i ++)
            this._renderer.AllocateRotatedTexturedQuad(this._whiteTexture, new Vector2(1280f / 2f, 720f / 2f), Vector2.One, (float) i * (3.1415f / 180f) + this._rotation, Vector2.Zero, Color.White);

        this._renderer.End();
        
        this._renderer.Draw();

        #region ImGui menu
        #if USE_IMGUI
        ImGui.DragFloat("Rotation", ref this._rotation, 0.01f, 0f, 8f);
        if (ImGui.Button("Go back to test selector")) {
            this.BaseGame.Components.Add(new BaseTestSelector());
            this.BaseGame.Components.Remove(this);
        }
        #endif
        #endregion

        base.Draw(deltaTime);
    }

    public override void Dispose() {
        this._renderer.Dispose();
        this._whiteTexture.Dispose();

        base.Dispose();
    }
}