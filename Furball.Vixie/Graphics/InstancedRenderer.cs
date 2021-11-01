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

            this._vertexBuffer = new BufferObject(16, BufferTargetARB.ArrayBuffer, BufferUsageARB.StreamDraw);

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
                    .AddElement<float>(2)
                    .AddElement<float>(4, true);

            this._vertexArray = new VertexArrayObject();
            this._vertexArray.Bind().AddBuffer(this._vertexBuffer, layout);
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
        }

        public void End() {

        }

        public unsafe void Draw(Texture texture, Vector2 position, Vector2? size = null, Color? colorOverride = null, Shader customShader = null) {
            //Initialize defaults
            colorOverride ??= Color.White;
            size          ??= texture.Size;
            customShader  ??= this._shader;

            float[] verticies = new float[] {
                /* Vertex Coordinates */  0,             size.Value.Y,  /* Texture Coordinates */  0.0f, 0.0f,  /* Color */  colorOverride.Value.R, colorOverride.Value.G, colorOverride.Value.B, colorOverride.Value.A, //Bottom Left corner
                /* Vertex Coordinates */  size.Value.X,  size.Value.Y,  /* Texture Coordinates */  1.0f, 0.0f,  /* Color */  colorOverride.Value.R, colorOverride.Value.G, colorOverride.Value.B, colorOverride.Value.A, //Bottom Right corner
                /* Vertex Coordinates */  size.Value.X,  0f,            /* Texture Coordinates */  1.0f, 1.0f,  /* Color */  colorOverride.Value.R, colorOverride.Value.G, colorOverride.Value.B, colorOverride.Value.A, //Top Right Corner
                /* Vertex Coordinates */  0,             0f,            /* Texture Coordinates */  0.0f, 1.0f,  /* Color */  colorOverride.Value.R, colorOverride.Value.G, colorOverride.Value.B, colorOverride.Value.A, //Top Left Corner
            };

            this._vertexBuffer.SetData<float>(verticies);
            texture.Bind();

            customShader
                .Bind()
                .SetUniform("vx_WindowProjectionMatrix", UniformType.GlMat4f, Global.GameInstance.WindowManager.ProjectionMatrix)
                .SetUniform("u_Translation",             UniformType.GlMat4f, Matrix4x4.CreateTranslation(position.X, position.Y, 0));

            gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        }

        public void Clear() {
            this.gl.ClearColor(Color.FromArgb(255, (int) (.45f * 255), (int) (.55f * 255), (int) (.60f * 255)));
            this.gl.Clear(ClearBufferMask.ColorBufferBit);
        }
    }
}
