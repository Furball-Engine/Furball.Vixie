using Furball.Vixie.Gl;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Furball.Vixie.TestApplication {
    public class TestGame : Game {
        private VertexBuffer<float> _triangleBuffer;

        public TestGame(WindowOptions options) : base(options) {

        }

        protected override void Initialize() {
            float[] verticies = new [] {
                -0.5f, -0.5f,
                 0.0f,  0.5f,
                 0.5f, -0.5f,
            };

            this._triangleBuffer = new VertexBuffer<float>(BufferUsageARB.StaticDraw);
            this._triangleBuffer.Bind();
            this._triangleBuffer.SetData(verticies);
        }
        protected override void Update(double obj) {

        }

        protected override void Draw(double obj) {

        }

        public override void Dispose() {

        }
    }
}
