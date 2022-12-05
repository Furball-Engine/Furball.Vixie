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

    private readonly uint _textureUniformIndex;

    private Stack<BufferObjectGl> _vtxQueue   = new();
    private Stack<BufferObjectGl> _idxQueue   = new();
    private Stack<BufferObjectGl> _uboQueue   = new();
    private List<BufferData>      _bufferList = new List<BufferData>();

    private uint _indexOffset;
    private uint _indexCount;

    public const int MAX_TEXTURES = 256; //keep this count in sync with the shader

    private Dictionary<VixieTextureGl, int> _textureMap      = new Dictionary<VixieTextureGl, int>();
    private TextureUniformEntry[]           _currentTextures = new TextureUniformEntry[MAX_TEXTURES];
    private int                             _usedTextures;

    private class BufferData {
        public VertexArrayObjectGl Vao;
        public BufferObjectGl      Vtx;
        public BufferObjectGl      Idx;
        public BufferObjectGl      Ubo;

        public uint IndexCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TextureUniformEntry {
        public  ulong Handle;
        private ulong _padding; //padding to 16 bytes
    }

    private const uint UNIFORM_BLOCK_BINDING = 1;

    public BindlessTexturingOpenGLRenderer(OpenGLBackend backend) {
        this._backend = backend;

        this._vtxMapper = new RamBufferMapper((nuint)(sizeof(Vertex) * QUAD_COUNT * 4));
        this._idxMapper = new RamBufferMapper(sizeof(ushort) * QUAD_COUNT * 6);
        
        this._textureUniformIndex = this._backend.gl.GetUniformBlockIndex(this._backend.Shader.ProgramId, "TextureUniform");
        this._backend.gl.UniformBlockBinding(this._backend.Shader.ProgramId, this._textureUniformIndex, UNIFORM_BLOCK_BINDING);
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
            1,
            VertexAttribPointerType.UnsignedInt,
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

    private BufferObjectGl CreateNewUniformBuffer() {
        BufferObjectGl buffer = new(
            this._backend,
            MAX_TEXTURES * sizeof(TextureUniformEntry),
            BufferTargetARB.UniformBuffer,
            BufferUsageARB.StaticDraw
        );

        return buffer;
    }

    private void DumpToBuffers() {
        if (this._vtxMapper.ReservedBytes == 0 || this._idxMapper.ReservedBytes == 0)
            return;

        BufferObjectGl vtx;
        BufferObjectGl idx;
        BufferObjectGl ubo;
        if (this._vtxQueue.Count == 0) {
            vtx = this.CreateNewVertexBuffer();
            idx = this.CreateNewIndexBuffer();
            ubo = this.CreateNewUniformBuffer();
        }
        else {
            vtx = this._vtxQueue.Pop();
            idx = this._idxQueue.Pop();
            ubo = this._uboQueue.Pop();
        }

        VertexArrayObjectGl vao = null;
        if (this._backend.VaoFeatureLevel.Boolean) {
            vao = this.CreateVao(vtx);
        }

        vtx.Bind();
        idx.Bind();
        ubo.Bind();
        vtx.SetSubData(this._vtxMapper.Handle, this._vtxMapper.ReservedBytes);
        idx.SetSubData(this._idxMapper.Handle, this._idxMapper.ReservedBytes);
        ubo.SetSubData<TextureUniformEntry>(this._currentTextures);
        // ubo.SetSubData<TextureUniformEntry>(this._currentTextures, this._usedTextures);
        ubo.Unbind();
        vtx.Unbind();
        idx.Unbind();

        this._bufferList.Add(new BufferData {
            Vao        = vao,
            Vtx        = vtx,
            Idx        = idx,
            Ubo        = ubo,
            IndexCount = this._indexCount,
        });

        this._indexCount   = 0;
        this._indexOffset  = 0;
        this._usedTextures = 0;
        this._textureMap.Clear();

        this._vtxMapper.Reset();
        this._idxMapper.Reset();
    }

    public override void Begin() {
        Guard.Assert(!this._begun, "!this._begun");
        this._begun = true;

        this._bufferList.ForEach(x => {
            //If the vertex or index buffer are set, add them to the queue
            if (x.Vtx != null)
                this._vtxQueue.Push(x.Vtx);
            if (x.Idx != null)
                this._idxQueue.Push(x.Idx);
            if (x.Ubo != null)
                this._uboQueue.Push(x.Ubo);

            //Set the vtx and idx buffers to null to prevent them from being held by the object
            x.Vtx = null;
            x.Idx = null;
            x.Ubo = null;

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

        //If we have encountered this texture before, return the index into the array
        if (this._textureMap.TryGetValue(tex, out int ind))
            return ind;
        
        //If we have no encountered this texture before, add it to the array and map, and return the index
        ind = this._usedTextures;
        this._currentTextures[ind] = new TextureUniformEntry {
            Handle = tex.BindlessHandle
        };
        this._textureMap.Add(tex, ind);
        
        this._usedTextures++;
        return ind;
    }

    public override MappedData Reserve(ushort vertexCount, uint indexCount, VixieTexture tex) {
        Guard.Assert(vertexCount != 0, "vertexCount != 0");
        Guard.Assert(indexCount  != 0, "indexCount != 0");

        Guard.Assert(vertexCount * sizeof(Vertex) < (int)this._vtxMapper.SizeInBytes,
                     "vertexCount * sizeof(Vertex) < this._vtxMapper.SizeInBytes");
        Guard.Assert(indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes,
                     "indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes");

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
                                                                                        vertexCount,
                              this.GetTextureId((VixieTextureGl)tex));
    }

    public override void Draw() {
        this._backend.Shader.Bind();

        for (int i = 0; i < this._bufferList.Count; i++) {
            BufferData buf = this._bufferList[i];
            buf.Vao?.Bind();

            buf.Vtx?.Bind();
            buf.Idx?.Bind();
            buf.Ubo?.Bind();
            this._backend.gl.BindBufferBase(BufferTargetARB.UniformBuffer, UNIFORM_BLOCK_BINDING, buf.Ubo!.BufferId);

            this._backend.gl.DrawElements(
                PrimitiveType.Triangles,
                buf.IndexCount,
                DrawElementsType.UnsignedShort,
                null
            );

            buf.Ubo?.Unbind();
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

        while (this._vtxQueue.Count > 0) {
            this._vtxQueue.Pop().Dispose();
        }
        while (this._idxQueue.Count > 0) {
            this._idxQueue.Pop().Dispose();
        }

        this._vtxQueue.Clear();
        this._idxQueue.Clear();
    }
}