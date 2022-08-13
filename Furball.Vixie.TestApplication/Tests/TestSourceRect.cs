using System;
using System.Drawing;
using System.Numerics;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;
using Color=Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie.TestApplication.Tests; 

public class TestSourceRect : GameComponent {
    private Renderer _renderer;
    private Texture       _whiteVixieTexture;

    public override void Initialize() {
        this._renderer = new Renderer();
        this._whiteVixieTexture = Resources.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

        base.Initialize();
    }

    private float _rotation = 1f;

    public override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();

        this._renderer.Begin();
        this._renderer.Draw(this._whiteVixieTexture, new Vector2(1280 / 2, 720 / 2), Vector2.One, this._rotation, Color.White, new Rectangle(371 / 2, 0, 371 / 2, 326 / 2));
        this._renderer.End();

        #region ImGui menu

        ImGui.SliderFloat("Rotation", ref this._rotation, 0f, (float)(Math.PI * 2f));

        if (ImGui.Button("Go back to test selector")) {
            this.BaseGame.Components.Add(new BaseTestSelector());
            this.BaseGame.Components.Remove(this);
        }

        #endregion

        base.Draw(deltaTime);
    }

    public override void Dispose() {
        this._renderer.Dispose();
        this._whiteVixieTexture.Dispose();

        base.Dispose();
    }
}