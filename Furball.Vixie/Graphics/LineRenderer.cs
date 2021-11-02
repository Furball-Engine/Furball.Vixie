using System.Drawing;
using System.Numerics;
using Furball.Vixie.Gl;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;
using Shader=Furball.Vixie.Gl.Shader;
using UniformType=Furball.Vixie.Gl.UniformType;

namespace Furball.Vixie.Graphics {
    public class LineRenderer {
        private Shader            _lineShader;
        private VertexArrayObject _vertexArray;
        private BufferObject      _vertexBuffer;

        public LineRenderer() {
            string vertexSource = ResourceHelpers.GetStringResource("ShaderCode/LineRenderer/VertexShader.glsl", true);
            string fragmentSource = ResourceHelpers.GetStringResource("ShaderCode/LineRenderer/PixelShader.glsl", true);
            string geometrySource = ResourceHelpers.GetStringResource("ShaderCode/LineRenderer/GeometryShader.glsl", true);

            this._lineShader =
                new Shader()
                    .Bind()
                    .AttachShader(ShaderType.VertexShader, vertexSource)
                    .AttachShader(ShaderType.FragmentShader, fragmentSource)
                    .AttachShader(ShaderType.GeometryShader, geometrySource)
                    .Link();

            VertexBufferLayout layout = new VertexBufferLayout();

            layout
                .AddElement<float>(4)  //Position
                .AddElement<float>(4, true); //Color

            this._vertexBuffer = new BufferObject(128, BufferTargetARB.ArrayBuffer);

            this._vertexArray  = new VertexArrayObject();
            this._vertexArray
                .Bind()
                .AddBuffer(this._vertexBuffer, layout);
        }

        public void Begin() {
            this._lineShader
                .Bind()
                .SetUniform("u_mvp",           UniformType.GlMat4f, Global.GameInstance.WindowManager.ProjectionMatrix)
                .SetUniform("u_viewport_size", UniformType.GlFloat, (float) Global.GameInstance.WindowManager.GameWindow.Size.X, (float) Global.GameInstance.WindowManager.GameWindow.Size.Y)
                .SetUniform("u_aa_radius",     UniformType.GlFloat, 6f,                                                          6f);

            this._vertexBuffer.Bind();
            this._vertexArray.Bind();
        }

        private float[] _verticies;

        public void Draw(Vector2 begin, Vector2 end, float thickness, Color color) {
            this._verticies = new float[] {
                begin.X, begin.Y, 0.0f, thickness, color.R, color.G, color.B, color.A,
                end.X,   end.Y,   0.0f, thickness, color.R, color.G, color.B, color.A,
            };

            this._vertexBuffer.SetData<float>(this._verticies);

            Global.Gl.DrawArrays(PrimitiveType.Lines, 0, 2);
        }
    }
}
