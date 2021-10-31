using System;
using System.Globalization;
using System.Numerics;
using Furball.Vixie.Gl;
using Furball.Vixie.Helpers;
using Furball.Vixie.ImGuiHelpers;
using Furball.Vixie.Shaders;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Shader=Furball.Vixie.Gl.Shader;
using Texture=Furball.Vixie.Gl.Texture;
using ImGui=ImGuiNET.ImGui;
using UniformType=Furball.Vixie.Gl.UniformType;

namespace Furball.Vixie.TestApplication {
    public class TestGame : Game {
        private BufferObject<uint>             _indexBuffer;
        private BufferObject<float>            _vertexBuffer;
        private VertexArrayObject<float> _vertexArrayObject;
        private Shader                         _shader;
        private Texture                        _texture;

        private Renderer _renderer;

        private ImGuiController _imGui;

        public TestGame(WindowOptions options) : base(options) {}

        protected override unsafe void Initialize() {
            //pippidonclear0.png is 371x326 pixels
            float[] verticies = new float[] {
                /* Vertex Coordinates */  0,     (326f), /* Texture Coordinates */ 0.0f, 0.0f, //Bottom Left corner
                /* Vertex Coordinates */  371f,  (326f), /* Texture Coordinates */ 1.0f, 0.0f, //Bottom Right corner
                /* Vertex Coordinates */  371f,  (0f),   /* Texture Coordinates */ 1.0f, 1.0f, //Top Right Corner
                /* Vertex Coordinates */  0,     (0f),   /* Texture Coordinates */ 0.0f, 1.0f, //Top Left Corner
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

            //Create and initialize shader
            this._shader = new BasicTexturedShader();

            this._texture = new Texture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));
            this._texture.Bind();

            //Create renderer
            this._renderer = new Renderer();

            this._imGui = ImGuiCreator.CreateController();
        }

        protected override void Update(double deltaTime) {}

        protected override unsafe void Draw(double deltaTime) {
            this._renderer.Clear();

#if !DEBUGNOIMGUI

             this._imGui.Update((float)deltaTime);

             ImGui.Text($"Frametime: {Math.Round(1000.0f    / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                           $"Framerate: {Math.Round(ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)}"
             );

             this._imGui.Render();

#endif

            this._shader
                .Bind().
                SetUniform("u_Translation", UniformType.GlMat4f, Matrix4x4.CreateTranslation(new Vector3(100, 100, 0)));

            this._renderer.Draw(this._vertexBuffer, this._indexBuffer, this._shader);

            this._shader
                .Bind().
                SetUniform("u_Translation", UniformType.GlMat4f, Matrix4x4.CreateTranslation(new Vector3(400, 100, 0)));

            this._renderer.Draw(this._vertexBuffer, this._indexBuffer, this._shader);
        }

        public override void Dispose() {
            this._imGui.Dispose();
            this._indexBuffer.Dispose();
            this._vertexBuffer.Dispose();
            this._vertexArrayObject.Dispose();
            this._shader.Dispose();
            this._texture.Dispose();
        }
    }
}
