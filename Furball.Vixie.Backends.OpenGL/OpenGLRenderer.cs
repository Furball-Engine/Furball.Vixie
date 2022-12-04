#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.OpenGL.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Backends.OpenGL; 

internal unsafe class OpenGlVixieRenderer : VixieRenderer {
    private readonly OpenGLBackend _backend;
    private readonly GL            _gl;

    private RamBufferMapper _vtxMapper;
    private RamBufferMapper _idxMapper;


    private Stack<BufferObjectGl> _vtxQueue = new();
    private Stack<BufferObjectGl> _idxQueue = new();

    private class BufferData : IDisposable {
        public VertexArrayObjectGl? Vao;
        public BufferObjectGl?      Vtx;
        public BufferObjectGl?      Idx;
        public uint                 IndexCount;

        public VixieTextureGl[] TexArray;
        public int              UsedTextures;
        
        private bool _isDisposed = false;
        public void Dispose() {
            if (this._isDisposed)
                return;

            this._isDisposed = true;
            
            this.Vao?.Dispose();

            this.TexArray = null;
        }
    }

    private readonly List<BufferData> _bufferList = new();

    private const int QUAD_COUNT = 256;
    
    public OpenGlVixieRenderer(OpenGLBackend backend) {
        this._backend = backend;
        this._gl      = backend.GetModernGl();
        
        this._vtxMapper = new RamBufferMapper((nuint)(sizeof(Vertex) * QUAD_COUNT * 4));
        this._idxMapper = new RamBufferMapper(sizeof(ushort) * QUAD_COUNT * 6);

        this._texHandles = new VixieTextureGl[this._backend.QueryMaxTextureUnits()];
    }

    private void SetVtxBufferToVao(VertexArrayObjectGl? vao, BufferObjectGl buffer) {
        vao?.Bind();

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

        vao?.Unbind();
    }
    
    private BufferObjectGl CreateNewVertexBuffer() {
        BufferObjectGl buffer = new(this._backend, (int)this._vtxMapper.SizeInBytes, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);

        return buffer;
    }
    
    private BufferObjectGl CreateNewIndexBuffer() {
        BufferObjectGl buffer = new(this._backend, (int)this._idxMapper.SizeInBytes, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw);

        return buffer;
    }

    private void DumpToBuffers() {
        if (this._vtxMapper.ReservedBytes == 0 || this._idxMapper.ReservedBytes == 0)
            return;

        VertexArrayObjectGl? vao = null;
        if(this._backend.VaoFeatureLevel.Boolean) {
            vao = new VertexArrayObjectGl(this._backend);
        }

        BufferObjectGl vtx;
        BufferObjectGl idx;
        if (this._vtxQueue.Count == 0) {
            vtx = this.CreateNewVertexBuffer();
            idx = this.CreateNewIndexBuffer();
        }
        else {
            vtx = this._vtxQueue.Pop();
            idx = this._idxQueue.Pop();
        }
        
        if(vao != null)
            this.SetVtxBufferToVao(vao, vtx);

        vtx.Bind();
        idx.Bind();
        vtx.SetSubData(this._vtxMapper.Handle, this._vtxMapper.ReservedBytes);
        idx.SetSubData(this._idxMapper.Handle, this._idxMapper.ReservedBytes);
        vtx.Unbind();
        idx.Unbind();
        
        BufferData buf;
        this._bufferList.Add(buf = new BufferData {
            Vtx          = vtx, Idx = idx, IndexCount = this._indexCount, Vao = vao, TexArray = new VixieTextureGl[this._texHandles.Length],
            UsedTextures = this._usedTextures
        });
        Array.Copy(this._texHandles, buf.TexArray, this._usedTextures);
        
        this._indexCount   = 0;
        this._indexOffset  = 0;
        
        this._vtxMapper.Reset();
        this._idxMapper.Reset();

        //Clear the `boundat` stuff
        for (int i = 0; i < this._usedTextures; i++) {
            VixieTextureGl tex = this._texHandles[i];
        }
        this._usedTextures = 0;

        this._texDict.Clear();
    }
    
    public override void Begin() {
        this._texDict.Clear();
        
        this._bufferList.ForEach(x => {
            if(x.Vtx != null)
                this._vtxQueue.Push(x.Vtx);
            if(x.Idx != null)
                this._idxQueue.Push(x.Idx);

            x.Vtx = null;
            x.Idx = null;

            x.Vao?.Dispose();
            x.Vao = null;
            
            x.Dispose();
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
    public override MappedData Reserve(ushort vertexCount, uint indexCount, VixieTexture tex) {
        Guard.Assert(vertexCount != 0, "vertexCount != 0");
        Guard.Assert(indexCount  != 0, "indexCount != 0");
        
        Guard.Assert(vertexCount * sizeof(Vertex) < (int)this._vtxMapper.SizeInBytes, "vertexCount * sizeof(Vertex) < this._vtxMapper.SizeInBytes");
        Guard.Assert(indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes, "indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes");
        
        void* vertex = this._vtxMapper.Reserve((nuint)(vertexCount * sizeof(Vertex)));
        void* index  = this._idxMapper.Reserve(indexCount * sizeof(ushort));
        
        if (vertex == null || index == null) {
            this.DumpToBuffers();

            return this.Reserve(vertexCount, indexCount, tex);
        }
        
        this._indexCount  += indexCount;
        this._indexOffset += vertexCount;

        return new MappedData((Vertex*)vertex, (ushort*)index, vertexCount, indexCount, this._indexOffset - 
        vertexCount, this.GetTextureId(tex));
    }

    private readonly Dictionary<VixieTexture, long> _texDict = new();
    private long GetTextureId(VixieTexture tex) {
        if (tex is not VixieTextureGl texGl)
            throw new InvalidOperationException($"You must pass a {typeof(VixieTextureGl)} into this function!");

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

    private int               _usedTextures;
    private VixieTextureGl?[] _texHandles;
    public override void Draw() {
        this._backend.Shader.Bind();

        for (int i = 0; i < this._bufferList.Count; i++) {
            BufferData buf = this._bufferList[i];
            buf.Vao?.Bind();

            buf.Vtx?.Bind();
            buf.Idx?.Bind();

            if (buf.Vao == null) {
                this.SetVtxBufferToVao(null, buf.Vtx!);
            }

            for (int i2 = 0; i2 < buf.UsedTextures; i2++) {
                VixieTextureGl tex = buf.TexArray[i2];

                tex.Bind(TextureUnit.Texture0 + i2);
            }

            this._gl.DrawElements(PrimitiveType.Triangles, buf.IndexCount, DrawElementsType.UnsignedShort, null);

            buf.Idx?.Unbind();
            buf.Vtx?.Unbind();

            buf.Vao?.Unbind();
        }

        this._backend.Shader.Unbind();
    }
    
    protected override void DisposeInternal() {
        //Clear all the references
        for (int i = 0; i < this._texHandles.Length; i++) {
            this._texHandles[i] = null;
        }
        
        this._texDict.Clear();
 
        this._bufferList.ForEach(x => {
            x.Vtx?.Dispose();
            x.Idx?.Dispose();
            x.Dispose();
        });
        
        this._vtxMapper.Dispose();
        this._idxMapper.Dispose();

        while(this._vtxQueue.Count > 0) {
            this._vtxQueue.Pop().Dispose();
        }
        while(this._idxQueue.Count > 0) {
            this._idxQueue.Pop().Dispose();
        }
        
        this._vtxQueue.Clear();
        this._idxQueue.Clear();
    }
}