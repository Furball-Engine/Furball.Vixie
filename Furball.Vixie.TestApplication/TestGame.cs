using Silk.NET.Windowing;

namespace Furball.Vixie.TestApplication {
    public class TestGame : Game {
        public TestGame(WindowOptions options) : base(options) {}

        protected override unsafe void Initialize() {
            this.Components.Add(new BaseTestSelector(this));

            base.Initialize();
        }
    }
}
