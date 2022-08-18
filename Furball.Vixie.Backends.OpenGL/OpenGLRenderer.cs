using System;
using Furball.Vixie.Backends.OpenGL.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Backends.OpenGL; 

internal unsafe class OpenGLRenderer : IRenderer {
    private readonly IGLBasedBackend _backend;
    private readonly GL              _gl;

    private RamBufferMapper _vtxMapper;
    private RamBufferMapper _idxMapper;

    private readonly VertexArrayObjectGL _vao;

    private readonly ShaderGL _shader;

    private readonly BufferObjectGL _vtxBuffer;//TODO: multi buffer
    private readonly BufferObjectGL _idxBuffer; //TODO: multi buffer
    
    public OpenGLRenderer(IGLBasedBackend backend) {
        this._backend = backend;
        this._gl      = backend.GetModernGL();
        
        this._vtxMapper = new RamBufferMapper((nuint)(sizeof(Vertex) * 1024));
        this._idxMapper = new RamBufferMapper(sizeof(ushort) * 1024);

        this._shader = new ShaderGL(backend);
        this._shader.AttachShader(ShaderType.VertexShader, ""); //TODO
        this._shader.AttachShader(ShaderType.FragmentShader, ""); //TODO
        this._shader.Link();

        this._vtxBuffer = new BufferObjectGL(backend, (int)this._vtxMapper.SizeInBytes, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
        this._idxBuffer = new BufferObjectGL(backend, (int)this._idxMapper.SizeInBytes, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.DynamicDraw);
        
        //TODO: make VAOs optional (for pre GL3.0)
        this._vao = new VertexArrayObjectGL(backend);

        this._vao.Bind();

        VertexBufferLayoutGL layout = new();
        layout.AddElement<float>(2);  //Position
        layout.AddElement<float>(2);  //Texture Coordinate
        layout.AddElement<float>(4);  //Color
        layout.AddElement<int>(1);    //Texture id

        this._vao.AddBuffer(this._vtxBuffer, layout);

        this._vao.Unbind();
    }
    
    public override void Begin() {
        this._vtxMapper.Reset();
        this._idxMapper.Reset();
    }
    
    public override void End() {
        this._vtxBuffer.SetSubData(this._vtxMapper.Handle, this._vtxMapper.ReservedBytes);
        this._idxBuffer.SetSubData(this._idxMapper.Handle, this._idxMapper.ReservedBytes);
    }

    public override MappedData Reserve(ushort vertexCount, uint indexCount) {
        void* vertex = this._vtxMapper.Reserve((nuint)(vertexCount * sizeof(Vertex)));
        void* index  = this._idxMapper.Reserve(indexCount * sizeof(ushort));

        if (vertex == null || index == null) {
            //TODO: handle this by uploading the data into a GPU buffer, then calling RamBufferMapper.Reset
        }

        return new MappedData((Vertex*)vertex, (ushort*)index, vertexCount, indexCount);
    }
    
    public override void Draw() {
        this._vao.Bind();

        this._shader.Bind();
        
        this._vtxBuffer.Bind();
        this._idxBuffer.Bind();
        
        this._gl.DrawElements(PrimitiveType.Triangles, (uint)(this._idxMapper.ReservedBytes / sizeof(ushort)), DrawElementsType.UnsignedShort, null);

        this._idxBuffer.Unbind();
        this._vtxBuffer.Unbind();
        
        this._vao.Unbind();
    }
    
    protected override void DisposeInternal() {
        this._vtxMapper.Dispose();
        this._idxMapper.Dispose();
    }
}