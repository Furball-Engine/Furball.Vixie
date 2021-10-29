using Furball.Vixie.Gl;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Furball.Vixie.TestApplication {
    public class TestGame : Game {
        private VertexBuffer<float> _triangleBuffer;

        public TestGame(WindowOptions options) : base(options) {

        }

        protected unsafe override void Initialize() {
            float[] verticies = new [] {
                -0.5f, -0.5f,
                 0.0f,  0.5f,
                 0.5f, -0.5f,
            };

            string[] vertexSource = new [] {
                "#version 330 core",
                "",
                "layout(location = 0) in vec4 position;",
                "",
                "void main() {",
                "    gl_Position = position;",
                "}"
            };

            string[] fragmentSource = new [] {
                "#version 330 core\n",
                "",
                "layout(location = 0) out vec4 color;",
                "",
                "void main() {",
                "    color = vec4(1.0, 0.0, 0.0, 1.0);",
                "}"
            };

            uint program = gl.CreateProgram();

            uint vertex = gl.CreateShader(GLEnum.VertexShader);
            gl.ShaderSource(vertex, 1, vertexSource, (int*) null);
            gl.CompileShader(vertex);

            uint fragment = gl.CreateShader(GLEnum.FragmentShader);
            gl.ShaderSource(fragment, 1, fragmentSource, (int*) null);
            gl.CompileShader(fragment);

            gl.AttachShader(program, vertex);
            gl.AttachShader(program, fragment);
            gl.LinkProgram(program);
            gl.ValidateProgram(program);

            gl.DeleteShader(vertex);
            gl.DeleteShader(fragment);

            gl.UseProgram(program);

            uint buffer;
            gl.GenBuffers(1, out buffer);
            gl.BindBuffer(GLEnum.ArrayBuffer, buffer);
            fixed (void* data = verticies) {
                gl.BufferData(GLEnum.ArrayBuffer, 6 * sizeof(float), data, GLEnum.StaticDraw);
            }
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, GLEnum.Float, Boolean.False, sizeof(float) * 2, 0);



            //this._triangleBuffer = new VertexBuffer<float>(BufferUsageARB.StaticDraw);
            //this._triangleBuffer.Bind();
            //this._triangleBuffer.SetData(verticies);
            ////this._triangleBuffer.AddAttribute<float>(2);
            //this._triangleBuffer.VertexAttributePointer(0, 2, VertexAttribPointerType.Float, sizeof(float) * 6, 0);


        }
        protected override void Update(double obj) {

        }

        protected override void Draw(double obj) {
            gl.Clear(ClearBufferMask.ColorBufferBit);
            //this._triangleBuffer.Bind();
            gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }

        public override void Dispose() {

        }
    }
}
