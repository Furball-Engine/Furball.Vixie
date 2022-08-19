using System;
using System.Collections.Generic;
using System.Diagnostics;
using Furball.Vixie.Backends.OpenGL.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Backends.OpenGL; 

internal unsafe class OpenGLRenderer : IRenderer {
    private readonly OpenGLBackend _backend;
    private readonly GL            _gl;

    private RamBufferMapper _vtxMapper;
    private RamBufferMapper _idxMapper;

    private readonly ShaderGL _shader;

    private class BufferData : IDisposable {
        public VertexArrayObjectGL Vao;
        public BufferObjectGL      Vtx;
        public BufferObjectGL      Idx;
        public uint                IndexCount;

        public VixieTextureGL[] TexArray;
        public int              UsedTextures;
        
        private bool _isDisposed = false;
        public void Dispose() {
            if (this._isDisposed)
                return;

            this._isDisposed = true;
            
            this.Vtx?.Dispose();
            this.Idx?.Dispose();
            this.Vao?.Dispose();

            this.TexArray = null;
        }

        ~BufferData() {
            DisposeQueue.Enqueue(this);
        }
    }

    private readonly List<BufferData> _bufferList = new();

    private const int QUAD_COUNT = 1024;
    
    public OpenGLRenderer(OpenGLBackend backend) {
        this._backend = backend;
        this._gl      = backend.GetModernGL();
        
        this._vtxMapper = new RamBufferMapper((nuint)(sizeof(Vertex) * QUAD_COUNT * 4));
        this._idxMapper = new RamBufferMapper(sizeof(ushort) * QUAD_COUNT * 6);

        this._shader = new ShaderGL(backend);
        this._shader.AttachShader(ShaderType.VertexShader, ResourceHelpers.GetStringResource("Shaders/VertexShader.glsl"));
        this._shader.AttachShader(ShaderType.FragmentShader, RendererShaderGenerator.GetFragment(this._backend));
        this._shader.Link();

        this._shader.Bind();
        for (int i = 0; i < backend.QueryMaxTextureUnits(); i++) {
            this._shader.BindUniformToTexUnit($"tex_{i}", i);
        }
        this._shader.Unbind();

        this._texHandles = new VixieTextureGL[this._backend.QueryMaxTextureUnits()];
    }

    private BufferObjectGL CreateNewVertexBuffer(VertexArrayObjectGL vao) {
        BufferObjectGL buffer = new(this._backend, (int)this._vtxMapper.SizeInBytes, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);

        vao.Bind();

        VertexBufferLayoutGL layout = new();
        layout.AddElement<float>(2); //Position
        layout.AddElement<float>(2); //Texture Coordinate
        layout.AddElement<float>(4); //Color
        layout.AddElement<int>(1);   //Texture id

        vao.AddBuffer(buffer, layout);

        buffer.Unbind();

        vao.Unbind();

        return buffer;
    }
    
    private BufferObjectGL CreateNewIndexBuffer() {
        BufferObjectGL buffer = new(this._backend, (int)this._idxMapper.SizeInBytes, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw);

        return buffer;
    }

    private void DumpToBuffers() {
        if (this._vtxMapper.ReservedBytes == 0 || this._idxMapper.ReservedBytes == 0)
            return;
        
        //TODO: make VAOs optional (for pre GL3.0)
        VertexArrayObjectGL vao = new VertexArrayObjectGL(this._backend);
        
        BufferObjectGL vtx = this.CreateNewVertexBuffer(vao);
        BufferObjectGL idx = this.CreateNewIndexBuffer();

        vtx.Bind();
        idx.Bind();
        vtx.SetSubData(this._vtxMapper.Handle, this._vtxMapper.ReservedBytes);
        idx.SetSubData(this._idxMapper.Handle, this._idxMapper.ReservedBytes);
        vtx.Unbind();
        idx.Unbind();
        
        BufferData buf;
        this._bufferList.Add(buf = new BufferData {
            Vtx          = vtx, Idx = idx, IndexCount = this._indexCount, Vao = vao, TexArray = new VixieTextureGL[this._texHandles.Length],
            UsedTextures = this._usedTextures
        });
        Array.Copy(this._texHandles, buf.TexArray, this._usedTextures);
        
        this._indexCount   = 0;
        this._indexOffset  = 0;
        
        this._vtxMapper.Reset();
        this._idxMapper.Reset();

        //Clear the `boundat` stuff
        for (int i = 0; i < this._usedTextures; i++) {
            VixieTextureGL tex = this._texHandles[i];

            tex.BoundId = -1;
        }
        this._usedTextures = 0;
    }
    
    public override void Begin() {
        this._bufferList.ForEach(x => x.Dispose());
        this._bufferList.Clear();

        this._vtxMapper.Map();
        this._idxMapper.Map();

        this._usedTextures = 0;
        for (int i = 0; i < this._texHandles.Length; i++) {
            this._texHandles[i] = null;
        }

        this._indexCount = 0;
        this._indexOffset = 0;
    }
    
    public override void End() {
        this._vtxMapper.Unmap();
        this._idxMapper.Unmap();

        this.DumpToBuffers();
    }

    private uint _indexOffset;
    private uint _indexCount;
    public override MappedData Reserve(ushort vertexCount, uint indexCount) {
        Debug.Assert(vertexCount != 0, "vertexCount != 0");
        Debug.Assert(indexCount  != 0, "indexCount != 0");
        
        Debug.Assert(vertexCount * sizeof(Vertex) < (int)this._vtxMapper.SizeInBytes, "vertexCount * sizeof(Vertex) < this._vtxMapper.SizeInBytes");
        Debug.Assert(indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes, "indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes");
        
        void* vertex = this._vtxMapper.Reserve((nuint)(vertexCount * sizeof(Vertex)));
        void* index  = this._idxMapper.Reserve(indexCount * sizeof(ushort));
        
        if (vertex == null || index == null) {
            this.DumpToBuffers();

            return this.Reserve(vertexCount, indexCount);
        }
        
        this._indexCount  += indexCount;
        this._indexOffset += vertexCount;

        return new MappedData((Vertex*)vertex, (ushort*)index, vertexCount, indexCount, this._indexOffset - vertexCount);
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
            this.DumpToBuffers();
            return this.GetTextureId(tex);
        }
        
        return this._usedTextures - 1;
    }

    private int              _usedTextures;
    private VixieTextureGL[] _texHandles;
    public override void Draw() {
        this._shader.Bind();
        this._shader.SetUniform("ProjectionMatrix", this._backend.ProjectionMatrix);

        for (int i = 0; i < this._bufferList.Count; i++) {
            BufferData buf = this._bufferList[i];
            buf.Vao.Bind();

            buf.Vtx.Bind();
            buf.Idx.Bind();

            for (int i2 = 0; i2 < buf.UsedTextures; i2++) {
                VixieTextureGL tex = buf.TexArray[i2];

                tex.Bind(TextureUnit.Texture0 + i2);
            }

            this._gl.DrawElements(PrimitiveType.Triangles, buf.IndexCount, DrawElementsType.UnsignedShort, null);

            buf.Idx.Unbind();
            buf.Vtx.Unbind();

            buf.Vao.Unbind();
        }

        this._shader.Unbind();
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