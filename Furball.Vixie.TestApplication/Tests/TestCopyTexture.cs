using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers.Helpers;

namespace Furball.Vixie.TestApplication.Tests; 

public class TestCopyTexture : Screen {
    private Renderer _renderer;
    private Texture  _sourceTexture;
    private Texture  _destinationTexture;

    public override void Initialize() {
        base.Initialize();

        this._renderer      = Game.ResourceFactory.CreateRenderer();
        this._sourceTexture = Game.ResourceFactory.CreateTextureFromByteArray(
            ResourceHelpers.GetByteResource("Resources/pippidonclear0.png", typeof(TestGame)));
        this._destinationTexture = Game.ResourceFactory.CreateEmptyTexture(
            (uint)this._sourceTexture.Width,
            (uint)this._sourceTexture.Height
        );

        this._sourceTexture.CopyTo(this._destinationTexture);
    }

    public override void Draw(double delta) {
        base.Draw(delta);
        
        this._renderer.Begin();
        this._renderer.AllocateUnrotatedTexturedQuad(
            this._sourceTexture,
            Vector2.Zero,
            Vector2.One,
            Color.White
        );
        this._renderer.AllocateUnrotatedTexturedQuad(
            this._destinationTexture,
            new Vector2(this._sourceTexture.Width, 0),
            Vector2.One,
            Color.White
        );
        this._renderer.End();
        this._renderer.Draw();
    }

    public override void Dispose() {
        base.Dispose();
        
        this._renderer.Dispose();
    }
}