using Furball.Vixie.Gl;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Shader=Furball.Vixie.Gl.Shader;
using UniformType=Furball.Vixie.Gl.UniformType;

namespace Furball.Vixie.TestApplication {
    public class TestGame : Game {
        private BufferObject<uint>             _indexBuffer;
        private BufferObject<float>            _vertexBuffer;
        private VertexArrayObject<float, uint> _vertexArrayObject;
        private Shader                         _shader;

        private Renderer _renderer;

        public TestGame(WindowOptions options) : base(options) {

        }

        protected override unsafe void Initialize() {


            float[] verticies = new float[] {
                 0.5f,  0.5f, //0
                 0.5f, -0.5f, //1
                -0.5f, -0.5f, //2
                -0.5f,  0.5f, //3
            };

            uint[] indicies = new uint[] {
                0, 1, 2,
                2, 3, 0
            };

            string vertexSource = ResourceHelpers.GetStringResource("Shaders/BasicVertexShader.glsl");
            string fragmentSource = ResourceHelpers.GetStringResource("Shaders/BasicPixelShader.glsl");

            this._vertexBuffer      = new BufferObject<float>(verticies, BufferTargetARB.ArrayBuffer);
            this._indexBuffer       = new BufferObject<uint>(indicies, BufferTargetARB.ElementArrayBuffer);
            this._vertexArrayObject = new VertexArrayObject<float, uint>(this._vertexBuffer, this._indexBuffer);

            this._vertexArrayObject.AddAttribute<float>(2);

            this._shader = new Shader();

            this._shader
                .AttachShader(ShaderType.VertexShader, vertexSource)
                .AttachShader(ShaderType.FragmentShader, fragmentSource)
                .Link()
                .Bind()
                .SetUniform("u_Color", UniformType.GlFloat, 0.2f, 0.1f, 0.2f, 1.0f);

            this._renderer = new Renderer();
        }
        

        protected override void Update(double obj) {

        }

        protected override unsafe void Draw(double obj) {
            this._renderer.Clear();

            this._renderer.Draw(this._vertexBuffer, this._indexBuffer, this._shader);
        }

        public override void Dispose() {
            this._indexBuffer.Dispose();
            this._vertexBuffer.Dispose();
            this._vertexArrayObject.Dispose();
            this._shader.Dispose();
        }
    }
}
