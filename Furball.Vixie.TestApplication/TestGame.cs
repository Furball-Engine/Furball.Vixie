using System;
using Furball.Vixie.Gl;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Boolean = Silk.NET.OpenGL.Boolean;
using Color = System.Drawing.Color;

namespace Furball.Vixie.TestApplication {
    public class TestGame : Game {
        private VertexBuffer<float> _triangleBuffer;

        public TestGame(WindowOptions options) : base(options) {

        }

        public uint Vao;

        protected unsafe override void Initialize() {
            this.gl.DebugMessageCallback(this.Callback, null);
            
            Vao = gl.GenVertexArray();
            gl.BindVertexArray(Vao);

            float[] verticies = new [] {
                -0.5f, -0.5f,
                 0.0f,  0.5f,
                 0.5f, -0.5f,
            };

            string[] vertexSource = new [] {
                "#version 330 core\n",
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
            
            //Checking the shader for compilation errors.
            string infoLog = gl.GetShaderInfoLog(vertex);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                Console.WriteLine($"Error compiling vertex shader {infoLog}");
            }

            uint fragment = gl.CreateShader(GLEnum.FragmentShader);
            gl.ShaderSource(fragment, 1, fragmentSource, (int*) null);
            gl.CompileShader(fragment);
            
            //Checking the shader for compilation errors.
            infoLog = gl.GetShaderInfoLog(fragment);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                Console.WriteLine($"Error compiling fragment shader {infoLog}");
            }

            gl.AttachShader(program, vertex);
            gl.AttachShader(program, fragment);
            gl.LinkProgram(program);
            gl.ValidateProgram(program);

            gl.DeleteShader(vertex);
            gl.DeleteShader(fragment);

            gl.BindVertexArray(Vao);
            gl.UseProgram(program);

            uint buffer;
            gl.GenBuffers(1, out buffer);
            gl.BindBuffer(GLEnum.ArrayBuffer, buffer);
            
            fixed (void* data = verticies) {
                gl.BufferData(GLEnum.ArrayBuffer, 6 * sizeof(float), data, GLEnum.StaticDraw);
            }
            
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, GLEnum.Float, Boolean.False, sizeof(float) * 2, 0);
            
        }
        
        private void Callback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userparam) {
            string messagea = SilkMarshal.PtrToString(message);
            
            Console.WriteLine(messagea);
        }
        protected override void Update(double obj) {

        }

        protected override void Draw(double obj) {
            gl.Clear(ClearBufferMask.ColorBufferBit);
            // this.gl.ClearColor(Color.White);
            //this._triangleBuffer.Bind();
            gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }

        public override void Dispose() {

        }
    }
}
