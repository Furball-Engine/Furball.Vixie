using System;
using System.Numerics;
using Furball.Vixie.Backends.OpenGL.Shared;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using Silk.NET.OpenGLES;
using GL=Silk.NET.OpenGLES.GL;

namespace Furball.Vixie.Backends.OpenGLES {
    public class LineRendererGLES30 : ILineRenderer {
        private struct LineData {
            public Vector2 Position;
            public Color   Color;
        }
        
        private readonly OpenGLESBackend _backend;

        private const int BATCH_MAX = 128; //cut this in two for actual line count, as we use 2 LineData structs per line :^)
        
        private readonly ShaderGL _program;
        private readonly uint        _arrayBuf;

        private          int        _batchedLines = 0;
        private readonly LineData[] _lineData     = new LineData[BATCH_MAX];
        
        public bool IsBegun { get; set; }
        
        internal unsafe LineRendererGLES30(OpenGLESBackend backend) {
            this._backend = backend;
            this._backend.CheckThread();
            this._gl      = backend.GetGLES();
            
            string vertex   = ResourceHelpers.GetStringResource("Shaders/LineRenderer/VertexShader30.glsl");
            string fragment = ResourceHelpers.GetStringResource("Shaders/LineRenderer/FragmentShader30.glsl");

            this._program = new ShaderGL(backend);

            this._program.AttachShader(ShaderType.VertexShader, vertex)
                         .AttachShader(ShaderType.FragmentShader, fragment)
                         .Link();

            this._arrayBuf = this._gl.GenBuffer();
            this._gl.BindBuffer(BufferTargetARB.ArrayBuffer, this._arrayBuf);
            //Fill the buffer with empty
            this._gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(sizeof(LineData) * BATCH_MAX), null, BufferUsageARB.DynamicDraw);
            
            this._gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        }

        public void Dispose() {
            this._backend.CheckThread();

            this._program.Dispose();
        }
        ~LineRendererGLES30() {
            DisposeQueue.Enqueue(this._program);
        }
        
        public unsafe void Begin() {
            this._backend.CheckThread();
            this.IsBegun = true;
            
            this._program.Bind();

            fixed (void* ptr = &this._backend.ProjectionMatrix)
                this._gl.UniformMatrix4(this._program.GetUniformLocation("u_ProjectionMatrix"), 1, false, (float*)ptr);
            this._backend.CheckError("uniform matrix 4");
            
            this._gl.BindBuffer(GLEnum.ArrayBuffer, this._arrayBuf);
            
            this._gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(LineData), (void*)0);
            this._gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, (uint)sizeof(LineData), (void*)sizeof(Vector2));
            
            this._gl.EnableVertexAttribArray(0);
            this._gl.EnableVertexAttribArray(1);
            
            this._gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            
            this._program.Unbind();
        }

        private          float       lastThickness = 0;
        private readonly GL          _gl;
        public void Draw(Vector2 begin, Vector2 end, float thickness, Color color) {
            this._backend.CheckThread();
            if (!this.IsBegun) throw new Exception("LineRenderer is not begun!!");

            if (thickness == 0 || color.A == 0) return;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (thickness != this.lastThickness || this._batchedLines == BATCH_MAX) {
                this.Flush();
                this.lastThickness = thickness;
            }

            this._lineData[this._batchedLines].Position     = begin;
            this._lineData[this._batchedLines + 1].Position = end;
            this._lineData[this._batchedLines].Color        = color;
            this._lineData[this._batchedLines + 1].Color    = color;

            this._batchedLines += 2;
        }

        private unsafe void Flush() {
            this._backend.CheckThread();
            if (this._batchedLines == 0 || this.lastThickness == 0) return;

            //Bind program
            this._program.Bind();
            //Bind the buffer with our batched lines
            this._gl.BindBuffer(GLEnum.ArrayBuffer, this._arrayBuf);
            
            //Buffer the data
            this._gl.BufferSubData<LineData>(GLEnum.ArrayBuffer, 0, (nuint)(this._batchedLines * sizeof(LineData)), this._lineData);

            this._gl.LineWidth(this.lastThickness);
            this._gl.DrawArrays(PrimitiveType.Lines, 0, (uint)this._batchedLines);

            //Unbind buffer
            this._gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            //Unbind program
            this._program.Unbind();

            this._batchedLines = 0;
            this.lastThickness = 0;
        }
        
        public void End() {
            this._backend.CheckThread();
            this.IsBegun = false;
            this.Flush();

            this._program.Bind();
            this._gl.BindBuffer(GLEnum.ArrayBuffer, this._arrayBuf);

            this._gl.DisableVertexAttribArray(0);
            this._gl.DisableVertexAttribArray(1);
            
            this._gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            this._program.Unbind();
        }
    }
}
