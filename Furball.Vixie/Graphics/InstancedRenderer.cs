using System.Drawing;
using System.Numerics;
using Furball.Vixie.Gl;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;
using Shader=Furball.Vixie.Gl.Shader;
using Texture=Furball.Vixie.Gl.Texture;
using UniformType=Furball.Vixie.Gl.UniformType;

namespace Furball.Vixie.Graphics {
    public class InstancedRenderer {
        private GL gl;

        private VertexArrayObject _vertexArray;
        private BufferObject      _vertexBuffer;
        private BufferObject      _indexBuffer;

        private Shader _shader;

        public InstancedRenderer() {
            this.gl = Global.Gl;

            this._vertexBuffer = new BufferObject(64, BufferTargetARB.ArrayBuffer, BufferUsageARB.StreamDraw);

            uint[] indicies = new uint[] {
                0, 1, 2,
                2, 3, 0
            };

            this._indexBuffer = BufferObject.CreateNew<uint>(indicies, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StreamDraw);

            string vertSource = ResourceHelpers.GetStringResource("ShaderCode/InstanceRenderer/InstanceRendererVertexShader.glsl");
            string fragSource = ResourceHelpers.GetStringResource("ShaderCode/InstanceRenderer/InstanceRendererPixelShader.glsl");

            this._shader =
                new Shader()
                    .AttachShader(ShaderType.VertexShader, vertSource)
                    .AttachShader(ShaderType.FragmentShader, fragSource)
                    .Link();

            VertexBufferLayout layout =
                new VertexBufferLayout()
                    .AddElement<float>(2)
                    .AddElement<float>(2);

            this._vertexArray = new VertexArrayObject();
            this._vertexArray.Bind().AddBuffer(this._vertexBuffer, layout);

            this.ChangeShader(this._shader);
        }

        public unsafe void Draw(BufferObject vertexBuffer, BufferObject indexBuffer, Shader shader) {
            vertexBuffer.Bind();
            indexBuffer.Bind();
            shader.Bind()
                //vx_WindowProjectionMatrix is a uniform provided by Vixie which can optionally be used to scale things into the window
                .SetUniform("vx_WindowProjectionMatrix", UniformType.GlMat4f, Global.GameInstance.WindowManager.ProjectionMatrix);

            this.gl.DrawElements(PrimitiveType.Triangles, indexBuffer.DataCount, DrawElementsType.UnsignedInt, null);
        }

        public void Begin() {
            this._vertexBuffer.Bind();
            this._indexBuffer.Bind();
            this._vertexArray.Bind();
            this._indexBuffer.Bind();
            this._currentShader.Bind();
        }

        public void End() {

        }

        private Shader _currentShader;

        public void ChangeShader(Shader shader) {
            this._currentShader = shader;

            this._currentShader
                .Bind()
                .SetUniform("vx_WindowProjectionMatrix", UniformType.GlMat4f, Global.GameInstance.WindowManager.ProjectionMatrix);
        }

        private float[] _verticies;

        public unsafe void Draw(Texture texture, Vector2 position, Vector2? size = null, Color? colorOverride = null) {
            _verticies = new float[] {
                /* Vertex Coordinates */  position.X,                   position.Y + texture.Size.Y,  /* Texture Coordinates */  0.0f, 0.0f,  //Bottom Left corner
                /* Vertex Coordinates */  position.X + texture.Size.X,  position.Y + texture.Size.Y,  /* Texture Coordinates */  1.0f, 0.0f,  //Bottom Right corner
                /* Vertex Coordinates */  position.X + texture.Size.X,  position.Y,                   /* Texture Coordinates */  1.0f, 1.0f,  //Top Right Corner
                /* Vertex Coordinates */  position.X,                   position.Y,                   /* Texture Coordinates */  0.0f, 1.0f,  //Top Left Corner
            };

            this._vertexBuffer.SetData<float>(_verticies);
            texture.Bind();

            gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        }

        public void Clear() {
            this.gl.ClearColor(Color.FromArgb(255, (int) (.45f * 255), (int) (.55f * 255), (int) (.60f * 255)));
            this.gl.Clear(ClearBufferMask.ColorBufferBit);
        }
    }
}
