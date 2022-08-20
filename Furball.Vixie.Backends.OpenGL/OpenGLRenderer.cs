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

    private Stack<BufferObjectGL> _vtxQueue = new();
    private Stack<BufferObjectGL> _idxQueue = new();

    private class BufferData : IDisposable {
        public VertexArrayObjectGL Vao;
        public BufferObjectGL      Vtx;
        public BufferObjectGL      Idx;
        public uint                IndexCount;

        public VixieTextureGL Texture;
        
        private bool _isDisposed = false;
        public void Dispose() {
            if (this._isDisposed)
                return;

            this._isDisposed = true;
            
            this.Vao?.Dispose();

            this.Texture = null;
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
        this._shader.BindUniformToTexUnit("tex", 0);
        this._shader.Unbind();
    }

    private void SetVtxBufferToVao(VertexArrayObjectGL vao, BufferObjectGL buffer) {
        vao.Bind();

        VertexBufferLayoutGL layout = new();
        layout.AddElement<float>(2); //Position
        layout.AddElement<float>(2); //Texture Coordinate
        layout.AddElement<float>(4); //Color
        layout.AddElement<int>(2);   //Texture id

        vao.AddBuffer(buffer, layout);

        buffer.Unbind();

        vao.Unbind();
    }
    
    private BufferObjectGL CreateNewVertexBuffer() {
        BufferObjectGL buffer = new(this._backend, (int)this._vtxMapper.SizeInBytes, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);

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
        
        BufferObjectGL vtx;
        BufferObjectGL idx;
        if (this._vtxQueue.Count == 0) {
            vtx = this.CreateNewVertexBuffer();
            idx = this.CreateNewIndexBuffer();
        }
        else {
            vtx = this._vtxQueue.Pop();
            idx = this._idxQueue.Pop();
        }
        
        this.SetVtxBufferToVao(vao, vtx);

        vtx.Bind();
        idx.Bind();
        vtx.SetSubData(this._vtxMapper.Handle, this._vtxMapper.ReservedBytes);
        idx.SetSubData(this._idxMapper.Handle, this._idxMapper.ReservedBytes);
        vtx.Unbind();
        idx.Unbind();
        
        this._bufferList.Add(new BufferData {
            Vtx = vtx, Idx = idx, IndexCount = this._indexCount, Vao = vao, Texture = this._currentTexture
        });

        this._indexCount   = 0;
        this._indexOffset  = 0;
        
        this._vtxMapper.Reset();
        this._idxMapper.Reset();

        this._currentTexture = null;
    }
    
    public override void Begin() {
        this._bufferList.ForEach(x => {
            this._vtxQueue.Push(x.Vtx);
            this._idxQueue.Push(x.Idx);
        }); 
        this._bufferList.Clear();

        this._vtxMapper.Map();
        this._idxMapper.Map();

        this._currentTexture = null;

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
    public override MappedData Reserve(ushort vertexCount, uint indexCount, VixieTexture texture) {
        Debug.Assert(vertexCount != 0, "vertexCount != 0");
        Debug.Assert(indexCount  != 0, "indexCount != 0");
        
        Debug.Assert(vertexCount * sizeof(Vertex) < (int)this._vtxMapper.SizeInBytes, "vertexCount * sizeof(Vertex) < this._vtxMapper.SizeInBytes");
        Debug.Assert(indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes, "indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes");

        if (this._currentTexture == null)
            this._currentTexture = (VixieTextureGL)texture;

        if (this._currentTexture != texture) {
            this.DumpToBuffers();
            
            return this.Reserve(vertexCount, indexCount, texture);
        }
        
        void* vertex = this._vtxMapper.Reserve((nuint)(vertexCount * sizeof(Vertex)));
        void* index  = this._idxMapper.Reserve(indexCount * sizeof(ushort));
        
        if (vertex == null || index == null) {
            this.DumpToBuffers();

            return this.Reserve(vertexCount, indexCount, texture);
        }
        
        this._indexCount  += indexCount;
        this._indexOffset += vertexCount;

        return new MappedData((Vertex*)vertex, (ushort*)index, vertexCount, indexCount, this._indexOffset - vertexCount, 0);
    }
    
    private VixieTextureGL _currentTexture;
    public override void Draw() {
        this._shader.Bind();
        this._shader.SetUniform("ProjectionMatrix", this._backend.ProjectionMatrix);

        for (int i = 0; i < this._bufferList.Count; i++) {
            BufferData buf = this._bufferList[i];
            buf.Vao.Bind();

            buf.Vtx.Bind();
            buf.Idx.Bind();

            buf.Texture.Bind();
            
            this._gl.DrawElements(PrimitiveType.Triangles, buf.IndexCount, DrawElementsType.UnsignedShort, null);

#if DEBUG
            buf.Idx.Unbind();
            buf.Vtx.Unbind();

            buf.Vao.Unbind();
#endif
        }

#if DEBUG
        this._shader.Unbind();
#endif
    }
    
    protected override void DisposeInternal() {
        //Clear all the references
        this._currentTexture = null;
        
        this._vtxMapper.Dispose();
        this._idxMapper.Dispose();
        
        this._vtxQueue.Clear();
        this._idxQueue.Clear();
        
        this._shader.Dispose();
    }
}