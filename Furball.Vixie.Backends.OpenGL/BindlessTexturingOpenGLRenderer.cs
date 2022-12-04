using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.OpenGL.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Backends.OpenGL;

// ReSharper disable once InconsistentNaming
public unsafe class BindlessTexturingOpenGLRenderer : VixieRenderer {
    private readonly OpenGLBackend _backend;

    private bool _begun;

    private const int QUAD_COUNT = 2048;

    private readonly RamBufferMapper _vtxMapper;
    private readonly RamBufferMapper _idxMapper;

    private Stack<BufferObjectGl> _vtxQueue   = new();
    private Stack<BufferObjectGl> _idxQueue   = new();
    private List<BufferData>      _bufferList = new List<BufferData>();

    private uint _indexOffset;
    private uint _indexCount;

    private class BufferData {
        public VertexArrayObjectGl Vao;
        public BufferObjectGl      Vtx;
        public BufferObjectGl      Idx;

        public uint IndexCount;
    }

    public BindlessTexturingOpenGLRenderer(OpenGLBackend backend) {
        this._backend = backend;

        this._vtxMapper = new RamBufferMapper((nuint)(sizeof(Vertex) * QUAD_COUNT * 4));
        this._idxMapper = new RamBufferMapper(sizeof(ushort) * QUAD_COUNT * 6);
    }

    private VertexArrayObjectGl CreateVao(BufferObjectGl vtx) {
        VertexArrayObjectGl vao = new VertexArrayObjectGl(this._backend);

        vao.Bind();
        vtx.Bind();

        this._backend.EnableVertexAttribArray(0);
        this._backend.EnableVertexAttribArray(1);
        this._backend.EnableVertexAttribArray(2);
        this._backend.EnableVertexAttribArray(3);

        this._backend.VertexAttribPointer(
            0,
            2,
            VertexAttribPointerType.Float,
            false,
            (uint)sizeof(Vertex),
            null
        );
        this._backend.VertexAttribPointer(
            1,
            2,
            VertexAttribPointerType.Float,
            false,
            (uint)sizeof(Vertex),
            (void*)Marshal.OffsetOf<Vertex>(nameof (Vertex.TextureCoordinate))
        );
        this._backend.VertexAttribPointer(
            2,
            4,
            VertexAttribPointerType.Float,
            false,
            (uint)sizeof(Vertex),
            (void*)Marshal.OffsetOf<Vertex>(nameof (Vertex.Color))
        );
        this._backend.VertexAttribPointer(
            3,
            2,
            VertexAttribPointerType.Int,
            false,
            (uint)sizeof(Vertex),
            (void*)Marshal.OffsetOf<Vertex>(nameof (Vertex.TexId))
        );


        vtx.Unbind();
        vao.Unbind();

        return vao;
    }

    private BufferObjectGl CreateNewVertexBuffer() {
        BufferObjectGl buffer = new(this._backend, (int)this._vtxMapper.SizeInBytes, BufferTargetARB.ArrayBuffer,
                                    BufferUsageARB.StaticDraw);

        return buffer;
    }

    private BufferObjectGl CreateNewIndexBuffer() {
        BufferObjectGl buffer = new(this._backend, (int)this._idxMapper.SizeInBytes, BufferTargetARB.ElementArrayBuffer,
                                    BufferUsageARB.StaticDraw);

        return buffer;
    }

    private void DumpToBuffers() {
        if (this._vtxMapper.ReservedBytes == 0 || this._idxMapper.ReservedBytes == 0)
            return;

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

        VertexArrayObjectGl vao = null;
        if (this._backend.VaoFeatureLevel.Boolean) {
            vao = this.CreateVao(vtx);
        }

        vtx.Bind();
        idx.Bind();
        vtx.SetSubData(this._vtxMapper.Handle, this._vtxMapper.ReservedBytes);
        idx.SetSubData(this._idxMapper.Handle, this._idxMapper.ReservedBytes);
        vtx.Unbind();
        idx.Unbind();

        this._bufferList.Add(new BufferData {
            Vtx        = vtx,
            Idx        = idx,
            IndexCount = this._indexCount,
            Vao        = vao
        });

        this._indexCount  = 0;
        this._indexOffset = 0;

        this._vtxMapper.Reset();
        this._idxMapper.Reset();
    }

    public override void Begin() {
        Guard.Assert(!this._begun, "!this._begun");
        this._begun = true;
        
        this._bufferList.ForEach(x => {
            //If the vertex or index buffer are set, add them to the queue
            if(x.Vtx != null)
                this._vtxQueue.Push(x.Vtx);
            if(x.Idx != null)
                this._idxQueue.Push(x.Idx);

            //Set the vtx and idx buffers to null to prevent them from being held by the object
            x.Vtx = null;
            x.Idx = null;

            //Dispose the VAO if it exists
            x.Vao?.Dispose();
            //Set the VAO to null to prevent it from being held by the object
            x.Vao = null;
        }); 
        this._bufferList.Clear();

        this._vtxMapper.Map();
        this._idxMapper.Map();

        this._indexCount  = 0;
        this._indexOffset = 0;
    }

    public override void End() {
        Guard.Assert(this._begun, "this._begun");
        this._begun = false;
        
        this._vtxMapper.Unmap();
        this._idxMapper.Unmap();
        
        this.DumpToBuffers();
    }

    private long GetTextureId(VixieTextureGl tex) {
        //If we dont know its bindless handle, we need to get it, then make it resident
        if (tex.BindlessHandle == 0) {
            tex.BindlessHandle = this._backend.BindlessTexturingExtension.GetTextureHandle(tex.TextureId);
            this._backend.BindlessTexturingExtension.MakeTextureHandleResident(tex.BindlessHandle);
        }

        //Reinterpret the ulong as a long, as thats what goes in our Vertex struct
        ulong handle = tex.BindlessHandle;
        return *(long*)&handle;
    }

    public override MappedData Reserve(ushort vertexCount, uint indexCount, VixieTexture tex) {
        Guard.Assert(vertexCount != 0, "vertexCount != 0");
        Guard.Assert(indexCount  != 0, "indexCount != 0");
        
        Guard.Assert(vertexCount * sizeof(Vertex) < (int)this._vtxMapper.SizeInBytes, "vertexCount * sizeof(Vertex) < this._vtxMapper.SizeInBytes");
        Guard.Assert(indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes, "indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes");
        
        Guard.Assert(tex is VixieTextureGl, "tex is VixieTextureGl");
        
        void* vertex = this._vtxMapper.Reserve((nuint)(vertexCount * sizeof(Vertex)));
        void* index  = this._idxMapper.Reserve(indexCount * sizeof(ushort));
        
        if (vertex == null || index == null) {
            this.DumpToBuffers();

            return this.Reserve(vertexCount, indexCount, tex);
        }
        
        this._indexCount  += indexCount;
        this._indexOffset += vertexCount;

        return new MappedData((Vertex*)vertex, (ushort*)index, vertexCount, indexCount, this._indexOffset - 
                                                                                        vertexCount, this.GetTextureId((VixieTextureGl)tex));
    }

    public override void Draw() {
        this._backend.Shader.Bind();

        for (int i = 0; i < this._bufferList.Count; i++) {
            BufferData buf = this._bufferList[i];
            buf.Vao?.Bind();

            buf.Vtx?.Bind();
            buf.Idx?.Bind();

            this._backend.gl.DrawElements(
                PrimitiveType.Triangles,
                buf.IndexCount,
                DrawElementsType.UnsignedShort,
                null
            );

            buf.Idx?.Unbind();
            buf.Vtx?.Unbind();

            buf.Vao?.Unbind();
        }

        this._backend.Shader.Unbind();
    }

    protected override void DisposeInternal() {
        //Clear the buffer list to release any held references
        this._bufferList.Clear();
        
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