using Furball.Vixie.TestApplication.Tests;
using Silk.NET.Windowing;

namespace Furball.Vixie.TestApplication {
    public class TestGame : Game {
        public TestGame(WindowOptions options) {}

        protected override unsafe void Initialize() {
            this.Components.Add(new TestTextureRenderTargets());

            base.Initialize();
        }
    }
}
