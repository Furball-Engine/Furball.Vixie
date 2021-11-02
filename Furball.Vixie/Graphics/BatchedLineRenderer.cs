using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Gl;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;
using Shader=Furball.Vixie.Gl.Shader;
using UniformType=Furball.Vixie.Gl.UniformType;

namespace Furball.Vixie.Graphics {
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BatchedLineVertex {
        public fixed float Positions[4];
        public fixed float Color[4];
    }

    public class BatchedLineRenderer {
        public int MaxLines { get; private set; }
        public int MaxVerticies { get; private set; }
        private readonly GL gl;

        private readonly VertexArrayObject _vertexArray;
        private readonly BufferObject      _vertexBuffer;
        private readonly Shader            _lineShader;

        public int DrawCalls { get; private set; }
        public int Lines { get; private set; }

        private readonly BatchedLineVertex[] _localVertexBuffer;

        public unsafe BatchedLineRenderer(int capacity = 8192) {
            this.gl = Global.Gl;

            this.MaxLines     = capacity;
            this.MaxVerticies = capacity * 2;

            //Load Shader Source
            string vertexSource = ResourceHelpers.GetStringResource("ShaderCode/LineRenderer/VertexShader.glsl",     true);
            string fragmentSource = ResourceHelpers.GetStringResource("ShaderCode/LineRenderer/PixelShader.glsl",    true);
            string geometrySource = ResourceHelpers.GetStringResource("ShaderCode/LineRenderer/GeometryShader.glsl", true);

            //Create, Bind, Attach, Compile and Link the Vertex Fragment and Geometry Shaders
            this._lineShader =
                new Shader()
                    .Bind()
                    .AttachShader(ShaderType.VertexShader,   vertexSource)
                    .AttachShader(ShaderType.FragmentShader, fragmentSource)
                    .AttachShader(ShaderType.GeometryShader, geometrySource)
                    .Link();

            //Define Layout of the Vertex Buffer
            VertexBufferLayout layout =
                new VertexBufferLayout()
                    .AddElement<float>(4)                  //Position
                    .AddElement<float>(4, true);  //Color

            this._vertexBuffer = new BufferObject(sizeof(BatchedLineVertex) * this.MaxVerticies, BufferTargetARB.ArrayBuffer);

            this._vertexArray = new VertexArrayObject();
            //Add the layout to the Vertex Array
            this._vertexArray
                .Bind()
                .AddBuffer(this._vertexBuffer, layout);

            this._localVertexBuffer = new BatchedLineVertex[this.MaxVerticies];
        }

        private        int                _vertexBufferIndex  = 0;
        private        int                _processedVerticies = 0;
        private unsafe BatchedLineVertex* _vertexPointer;

        public unsafe void Begin(bool clearStats = true) {
            fixed (BatchedLineVertex* data = this._localVertexBuffer)
                this._vertexPointer = data;

            if (clearStats) {
                this.DrawCalls = 0;
                this.Lines     = 0;
            }

            //Bind the Shader and set the necessary uniforms
            this._lineShader
                .Bind()
                .SetUniform("u_mvp",           UniformType.GlMat4f, Global.GameInstance.WindowManager.ProjectionMatrix)
                .SetUniform("u_viewport_size", UniformType.GlFloat, (float) Global.GameInstance.WindowManager.GameWindow.Size.X, (float) Global.GameInstance.WindowManager.GameWindow.Size.Y)
                .SetUniform("u_aa_radius",     UniformType.GlFloat, 6f,                                                          6f);

            //Bind the Buffer and Array
            this._vertexBuffer.Bind();
            this._vertexArray.Bind();
        }

        public unsafe void Draw(Vector2 start, Vector2 end, float thickness, Color color) {
            if (this._processedVerticies >= MaxVerticies) {
                this.End();
                this.Begin(false);
            }

            this._vertexPointer->Positions[0] = start.X;
            this._vertexPointer->Positions[1] = start.Y;
            this._vertexPointer->Positions[2] = 0;
            this._vertexPointer->Positions[3] = thickness;
            this._vertexPointer->Color[0]     = color.R;
            this._vertexPointer->Color[1]     = color.G;
            this._vertexPointer->Color[2]     = color.B;
            this._vertexPointer->Color[3]     = color.A;
            this._vertexPointer++;

            this._vertexPointer->Positions[0] = end.X;
            this._vertexPointer->Positions[1] = end.Y;
            this._vertexPointer->Positions[2] = 0;
            this._vertexPointer->Positions[3] = thickness;
            this._vertexPointer->Color[0]     = color.R;
            this._vertexPointer->Color[1]     = color.G;
            this._vertexPointer->Color[2]     = color.B;
            this._vertexPointer->Color[3]     = color.A;
            this._vertexPointer++;

            this._vertexBufferIndex  += 36;
            this._processedVerticies += 2;
            this.Lines++;
        }

        public unsafe void End() {
            nuint size = (nuint)this._vertexBufferIndex;

            fixed (void* data = this._localVertexBuffer) {
                this._vertexBuffer
                    .Bind()
                    .SetSubData(data, size);
            }

            gl.DrawArrays(PrimitiveType.Lines, 0, (uint) (this._processedVerticies / 2));

            this._processedVerticies = 0;
            this._vertexBufferIndex = 0;
            this.DrawCalls++;
        }
    }
}
