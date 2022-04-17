using Silk.NET.Windowing;

namespace Furball.Vixie.TestApplication {
    public class TestGame : Game {
        public TestGame(WindowOptions options) {}

        protected override void Initialize() {
            this.Components.Add(new BaseTestSelector());

            base.Initialize();
        }
    }
}
