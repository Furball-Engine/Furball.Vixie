using System;
using Furball.Vixie.Backends.OpenGL.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Backends.OpenGL; 

internal unsafe class OpenGLRenderer : IRenderer {
    private readonly OpenGLBackend _backend;
    private readonly GL            _gl;

    private RamBufferMapper _vtxMapper;
    private RamBufferMapper _idxMapper;

    private readonly VertexArrayObjectGL _vao;

    private readonly ShaderGL _shader;

    private readonly BufferObjectGL _vtxBuffer;//TODO: multi buffer
    private readonly BufferObjectGL _idxBuffer; //TODO: multi buffer
    
    public OpenGLRenderer(OpenGLBackend backend) {
        this._backend = backend;
        this._gl      = backend.GetModernGL();
        
        this._vtxMapper = new RamBufferMapper((nuint)(sizeof(Vertex) * 1024));
        this._idxMapper = new RamBufferMapper(sizeof(ushort) * 1024);

        this._shader = new ShaderGL(backend);
        this._shader.AttachShader(ShaderType.VertexShader, ResourceHelpers.GetStringResource("Shaders/VertexShader.glsl"));
        this._shader.AttachShader(ShaderType.FragmentShader, RendererShaderGenerator.GetFragment(this._backend));
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

        this._texHandles = new VixieTextureGL[this._backend.QueryMaxTextureUnits()];
    }
    
    public override void Begin() {
        this._vtxMapper.Reset();
        this._idxMapper.Reset();

        this._usedTextures = 0;
        for (int i = 0; i < this._texHandles.Length; i++) {
            this._texHandles[i] = null;
        }
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
    
    public override int GetTextureId(VixieTexture tex) {
        if (tex is not VixieTextureGL texGl)
            throw new InvalidOperationException($"You must pass a {typeof(VixieTextureGL)} into this function!");

        if (texGl.BoundId != -1)
            return texGl.BoundId;

        texGl.BoundId = this._usedTextures;
        
        this._texHandles[this._usedTextures] = texGl;

        this._usedTextures++;

        if (this._usedTextures >= this._backend.QueryMaxTextureUnits()) {
            throw new NotImplementedException("You cannot use more than 32 textures here yet!");
        }
        
        return this._usedTextures - 1;
    }

    private int              _usedTextures;
    private VixieTextureGL[] _texHandles;
    public override void Draw() {
        this._vao.Bind();

        this._shader.Bind();
        
        this._vtxBuffer.Bind();
        this._idxBuffer.Bind();

        this._shader.SetUniform("ProjectionMatrix", this._backend.ProjectionMatrix);

        for (int i = 0; i < this._usedTextures; i++) {
            VixieTextureGL tex = this._texHandles[i];

            tex.Bind(TextureUnit.Texture0 + i);
        }

        this._gl.DrawElements(PrimitiveType.Triangles, (uint)(this._idxMapper.ReservedBytes / sizeof(ushort)), DrawElementsType.UnsignedShort, null);

        this._idxBuffer.Unbind();
        this._vtxBuffer.Unbind();
        
        this._vao.Unbind();
    }
    
    protected override void DisposeInternal() {
        //Clear all the references
        for (int i = 0; i < this._texHandles.Length; i++) {
            this._texHandles[i] = null;
        }
        
        this._vtxMapper.Dispose();
        this._idxMapper.Dispose();
    }
}