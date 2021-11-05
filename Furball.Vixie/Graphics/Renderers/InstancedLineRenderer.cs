using System;
using System.Drawing;
using System.Numerics;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Graphics.Renderers {
    /// <summary>
    /// Line Renderer which draws in an Instanced fashion.
    /// </summary>
    public class InstancedLineRenderer : IDisposable {
        /// <summary>
        /// OpenGL API, used to shorten code
        /// </summary>
        private GL gl;
        /// <summary>
        /// The Shader for Drawing Lines. Vertex, Fragment & Geometry Shaders
        /// </summary>
        private Shader            _lineShader;
        /// <summary>
        /// Vertex Array which holds the layout of the Vertex Buffer
        /// </summary>
        private VertexArrayObject _vertexArray;
        /// <summary>
        /// Vertex Buffer which holds the verticies
        /// </summary>
        private BufferObject      _vertexBuffer;
        /// <summary>
        /// Line Renderer which draws in an Instanced fashion.
        /// </summary>
        public InstancedLineRenderer() {
            this.gl = Global.Gl;
            //Load Shader Source
            string vertexSource = ResourceHelpers.GetStringResource("ShaderCode/LineRenderer/VertexShader.glsl", true);
            string fragmentSource = ResourceHelpers.GetStringResource("ShaderCode/LineRenderer/PixelShader.glsl", true);
            string geometrySource = ResourceHelpers.GetStringResource("ShaderCode/LineRenderer/GeometryShader.glsl", true);

            //Create, Bind, Attach, Compile and Link the Vertex Fragment and Geometry Shaders
            this._lineShader =
                new Shader()
                    .Bind()
                    .AttachShader(ShaderType.VertexShader, vertexSource)
                    .AttachShader(ShaderType.FragmentShader, fragmentSource)
                    .AttachShader(ShaderType.GeometryShader, geometrySource)
                    .Link();

            //Define Layout of the Vertex Buffer
            VertexBufferLayout layout =
                new VertexBufferLayout()
                    .AddElement<float>(4)                  //Position
                    .AddElement<float>(4, true); //Color

            this._vertexBuffer = new BufferObject(128, BufferTargetARB.ArrayBuffer);

            this._vertexArray  = new VertexArrayObject();
            //Add the layout to the Vertex Array
            this._vertexArray
                .Bind()
                .AddBuffer(this._vertexBuffer, layout);
        }

        ~InstancedLineRenderer() {
            this.Dispose();
        }

        /// <summary>
        /// Initializes the InstancedLineRenderer, do this before drawing
        /// </summary>
        public void Begin() {
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
        /// Temporary Vertex Buffer, placed here to not redefine it every time
        /// </summary>
        private float[] _verticies;
        /// <summary>
        /// Draws a Line
        /// </summary>
        /// <param name="begin">Starting Point</param>
        /// <param name="end">End Point</param>
        /// <param name="thickness">Thickness of the Line</param>
        /// <param name="color">Color of the Line</param>
        public void Draw(Vector2 begin, Vector2 end, float thickness, Color color) {
            //Define the Verticies
            this._verticies = new float[] {
                begin.X, begin.Y, 0.0f, thickness, color.R, color.G, color.B, color.A,
                end.X,   end.Y,   0.0f, thickness, color.R, color.G, color.B, color.A,
            };
            //Upload to GPU
            this._vertexBuffer.SetData<float>(this._verticies);
            //Draw
            this.gl.DrawArrays(PrimitiveType.Lines, 0, 2);
        }
        /// <summary>
        /// Ends the Instanced Renderer, unlocking all buffers
        /// </summary>
        public void End() {
            this._lineShader.Unlock();
            this._vertexBuffer.Unlock();
            this._vertexArray.Unlock();
        }

        public void Dispose() {
            //Unlock Shaders and other things
            if (this._lineShader.Locked)
                this._lineShader.Unlock();
            if (this._vertexBuffer.Locked)
                this._vertexBuffer.Unlock();
            if (this._vertexArray.Locked)
                this._vertexArray.Unlock();

            this._lineShader.Dispose();
            this._vertexBuffer.Dispose();
            this._vertexArray.Dispose();
        }
    }
}
