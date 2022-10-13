using System;
using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
#if USE_IMGUI
using ImGuiNET;
#endif

namespace Furball.Vixie.TestApplication.Tests; 

public class TestMultipleTextures : Screen {
    private Texture[]     _textures = new Texture[32];
    private Renderer _vixieRenderer;

    private float _scale = 0.5f;
        
    public override void Initialize() {
        for (int i = 0; i != this._textures.Length; i++) {
            if (i % 2 == 0 && i != 0)
                this._textures[i]  = Texture.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png", typeof(TestGame)));
            else this._textures[i] = Texture.CreateTextureFromByteArray(ResourceHelpers.GetByteResource("Resources/test.qoi", typeof(TestGame)));
        }

        this._vixieRenderer = new Renderer();

        base.Initialize();
    }

    public override void Draw(double deltaTime) {
        GraphicsBackend.Current.Clear();

        this._vixieRenderer.Begin();

        int x = 0;
        int y = 0;

        for (int i = 0; i != this._textures.Length; i++) {
            this._vixieRenderer.AllocateUnrotatedTexturedQuad(this._textures[i], new Vector2(x, y), new Vector2(this._scale), i % 2 == 0 ? Color.White : new(1f, 1f, 1f, 0.7f), i % 4 == 0 ? TextureFlip.FlipHorizontal : TextureFlip.FlipVertical);
            
            if (i % 3 == 0 && i != 0) {
                y += 64;
                x =  0;
            }

            x += 256;
        }

        this._vixieRenderer.End();
        
        this._vixieRenderer.Draw();

        #region ImGui menu
        #if USE_IMGUI
        ImGui.SliderFloat("Texture Scale", ref this._scale, 0f, 20f);

        if (ImGui.Button("Go back to test selector")) {
            TestGame.Instance.ChangeScreen(new BaseTestSelector());
        }
        #endif
        #endregion

        base.Draw(deltaTime);
    }

    public override void Dispose() {
        for(int i = 0; i != this._textures.Length; i++)
            this._textures[i].Dispose();

        this._vixieRenderer.Dispose();

        base.Dispose();
    }
}