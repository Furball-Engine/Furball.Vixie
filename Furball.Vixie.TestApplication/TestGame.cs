using System;
using Furball.Vixie.Gl;
using Furball.Vixie.Helpers;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Shader=Furball.Vixie.Gl.Shader;

namespace Furball.Vixie.TestApplication {
    public class TestGame : Game {
        private BufferObject<uint>             _indexBuffer;
        private BufferObject<float>            _vertexBuffer;
        private VertexArrayObject<float, uint> _vertexArrayObject;
        private Shader                         _shader;

        public TestGame(WindowOptions options) : base(options) {

        }

        protected override unsafe void Initialize() {
            gl.DebugMessageCallback(this.Callback, null);

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

            string vertexSource = EmbeddedResourceHelpers.GetStringResource("Shaders/BasicVertexShader.glsl");
            string fragmentSource = EmbeddedResourceHelpers.GetStringResource("Shaders/BasicPixelShader.glsl");

            this._vertexBuffer      = new BufferObject<float>(verticies, BufferTargetARB.ArrayBuffer);
            this._indexBuffer       = new BufferObject<uint>(indicies, BufferTargetARB.ElementArrayBuffer);
            this._vertexArrayObject = new VertexArrayObject<float, uint>(this._vertexBuffer, this._indexBuffer);

            this._vertexArrayObject.AddAttribute<float>(2);

            this._shader = new Shader();

            this._shader
                .AttachShader(ShaderType.VertexShader, vertexSource)
                .AttachShader(ShaderType.FragmentShader, fragmentSource)
                .Link();

        }
        
        private void Callback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userparam) {
            string messagea = SilkMarshal.PtrToString(message);
            
            Console.WriteLine(messagea);
        }
        protected override void Update(double obj) {

        }

        protected override unsafe void Draw(double obj) {
            gl.Clear(ClearBufferMask.ColorBufferBit);

            this._vertexArrayObject.Bind();
            this._shader.Bind();

            gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        }

        public override void Dispose() {
            this._indexBuffer.Dispose();
            this._vertexBuffer.Dispose();
            this._vertexArrayObject.Dispose();
            this._shader.Dispose();
        }
    }
}
