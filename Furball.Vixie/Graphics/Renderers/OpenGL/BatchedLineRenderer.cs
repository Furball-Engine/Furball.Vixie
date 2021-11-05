using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Graphics.Renderers {
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BatchedLineVertex {
        public fixed float Positions[4];
        public fixed float Color[4];
    }

    public class BatchedLineRenderer : IDisposable, ILineRenderer {
        public int MaxLines { get; private set; }
        public int MaxVerticies { get; private set; }
        private readonly GL gl;

        private readonly VertexArrayObject _vertexArray;
        private readonly BufferObject      _vertexBuffer;
        private readonly Shader            _lineShader;

        private readonly BatchedLineVertex[] _localVertexBuffer;

        public unsafe BatchedLineRenderer(int capacity = 8192) {
            this.gl = Global.Gl;

            this.MaxLines     = capacity;
            this.MaxVerticies = capacity * 32;

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

        public unsafe void Begin() {
            fixed (BatchedLineVertex* data = this._localVertexBuffer)
                this._vertexPointer = data;

            //Bind the Shader and set the necessary uniforms
            this._lineShader
                .LockingBind()
                .SetUniform("u_mvp",           UniformType.GlMat4f, Global.GameInstance.WindowManager.ProjectionMatrix)
                .SetUniform("u_viewport_size", UniformType.GlFloat, (float) Global.GameInstance.WindowManager.GameWindow.Size.X, (float) Global.GameInstance.WindowManager.GameWindow.Size.Y)
                .SetUniform("u_aa_radius",     UniformType.GlFloat, 6f,                                                          6f);

            //Bind the Buffer and Array
            this._vertexBuffer.LockingBind();
            this._vertexArray.LockingBind();
        }

        /// <summary>
        /// Draws a Line
        /// </summary>
        /// <param name="begin">Starting Point</param>
        /// <param name="end">End Point</param>
        /// <param name="thickness">Thickness of the Line</param>
        /// <param name="color">Color of the Line</param>
        public unsafe void Draw(Vector2 begin, Vector2 end, float thickness, Color color) {
            if (this._processedVerticies >= this.MaxVerticies) {
                this.End();
                this.Begin();
            }

            this._vertexPointer->Positions[0] = begin.X;
            this._vertexPointer->Positions[1] = begin.Y;
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

            this._vertexBufferIndex  += 32;
            this._processedVerticies += 2;
        }

        public unsafe void End() {
            nuint size = (nuint)this._vertexBufferIndex * 4;

            fixed (void* data = this._localVertexBuffer) {
                this._vertexBuffer
                    .SetSubData(data, size);
            }

            this.gl.DrawArrays(PrimitiveType.Lines, 0, (uint) (this._processedVerticies));

            this._processedVerticies = 0;
            this._vertexBufferIndex = 0;

            this._lineShader.Unlock();
            this._vertexBuffer.Unlock();
            this._vertexArray.Unlock();
        }

        public void Dispose() {
            try {
                //Unlock Shaders and other things
                if (this._lineShader.Locked)
                    this._lineShader.Unlock();
                if (this._vertexBuffer.Locked)
                    this._vertexBuffer.Unlock();
                if (this._vertexArray.Locked)
                    this._vertexArray.Unlock();

                this._vertexArray.Dispose();
                this._lineShader.Dispose();
                this._vertexBuffer.Dispose();
            }
            catch {

            }
        }
    }
}
