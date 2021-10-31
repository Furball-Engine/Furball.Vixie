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

                /* Vertex Coordinates */  100f,  (100f), /* Texture Coordinates */ 0.0f, 0.0f, //Bottom Left corner
                /* Vertex Coordinates */  200f,  (100f), /* Texture Coordinates */ 1.0f, 0.0f, //Bottom Right corner
                /* Vertex Coordinates */  200f,  (200f), /* Texture Coordinates */ 1.0f, 1.0f, //Top Right Corner
                /* Vertex Coordinates */  100f,  (200f), /* Texture Coordinates */ 0.0f, 1.0f, //Top Left Corner
            };

            //Indicies, basically what order to draw both triangles in
            //because its 2 triangles OpenGL knows to take 3 indicies per triangle
            //so the first triangle will take index 0 1 and 2 from vertecies
            //and the 2nd triangle will take index 2 3 and 0, which together forms a quad
            uint[] indicies = new uint[] {
                //Triangle 1 from bottom left, to bottom right, to top right corner
                0, 1, 2,
                //Triangle 2 from top right, to top left, to bottom left corner
                2, 3, 0
            };

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

            //Used for putting everything into a Coordinate space that we can work with
            Matrix4x4 projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, 1280, 0, 720, 0f, 1f);;


            //Used for simulating a camera
            Matrix4x4 viewMatrix = Matrix4x4.CreateTranslation(new Vector3(0, 0, 0));
            //Used for putting the model/object into a certain position
            Matrix4x4 modelMatrix = Matrix4x4.CreateTranslation(new Vector3(0, 0.5f, 0));

            Matrix4x4 mvp = projectionMatrix * viewMatrix * modelMatrix;

            //Create and initialize shader
            this._shader = new BasicTexturedShader();
            this._shader.SetUniform("u_ProjectionMatrix", UniformType.GlMat4f, mvp);
            //this._shader.SetUniform("u_WindowSize", UniformType.GlFloat, 1280f, 720f);

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
