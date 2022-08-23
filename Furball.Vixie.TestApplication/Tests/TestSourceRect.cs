using System;
using System.Drawing;
using System.Numerics;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;
using Color=Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie.TestApplication.Tests; 

public class TestSourceRect : GameComponent {
    private Renderer _renderer;
    private Texture       _texture;

    public override void Initialize() {
        this._renderer = GraphicsBackend.Current.CreateRenderer();
        this._texture = Texture.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

        base.Initialize();
    }

    private float _rotation = 1f;

    public override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();

        this._renderer.Begin();
        this._renderer.AllocateRotatedTexturedQuadWithSourceRect(this._texture, new Vector2(1280 / 2, 720 / 2), Vector2.One, this._rotation, Vector2.Zero, new Rectangle(this._texture.Width / 2, 0, this._texture.Width / 2, this._texture.Height / 2), Color.White);
        this._renderer.End();
        
        this._renderer.Draw();

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
        this._texture.Dispose();

        base.Dispose();
    }
}