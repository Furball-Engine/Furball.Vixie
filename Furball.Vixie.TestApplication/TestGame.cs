using System.Numerics;
using Furball.Vixie.Gl;
using Furball.Vixie.Helpers;
using Furball.Vixie.Shaders;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Shader=Furball.Vixie.Gl.Shader;
using Texture=Furball.Vixie.Gl.Texture;
using UniformType=Furball.Vixie.Gl.UniformType;

namespace Furball.Vixie.TestApplication {
    public class TestGame : Game {
        private BufferObject<uint>             _indexBuffer;
        private BufferObject<float>            _vertexBuffer;
        private VertexArrayObject<float> _vertexArrayObject;
        private Shader                         _shader;
        private Texture                        _texture;

        private Renderer _renderer;

        public TestGame(WindowOptions options) : base(options) {

        }

        protected override unsafe void Initialize() {
            float[] verticies = new float[] {
                /* Vertex Coordinates */ -0.5f, -0.5f, /* Texture Coordinates */ 0.0f, 0.0f,
                /* Vertex Coordinates */  0.5f, -0.5f, /* Texture Coordinates */ 1.0f, 0.0f,
                /* Vertex Coordinates */  0.5f,  0.5f, /* Texture Coordinates */ 1.0f, 1.0f,
                /* Vertex Coordinates */ -0.5f,  0.5f, /* Texture Coordinates */ 0.0f, 1.0f,
            };

            //Indicies, basically what order to draw both triangles in
            //because its 2 triangles OpenGL knows to take 3 indicies per triangle
            //so the first triangle will take index 0 1 and 2 from vertecies
            //and the 2nd triangle will take index 2 3 and 0, which together forms a quad
            uint[] indicies = new uint[] {
                0, 1, 2,
                2, 3, 0
            };
            //load shader sources
            string vertexSource = ResourceHelpers.GetStringResource("Shaders/BasicTexturedVertexShader.glsl", true);
            string fragmentSource = ResourceHelpers.GetStringResource("Shaders/BasicTexturedPixelShader.glsl", true);

            //prepare buffers
            this._vertexBuffer      = new BufferObject<float>(verticies, BufferTargetARB.ArrayBuffer);
            this._indexBuffer       = new BufferObject<uint>(indicies, BufferTargetARB.ElementArrayBuffer);
            this._vertexArrayObject = new VertexArrayObject<float>();

            VertexBufferLayout layout = new VertexBufferLayout();

            layout
                .AddElement<float>(2)
                .AddElement<float>(2);

            //describe layout of VAO
            this._vertexArrayObject.AddBuffer(this._vertexBuffer, layout);

            Matrix4x4 projectionMatrix = Matrix4x4.CreateOrthographic(-8f, 8f, -4.5f, 4.5f);

            //Create and initialize shader
            this._shader = new BasicTexturedShader();
            this._shader.SetUniform("u_ProjectionMatrix", UniformType.GlMat4f, projectionMatrix);

            this._texture = new Texture(ResourceHelpers.GetByteResource("Resources.pippidonclear0.png"));
            this._texture.Bind();

            //Create renderer
            this._renderer = new Renderer();
        }
        

        protected override void Update(double deltaTime) {

        }

        protected override unsafe void Draw(double deltaTime) {
            this._renderer.Clear();

            this._renderer.Draw(this._vertexBuffer, this._indexBuffer, this._shader);
        }

        public override void Dispose() {
            this._indexBuffer.Dispose();
            this._vertexBuffer.Dispose();
            this._vertexArrayObject.Dispose();
            this._shader.Dispose();
            this._texture.Dispose();
        }
    }
}
