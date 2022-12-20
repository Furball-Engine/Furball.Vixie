#nullable enable
using System;
using System.Collections.Generic;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Backends.WebGPU.Abstractions;
using Furball.Vixie.Helpers;
using Silk.NET.WebGPU;
using Buffer = Silk.NET.WebGPU.Buffer;
using Color = Silk.NET.WebGPU.Color;

namespace Furball.Vixie.Backends.WebGPU;

public unsafe class WebGPURenderer : VixieRenderer {
    private readonly WebGPUBackend          _backend;
    private readonly Silk.NET.WebGPU.WebGPU _webgpu;

    private readonly WebGPUBufferMapper _vtxMapper;
    private readonly WebGPUBufferMapper _idxMapper;

    private const int QUADS_PER_BUFFER = 512;

    private readonly List<RenderBuffer> _renderBuffers = new List<RenderBuffer>();

    private Queue<WebGPUBuffer> _vtxBufferQueue = new Queue<WebGPUBuffer>();
    private Queue<WebGPUBuffer> _idxBufferQueue = new Queue<WebGPUBuffer>();

    private WebGPUTexture? _currentTexture;
    
    private readonly List<RenderBuffer> _workingBuffers = new List<RenderBuffer>();

    private class RenderBuffer : IDisposable {
        public WebGPUBuffer? Vtx;
        public WebGPUBuffer? Idx;

        public int            UsedTextures;
        public WebGPUTexture? Texture;

        public uint IndexCount;

        public uint IndexOffset;

        private bool _isDisposed;
        public void Dispose() {
            if (this._isDisposed)
                return;

            this._isDisposed = true;

            this.Vtx?.Dispose();
            this.Idx?.Dispose();

            this.Texture = null;
        }
    }

    public WebGPURenderer(WebGPUBackend backend) {
        this._backend = backend;
        this._webgpu  = backend.WebGPU;

        this._vtxMapper =
            new WebGPUBufferMapper(backend, (uint)(QUADS_PER_BUFFER * 4 * sizeof(Vertex)), BufferUsage.Vertex);
        this._idxMapper = new WebGPUBufferMapper(backend, QUADS_PER_BUFFER * 6 * sizeof(ushort), BufferUsage.Index);
    }

    private bool _isFirst = true;
    public override void Begin(CullFace cullFace = CullFace.CCW) {
        // Guard.EnsureNull(this._vtxMapper.Buffer, "this._vtxMapper._buffer");
        // Guard.EnsureNull(this._idxMapper.Buffer, "this._idxMapper._buffer");

        bool wasLastEmpty = this._renderBuffers.Count == 0;

        Buffer* lastVtx = null;
        Buffer* lastIdx = null;
        //Save all the buffers from the render queue
        foreach (RenderBuffer? x in this._renderBuffers) {
            Guard.EnsureNonNull(x.Vtx!, "x.Vtx");
            Guard.EnsureNonNull(x.Idx!, "x.Idx");

            if(lastVtx == x.Vtx!.Buffer && lastIdx == x.Idx!.Buffer)
                continue;

            this._vtxBufferQueue.Enqueue(x.Vtx!);
            this._idxBufferQueue.Enqueue(x.Idx!);

            lastVtx = x.Vtx!.Buffer;
            lastIdx = x.Idx!.Buffer;
            
            //We set these to null to ensure that they dont get disposed when `RenderBuffer.Dispose` is called by the
            //destructor
            x.Vtx = null;
            x.Idx = null;

            x.Dispose();
        }
        //Clear the render buffer queue
        this._renderBuffers.Clear();

        if (this._vtxBufferQueue.Count == 0 || wasLastEmpty) {
            Guard.Assert(this._isFirst || wasLastEmpty);

            Buffer* vtxBuf = this._vtxMapper.CopyMappedDataToNewBuffer();
            Buffer* idxBuf = this._idxMapper.CopyMappedDataToNewBuffer();

            if (vtxBuf != null && idxBuf != null) {
                this._vtxBufferQueue.Enqueue(new WebGPUBuffer(this._webgpu, vtxBuf));
                this._idxBufferQueue.Enqueue(new WebGPUBuffer(this._webgpu, idxBuf));
            }

            this._isFirst = false;
        }
        else {
            Buffer* vtxBuf = this._vtxBufferQueue.Dequeue().Buffer;
            Buffer* idxBuf = this._idxBufferQueue.Dequeue().Buffer;

            this._vtxMapper.CopyMappedDataToExistingBuffer(vtxBuf);
            this._idxMapper.CopyMappedDataToExistingBuffer(idxBuf);

            if (vtxBuf != null && idxBuf != null) {
                this._vtxBufferQueue.Enqueue(new WebGPUBuffer(this._webgpu, vtxBuf));
                this._idxBufferQueue.Enqueue(new WebGPUBuffer(this._webgpu, idxBuf));
            }
        }

        this._indexCount   = 0;
        this._indexOffset  = 0;

        this._lastIndexOffset = 0;
        this._lastIndexCount  = 0;

        this._currentTexture = null;
    }

    public override void End() {
        this.DumpToBuffers();

        // Guard.EnsureNonNull(this._vtxMapper.Buffer, "this._vtxMapper._buffer");
        // Guard.EnsureNonNull(this._idxMapper.Buffer, "this._idxMapper._buffer");

        // this._vtxMapper.Buffer!.Dispose();
        // this._idxMapper.Buffer!.Dispose();

        // this._vtxMapper.Buffer = null;
        // this._idxMapper.Buffer = null;

        // this._vtxMapper.Unmap();
        // this._idxMapper.Unmap();
    }

    private void DumpToBuffers() {
        if (this._indexCount == 0)
            return;

        Buffer* vtx;
        Buffer* idx;
        if (this._vtxBufferQueue.Count > 0) {
            vtx = this._vtxBufferQueue.Dequeue().Buffer;
            idx = this._idxBufferQueue.Dequeue().Buffer;
            
            this._vtxMapper.CopyMappedDataToExistingBuffer(vtx);
            this._idxMapper.CopyMappedDataToExistingBuffer(idx);
        }
        else {
            vtx = this._vtxMapper.CopyMappedDataToNewBuffer();
            idx = this._idxMapper.CopyMappedDataToNewBuffer();
        }

        Guard.Assert(vtx != null);
        Guard.Assert(idx != null);

        if (this._workingBuffers.Count == 0) {
            this._renderBuffers.Add(
                new RenderBuffer {
                    Vtx          = new WebGPUBuffer(this._webgpu, vtx!),
                    Idx          = new WebGPUBuffer(this._webgpu, idx!),
                    IndexCount   = this._indexCount,
                    UsedTextures = this._usedTextures,
                    Texture      = this._currentTexture,
                    IndexOffset  = this._lastIndexOffset
                }
            );
        }
        else {
            if (this._indexCount != this._lastIndexCount) {
                RenderBuffer buf = new RenderBuffer();
                buf.Texture     = this._currentTexture;
                buf.IndexOffset = this._lastIndexOffset;
                buf.IndexCount  = this._indexCount - this._lastIndexCount;

                this._lastIndexCount  = this._indexCount;
                this._lastIndexOffset = this._indexCount;
            
                this._workingBuffers.Add(buf); 
            }
            
            foreach (RenderBuffer workingBuffer in this._workingBuffers) {
                workingBuffer.Vtx = new WebGPUBuffer(this._webgpu, vtx);
                workingBuffer.Idx = new WebGPUBuffer(this._webgpu, idx);

                this._renderBuffers.Add(workingBuffer);
            }

            this._workingBuffers.Clear();
        }

        this._currentTexture = null;

        this._usedTextures = 0;
        this._indexOffset  = 0;
        this._indexCount   = 0;

        this._lastIndexCount  = 0;
        this._lastIndexOffset = 0;
    }

    private ushort _indexOffset;
    private uint   _indexCount;
    private int    _usedTextures;

    private int _reserveRecursionCount = 0;
    public override MappedData Reserve(ushort vertexCount, uint indexCount, VixieTexture tex) {
        Guard.Assert(vertexCount != 0, "vertexCount != 0");
        Guard.Assert(indexCount  != 0, "indexCount != 0");

        Guard.Assert(
            vertexCount * sizeof(Vertex) < (int)this._vtxMapper.SizeInBytes,
            "vertexCount * sizeof(Vertex) < this._vtxMapper.SizeInBytes"
        );
        Guard.Assert(
            indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes,
            "indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes"
        );

        void* vtx = this._vtxMapper.Reserve((nuint)(vertexCount * sizeof(Vertex)));
        void* idx = this._idxMapper.Reserve(indexCount * sizeof(ushort));

        long textureId = this.GetTextureId(tex);
        
        if (vtx == null || idx == null) {
            //We should *never* recurse multiple times in this function, if we do, that indicates that for some reason,
            //even after dumping to a buffer to draw, we still are unable to reserve memory.
            Guard.Assert(this._reserveRecursionCount == 0, "this._reserveRecursionCount == 0");

            this.DumpToBuffers();
            this._reserveRecursionCount++;
            return this.Reserve(vertexCount, indexCount, tex);
        }
        
        this._indexOffset += vertexCount;
        this._indexCount  += indexCount;

        this._reserveRecursionCount = 0;
        return new MappedData(
            (Vertex*)vtx,
            (ushort*)idx,
            vertexCount,
            indexCount,
            (uint)(this._indexOffset - vertexCount), 
            textureId
        );
    }

    private uint _lastIndexOffset = 0;
    private uint _lastIndexCount  = 0;
    private long GetTextureId(VixieTexture texOrig) {
        this._backend.CheckThread();

        Guard.EnsureNonNull(texOrig, "texOrig");

        WebGPUTexture tex = (WebGPUTexture)texOrig;

        if (this._currentTexture != null && tex != this._currentTexture) {
            RenderBuffer buf = new RenderBuffer();
            buf.Texture     = this._currentTexture;
            buf.IndexOffset = this._lastIndexOffset;
            buf.IndexCount  = this._indexCount - this._lastIndexCount;

            this._lastIndexCount  = this._indexCount;
            this._lastIndexOffset = this._indexCount;
            
            this._workingBuffers.Add(buf);
        }

        this._currentTexture = tex;

        return 0;
    }

    public override void Draw() {
        RenderPassColorAttachment colorAttachment = new RenderPassColorAttachment {
            View          = this._backend.SwapchainTextureView,
            LoadOp        = this._backend.ClearAsap ? LoadOp.Clear : LoadOp.Load,
            StoreOp       = StoreOp.Store,
            ResolveTarget = null, ClearValue = new Color(0, 0, 0, 0)
        };
        this._backend.ClearAsap = false;
        
        RenderPassEncoder* renderPass =
            this._webgpu.CommandEncoderBeginRenderPass(this._backend.CommandEncoder, new RenderPassDescriptor {
                ColorAttachments = &colorAttachment,
                ColorAttachmentCount = 1
            });

        this._webgpu.RenderPassEncoderSetPipeline(renderPass, this._backend.Pipeline);
        
        this._webgpu.RenderPassEncoderSetBindGroup(renderPass, 1, this._backend.ProjectionMatrixBindGroup, 0, 0);

        for (int i = 0; i < this._renderBuffers.Count; i++) {
            RenderBuffer buf = this._renderBuffers[i];
            Guard.EnsureNonNull(buf.Vtx);
            Guard.EnsureNonNull(buf.Idx);
            Guard.EnsureNonNull(buf.Texture);

            this._webgpu.RenderPassEncoderSetVertexBuffer(
                renderPass,
                0,
                buf.Vtx!.Buffer,
                0,
                this._vtxMapper.SizeInBytes
            );
            this._webgpu.RenderPassEncoderSetIndexBuffer(
                renderPass,
                buf.Idx!.Buffer,
                IndexFormat.Uint16,
                0,
                this._idxMapper.SizeInBytes
            );

            this._webgpu.RenderPassEncoderSetBindGroup(
                renderPass,
                0,
                buf.Texture!.BindGroup,
                0,
                null
            );

            this._webgpu.RenderPassEncoderDrawIndexed(renderPass, buf.IndexCount, 1, buf.IndexOffset, 0, 0);
        }

        this._webgpu.RenderPassEncoderEnd(renderPass);
    }

    protected override void DisposeInternal() {
        this._idxMapper.Dispose();
        this._vtxMapper.Dispose();

        this._currentTexture = null;
        
        foreach (RenderBuffer buf in this._renderBuffers) {
            buf.Dispose();
        }

        while (this._vtxBufferQueue.Count > 0) {
            WebGPUBuffer buf = this._vtxBufferQueue.Dequeue();
            buf.Dispose();
        }    
        while (this._idxBufferQueue.Count > 0) {
            WebGPUBuffer buf = this._idxBufferQueue.Dequeue();
            buf.Dispose();
        }
        
        this._workingBuffers.Clear();
        this._renderBuffers.Clear();
        this.FontRenderer = null;
    }
}