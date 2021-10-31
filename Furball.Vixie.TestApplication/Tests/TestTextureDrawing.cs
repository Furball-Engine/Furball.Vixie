using System;
using System.Globalization;
using System.Numerics;
using Furball.Vixie.Gl;
using Furball.Vixie.Helpers;
using Furball.Vixie.ImGuiHelpers;
using Furball.Vixie.Shaders;
using ImGuiNET;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Texture=Furball.Vixie.Gl.Texture;
using UniformType=Furball.Vixie.Gl.UniformType;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestTextureDrawing : GameComponent {
        private Renderer _instancedRenderer;

        private BufferObject<float> _vertexBuffer;
        private BufferObject<uint>  _indexBuffer;
        private BasicTexturedShader _shader;
        private Texture             _texture;

        private VertexArrayObject<float> _vertexArrayObject;

        private ImGuiController _imGuiController;

        public TestTextureDrawing(Game game) : base(game) {}

        public override void Initialize() {
            this._instancedRenderer = new Renderer();

            //pippidonclear0.png is 371x326 pixels
            float[] verticies = new float[] {
                /* Vertex Coordinates */  0,     326f, /* Texture Coordinates */ 0.0f, 0.0f, //Bottom Left corner
                /* Vertex Coordinates */  371f,  326f, /* Texture Coordinates */ 1.0f, 0.0f, //Bottom Right corner
                /* Vertex Coordinates */  371f,  0f,   /* Texture Coordinates */ 1.0f, 1.0f, //Top Right Corner
                /* Vertex Coordinates */  0,     0f,   /* Texture Coordinates */ 0.0f, 1.0f, //Top Left Corner
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

            //Initialize all the buffers and the Textured Shader
            this._vertexBuffer = new BufferObject<float>(verticies, BufferTargetARB.ArrayBuffer);
            this._indexBuffer  = new BufferObject<uint>(indicies, BufferTargetARB.ElementArrayBuffer);
            this._shader       = new BasicTexturedShader();

            VertexBufferLayout layout = new VertexBufferLayout();
            //Define the layout of the Vertex Buffer
            layout
                .AddElement<float>(2)   //Vertex Positions
                .AddElement<float>(2);  //Texture Coordinates

            this._vertexArrayObject = new VertexArrayObject<float>();
            this._vertexArrayObject.AddBuffer(this._vertexBuffer, layout);

            //Load the Texture
            this._texture = new Texture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

            this._imGuiController = ImGuiCreator.CreateController();

            base.Initialize();
        }

        public override void Draw(double deltaTime) {
            this._instancedRenderer.Clear();

            this._texture.Bind();

            this._shader
                .Bind()
                .SetUniform("u_Translation", UniformType.GlMat4f, Matrix4x4.CreateTranslation(200, 200, 0));

            this._instancedRenderer.Draw(this._vertexBuffer, this._indexBuffer, this._shader);

            #region ImGui menu

            this._imGuiController.Update((float) deltaTime);

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector(this.BaseGame));
                this.BaseGame.Components.Remove(this);
            }

            this._imGuiController.Render();

            #endregion

            base.Draw(deltaTime);
        }

        public override void Dispose() {
            this._indexBuffer.Dispose();
            this._vertexBuffer.Dispose();
            this._vertexArrayObject.Dispose();
            this._shader.Dispose();
            this._texture.Dispose();

            base.Dispose();

            base.Dispose();
        }
    }
}
