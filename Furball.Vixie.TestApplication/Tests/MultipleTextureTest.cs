using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Renderers.OpenGL;
using Furball.Vixie.Helpers;

namespace Furball.Vixie.TestApplication.Tests {
    public class MultipleTextureTest : GameComponent {
        private Texture[]       _textures = new Texture[32];
        private BatchedRenderer _batchedRenderer;

        public override void Initialize() {
            for (int i = 0; i != this._textures.Length; i++) {
                if (i % 2 == 0 && i != 0)
                    this._textures[i]  = new Texture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));
                else this._textures[i] = new Texture();
            }

            this._batchedRenderer = new BatchedRenderer();

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._batchedRenderer.Begin();

            int x = 0;
            int y = 0;

            for (int i = 0; i != this._textures.Length; i++) {
                this._batchedRenderer.Draw(this._textures[i], new Vector2(x, y), null, new Vector2(0.5f, 0.5f));
                if (i % 3 == 0 && i != 0) {
                    y += 64;
                    x =  0;
                }

                x += 256;
            }

            this._batchedRenderer.End();

            base.Draw(deltaTime);
        }
    }
}
