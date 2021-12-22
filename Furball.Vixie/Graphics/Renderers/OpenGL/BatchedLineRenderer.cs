using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Graphics.Renderers.OpenGL {
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BatchedLineVertex {
        public fixed float Positions[4];
        public fixed float Color[4];
    }

    public class BatchedLineRenderer : IDisposable, ILineRenderer {
        /// <summary>
        /// Max Lines allowed in 1 Batch
        /// </summary>
        public int MaxLines { get; private set; }
        /// <summary>
        /// Max Vertcies allowed in 1 batch
        /// </summary>
        public int MaxVerticies { get; private set; }
        /// <summary>
        /// OpenGL API, used to Shorten Code
        /// </summary>
        private readonly GL gl;

        /// <summary>
        /// Vertex Array which stores the Vertex Buffer layout information
        /// </summary>
        private readonly VertexArrayObject _vertexArray;
        /// <summary>
        /// Vertex buffer which contains all the Batched Verticies
        /// </summary>
        private readonly BufferObject      _vertexBuffer;
        /// <summary>
        /// Shader which draws those thicc lines
        /// </summary>
        private readonly Shader            _lineShader;


        /// <summary>
        /// Local Copy of the Vertex Buffer which gets uploaded to the GPU
        /// </summary>
        private readonly BatchedLineVertex[] _localVertexBuffer;

        public bool IsBegun { get; set; }

        /// <summary>
        /// Creates a Batched Line Renderer
        /// </summary>
        /// <param name="capacity">How many Lines to allow in 1 Batch</param>
        public unsafe BatchedLineRenderer(int capacity = 8192) {
            this.gl = Global.Gl;

            //Calculate Constants
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

            //Create Vertex Buffer with the Required size
            this._vertexBuffer = new BufferObject(sizeof(BatchedLineVertex) * this.MaxVerticies, BufferTargetARB.ArrayBuffer);

            //Create the VAO
            this._vertexArray = new VertexArrayObject();

            //Add the layout to the Vertex Array
            this._vertexArray
                .Bind()
                .AddBuffer(this._vertexBuffer, layout);

            //Initialize the Local Vertex Buffer copy
            this._localVertexBuffer = new BatchedLineVertex[this.MaxVerticies];
        }

        /// <summary>
        /// At what Index are we in the Vertex Buffer
        /// </summary>
        private        int                _vertexBufferIndex  = 0;
        /// <summary>
        /// Through how many verticies have we gone
        /// </summary>
        private        int                _processedVerticies = 0;
        /// <summary>
        /// Current pointer into the Vertex Buffer
        /// </summary>
        private unsafe BatchedLineVertex* _vertexPointer;

        /// <summary>
        /// Begins the Batch
        /// </summary>
        public unsafe void Begin() {
            fixed (BatchedLineVertex* data = this._localVertexBuffer)
                this._vertexPointer = data;

            //Bind the Shader and set the necessary uniforms
            this._lineShader
                .LockingBind()
                .SetUniform("u_mvp",           UniformType.GlMat4F, Global.GameInstance.WindowManager.ProjectionMatrix)
                .SetUniform("u_viewport_size", UniformType.GlFloat, (float) Global.GameInstance.WindowManager.GameWindow.Size.X, (float) Global.GameInstance.WindowManager.GameWindow.Size.Y)
                .SetUniform("u_aa_radius",     UniformType.GlFloat, 0f,                                                          0f);

            //Bind the Buffer and Array
            this._vertexBuffer.LockingBind();
            this._vertexArray.LockingBind();

            this.IsBegun = true;
        }

        /// <summary>
        /// Draws a Line
        /// </summary>
        /// <param name="begin">Starting Point</param>
        /// <param name="end">End Point</param>
        /// <param name="thickness">Thickness of the Line</param>
        /// <param name="color">Color of the Line</param>
        public unsafe void Draw(Vector2 begin, Vector2 end, float thickness, Color color) {
            if (!IsBegun)
                throw new Exception("Cannot call Draw before Calling Begin in BatchedLineRenderer!");

            //If we have gone over the allowed number of Verticies in 1 Batch, draw whats already there and restat
            if (this._processedVerticies >= this.MaxVerticies) {
                this.End();
                this.Begin();
            }

            //Vertex 1, Begin Point
            this._vertexPointer->Positions[0] = begin.X;
            this._vertexPointer->Positions[1] = begin.Y;
            this._vertexPointer->Positions[2] = 0;
            this._vertexPointer->Positions[3] = thickness;
            this._vertexPointer->Color[0]     = color.R;
            this._vertexPointer->Color[1]     = color.G;
            this._vertexPointer->Color[2]     = color.B;
            this._vertexPointer->Color[3]     = color.A;
            this._vertexPointer++;

            //Vertex 2, End Point
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
        /// <summary>
        /// Ends the Batch and draws everything to the Screen
        /// </summary>
        public unsafe void End() {
            //Calculate how much to upload
            nuint size = (nuint)this._vertexBufferIndex * 4;

            //Upload
            fixed (void* data = this._localVertexBuffer) {
                this._vertexBuffer
                    .SetSubData(data, size);
            }

            //Draw
            this.gl.DrawArrays(PrimitiveType.Lines, 0, (uint) (this._processedVerticies));

            //Reset Counts
            this._processedVerticies = 0;
            this._vertexBufferIndex = 0;

            //Unlock all
            this._lineShader.Unlock();
            this._vertexBuffer.Unlock();
            this._vertexArray.Unlock();

            this.IsBegun = false;
        }
        /// <summary>
        /// Cleans up after itself
        /// </summary>
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
