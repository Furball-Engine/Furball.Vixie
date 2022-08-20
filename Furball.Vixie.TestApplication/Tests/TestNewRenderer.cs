using System.Drawing;
using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;
using Color = Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie.TestApplication.Tests; 

public unsafe class TestNewRenderer : GameComponent {
    private Texture   _texture;
    private Texture[] _textureArr;
    private IRenderer _renderer;

    private float   _scale = 0.5f;
    private Texture _whitePixel;

    public override void Initialize() {
        this._texture =
            Texture.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));
        this._whitePixel = Texture.CreateWhitePixelTexture();

        // this._textureArr = new Texture[64];
        // for (int i = 0; i < this._textureArr.Length; i++) {
            // this._textureArr[i] =
                // Texture.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));
        // }

        this._renderer = GraphicsBackend.Current.CreateRenderer();

        this._renderer.Begin();

        this._renderer.AllocateUnrotatedTexturedQuad(this._texture, new Vector2(200), new Vector2(1));
        this._renderer.AllocateRotatedTexturedQuad(this._texture, new Vector2(300), new Vector2(1), 1);
        this._renderer.AllocateUnrotatedTexturedQuadWithSourceRect(this._texture, new Vector2(500), new Vector2(1), new Rectangle(100, 100, 200, 200));
        this._renderer.AllocateRotatedTexturedQuadWithSourceRect(this._texture, new Vector2(600), new Vector2(1), 0.5f, new Rectangle(100, 100, 200, 200));

        for (int i = 0; i < 2000; i++) {
            this._renderer.AllocateUnrotatedTexturedQuad(this._texture, new Vector2(i*9 % 1200, 0), new Vector2(0.05f));
        }
        
        MappedData data = this._renderer.Reserve(6, 15);

        long pentagonTex = this._renderer.GetTextureId(this._whitePixel);
        data.VertexPtr[0] = new Vertex {
            Position          = new Vector2(100, 0),
            Color             = Color.Red,
            TexId             = pentagonTex,
            TextureCoordinate = Vector2.Zero
        };
        data.VertexPtr[1] = new Vertex {
            Position          = new Vector2(200, 100),
            Color             = Color.Blue,
            TexId             = pentagonTex,
            TextureCoordinate = new Vector2(1, 0.5f)
        };
        data.VertexPtr[2] = new Vertex {
            Position          = new Vector2(150, 200),
            Color             = Color.Green,
            TexId             = pentagonTex,
            TextureCoordinate = new Vector2(0.75f, 1)
        };
        data.VertexPtr[3] = new Vertex {
            Position          = new Vector2(50, 200),
            Color             = Color.Orange,
            TexId             = pentagonTex,
            TextureCoordinate = new Vector2(0.25f, 1)
        };
        data.VertexPtr[4] = new Vertex {
            Position          = new Vector2(0, 100),
            Color             = Color.CornflowerBlue,
            TexId             = pentagonTex,
            TextureCoordinate = new Vector2(0, 0.5f)
        };
        data.VertexPtr[5] = new Vertex {
            Position          = new Vector2(100, 100),
            Color             = Color.Yellow,
            TexId             = pentagonTex,
            TextureCoordinate = new Vector2(0.5f, 0.5f)
        };
        data.IndexPtr[0] = (ushort)(0 + data.IndexOffset);
        data.IndexPtr[1] = (ushort)(5 + data.IndexOffset);
        data.IndexPtr[2] = (ushort)(1 + data.IndexOffset);
        
        data.IndexPtr[3] = (ushort)(1 + data.IndexOffset);
        data.IndexPtr[4] = (ushort)(5 + data.IndexOffset);
        data.IndexPtr[5] = (ushort)(2 + data.IndexOffset);

        data.IndexPtr[6] = (ushort)(2 + data.IndexOffset);
        data.IndexPtr[7] = (ushort)(5 + data.IndexOffset);
        data.IndexPtr[8] = (ushort)(3 + data.IndexOffset);

        data.IndexPtr[9]  = (ushort)(3 + data.IndexOffset);
        data.IndexPtr[10] = (ushort)(5 + data.IndexOffset);
        data.IndexPtr[11] = (ushort)(4 + data.IndexOffset);

        data.IndexPtr[12] = (ushort)(4 + data.IndexOffset);
        data.IndexPtr[13] = (ushort)(5 + data.IndexOffset);
        data.IndexPtr[14] = (ushort)(0 + data.IndexOffset);

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