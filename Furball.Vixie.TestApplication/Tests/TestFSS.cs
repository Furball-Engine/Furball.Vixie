using System.Drawing;
using System.IO;
using System.Numerics;
using FontStashSharp;
using Furball.Vixie.FontStashSharp;
using Furball.Vixie.Graphics.Renderers.OpenGL;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestFSS : GameComponent {
        private FontSystem             _system;
        private VixieFontStashRenderer _renderer;
        private BatchedRenderer        _batchedRenderer;
        private DynamicSpriteFont      _font;

        public override void Initialize() {
            this._system = new FontSystem();
            this._system.AddFont(File.ReadAllBytes("font.ttf"));
            this._font = this._system.GetFont(72);

            this._batchedRenderer = new BatchedRenderer();
            this._renderer        = new VixieFontStashRenderer(this._batchedRenderer);

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._batchedRenderer.Begin();
            this._font.DrawText(this._renderer, "i am in severe pain please make text rendering work", new Vector2(100, 100), Color.White);
            this._batchedRenderer.End();

            base.Draw(deltaTime);
        }
    }
}
