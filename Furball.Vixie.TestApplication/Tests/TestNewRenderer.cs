using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;

namespace Furball.Vixie.TestApplication.Tests; 

public unsafe class TestNewRenderer : GameComponent {
    private Texture  _texture;
    private IRenderer _renderer;

    private float _scale = 0.5f;
        
    public override void Initialize() {
        this._texture =
            Texture.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));
        
        this._renderer = GraphicsBackend.Current.CreateRenderer();

        this._renderer.Begin();

        MappedData mappedData = this._renderer.Reserve(4, 6);
        mappedData.VertexPtr[0] = new Vertex {
            Position          = new Vector2(10, 10),
            Color             = Color.White,
            TexId             = this._renderer.GetTextureId(this._texture),
            TextureCoordinate = new Vector2(0, 0)
        };
        mappedData.VertexPtr[1] = new Vertex {
            Position          = new Vector2(100, 10),
            Color             = Color.White,
            TexId             = this._renderer.GetTextureId(this._texture),
            TextureCoordinate = new Vector2(1, 0)
        };
        mappedData.VertexPtr[2] = new Vertex {
            Position          = new Vector2(100, 100),
            Color             = Color.White,
            TexId             = this._renderer.GetTextureId(this._texture),
            TextureCoordinate = new Vector2(1, 1)
        };
        mappedData.VertexPtr[3] = new Vertex {
            Position          = new Vector2(10, 100),
            Color             = Color.White,
            TexId             = this._renderer.GetTextureId(this._texture),
            TextureCoordinate = new Vector2(0, 1)
        };
        mappedData.IndexPtr[0] = 3;
        mappedData.IndexPtr[1] = 2;
        mappedData.IndexPtr[2] = 0;
        mappedData.IndexPtr[3] = 1;
        mappedData.IndexPtr[4] = 2;
        mappedData.IndexPtr[5] = 0;
        
        this._renderer.End();
        
        base.Initialize();
    }

    public override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();

        this._renderer.Draw();

        #region ImGui menu

        ImGui.SliderFloat("Texture Scale", ref this._scale, 0f, 20f);

        if (ImGui.Button("Go back to test selector")) {
            this.BaseGame.Components.Add(new BaseTestSelector());
            this.BaseGame.Components.Remove(this);
        }

        #endregion

        base.Draw(deltaTime);
    }

    public override void Dispose() {
        this._texture.Dispose();

        this._renderer.Dispose();

        base.Dispose();
    }
}