using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.OpenGL.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.FontStashSharp;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Backends.OpenGL; 

internal unsafe class OpenGLRenderer : Renderer {
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

        public VixieTextureGL[] TexArray;
        public int              UsedTextures;
        
        private bool _isDisposed = false;
        public void Dispose() {
            if (this._isDisposed)
                return;

            this._isDisposed = true;
            
            this.Vao?.Dispose();

            this.TexArray = null;
        }

        ~BufferData() {
            DisposeQueue.Enqueue(this);
        }
    }

    private readonly List<BufferData> _bufferList = new();

    private const int QUAD_COUNT = 256;
    
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
        
        this._gl.BindAttribLocation(this._shader.ProgramId, 0, "VertexPosition");
        this._gl.BindAttribLocation(this._shader.ProgramId, 1, "TextureCoordinate");
        this._gl.BindAttribLocation(this._shader.ProgramId, 2, "VertexColor");
        this._gl.BindAttribLocation(this._shader.ProgramId, 3, "TextureId2");
        this._gl.BindAttribLocation(this._shader.ProgramId, 4, "TextureId");
        
        this._shader.Unbind();

        this._texHandles = new VixieTextureGL[this._backend.QueryMaxTextureUnits()];
        
        this.FontRenderer = new VixieFontStashRenderer(backend, this);
    }

    private void SetVtxBufferToVao(VertexArrayObjectGL vao, BufferObjectGL buffer) {
        vao.Bind();

        VertexBufferLayoutGL layout = new();
        layout.AddElement<float>(2); //Position
        layout.AddElement<float>(2); //Texture Coordinate
        layout.AddElement<float>(4); //Color
        layout.AddElement<int>(1);   //Texture id2
        layout.AddElement<int>(1);   //Texture id

        buffer.Bind();
        
        this._backend.EnableVertexAttribArray(0);
        this._backend.EnableVertexAttribArray(1);
        this._backend.EnableVertexAttribArray(2);
        this._backend.EnableVertexAttribArray(3);
        this._backend.EnableVertexAttribArray(4);

        //TODO: dont do this auto casting bs if we dont have to, detect supported OpenGL and GLSL versions, and use native
        //attribute floats where possible
        this._backend.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), null);
        this._backend.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex),
                                          (void*)Marshal.OffsetOf<Vertex>(nameof (Vertex.TextureCoordinate)));
        this._backend.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex),
                                          (void*)Marshal.OffsetOf<Vertex>(nameof (Vertex.Color)));
        this._backend.VertexAttribPointer(3, 1, VertexAttribPointerType.Int, false, (uint)sizeof(Vertex),
                                          (void*)Marshal.OffsetOf<Vertex>(nameof (Vertex.TexId)));
        this._backend.VertexAttribPointer(4, 1, VertexAttribPointerType.Int, false, (uint)sizeof(Vertex),
                                          (void*)(Marshal.OffsetOf<Vertex>(nameof (Vertex.TexId)) + sizeof(int)));

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
        this._texDict.Clear();
        
        this._bufferList.ForEach(x => {
            this._vtxQueue.Push(x.Vtx);
            this._idxQueue.Push(x.Idx);
        }); 
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

    private readonly Dictionary<VixieTexture, long> _texDict = new();
    public override long GetTextureId(VixieTexture tex) {
        if (tex is not VixieTextureGL texGl)
            throw new InvalidOperationException($"You must pass a {typeof(VixieTextureGL)} into this function!");

        if (this._texDict.TryGetValue(tex, out long id))
            return id;

        this._texHandles[this._usedTextures] = texGl;
        this._texDict.Add(tex, this._usedTextures);

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
        
        this._texDict.Clear();
 
        this._vtxMapper.Dispose();
        this._idxMapper.Dispose();
        
        this._vtxQueue.Clear();
        this._idxQueue.Clear();
        
        this._shader.Dispose();
    }
}