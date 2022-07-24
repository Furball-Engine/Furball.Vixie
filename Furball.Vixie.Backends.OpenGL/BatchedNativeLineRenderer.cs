using System;
using System.Numerics;
using Furball.Vixie.Backends.OpenGL.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using Silk.NET.OpenGL.Legacy;
using ShaderType=Silk.NET.OpenGL.ShaderType;

namespace Furball.Vixie.Backends.OpenGL; 

internal class BatchedNativeLineRenderer : ILineRenderer {
    private struct LineData {
        public Vector2 Position;
        public Color   Color;
    }
        
    private readonly OpenGLBackend _backend;

    private const int BATCH_MAX = 128; //cut this in two for actual line count, as we use 2 LineData structs per line :^)
        
    private readonly ShaderGL            _program;
    private readonly BufferObjectGL      _arrayBuf;
    private readonly GL                  _gl;
    private          VertexArrayObjectGL _vao;
    private          int                 _batchedLines = 0;
    private readonly LineData[]          _lineData     = new LineData[BATCH_MAX];
        
    internal unsafe BatchedNativeLineRenderer(OpenGLBackend backend) {
        this._backend = backend;
        this._backend.CheckThread();
        this._gl = backend.GetLegacyGL();
            
        string vertex   = ResourceHelpers.GetStringResource("Shaders/BatchedNativeLineRenderer/VertexShader.glsl");
        string fragment = ResourceHelpers.GetStringResource("Shaders/BatchedNativeLineRenderer/FragmentShader.glsl");

        this._program = new ShaderGL(backend);

        this._program.AttachShader(ShaderType.VertexShader, vertex)
            .AttachShader(ShaderType.FragmentShader, fragment)
            .Link();

        this._vao = new VertexArrayObjectGL(backend);

        this._vao.Bind();

        this._arrayBuf = new BufferObjectGL(this._backend, (sizeof(LineData) * BATCH_MAX), Silk.NET.OpenGL.BufferTargetARB.ArrayBuffer, Silk.NET.OpenGL.BufferUsageARB.DynamicDraw);
        this._arrayBuf.Bind();
        this._arrayBuf.SetData<LineData>(this._lineData);
        
        VertexBufferLayoutGL layout = new VertexBufferLayoutGL();
        layout.AddElement<float>(2);
        layout.AddElement<float>(4);

        this._vao.AddBuffer(this._arrayBuf, layout);
        this._vao.Unbind();
    }

    public void Dispose() {
        this._backend.CheckThread();

        this._program.Dispose();
    }
    ~BatchedNativeLineRenderer() {
        DisposeQueue.Enqueue(this._program);
    }
    public bool IsBegun {
        get;
        set;
    }
    public unsafe void Begin() {
        this._backend.CheckThread();
        this.IsBegun = true;
            
        this._program.Bind();

        fixed (void* ptr = &this._backend.ProjectionMatrix)
            this._gl.UniformMatrix4(this._program.GetUniformLocation("u_ProjectionMatrix"), 1, false, (float*)ptr);
        this._backend.CheckError("uniform matrix 4");
        
        this._vao.Bind();

        // this._gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(LineData), (void*)0);
        // this._backend.CheckError("set vertex attrib ptr 1");
        // this._gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, (uint)sizeof(LineData), (void*)sizeof(Vector2));
        // this._backend.CheckError("set vertex attrib ptr 2");
        
        this._gl.EnableVertexAttribArray(0);
        this._gl.EnableVertexAttribArray(1);
        this._backend.CheckError("enable vertex attrib arrays");
    }

    private float               lastThickness = 0;
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

        this._program.Bind();
        this._backend.CheckError("bindshader");

        this._gl.LineWidth(this.lastThickness * this._backend.VerticalRatio);
        this._backend.CheckError("line width");

        this._arrayBuf.Bind();
        fixed(void* ptr = this._lineData)
            this._arrayBuf.SetSubData(ptr, (nuint)(sizeof(LineData) * this._batchedLines));
        this._backend.CheckError("buffer data");
        
        this._gl.DrawArrays(PrimitiveType.Lines, 0, (uint)this._batchedLines);
        this._backend.CheckError("draw arrays");

        this._program.Unbind();

        this._batchedLines = 0;
        this.lastThickness = 0;
    }
        
    public void End() {
        this._backend.CheckThread();
        this.IsBegun = false;
        this.Flush();

        // this._gl.DisableVertexAttribArray(0);
        // this._gl.DisableVertexAttribArray(1);
    }
}