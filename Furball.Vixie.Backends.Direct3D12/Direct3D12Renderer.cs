using Furball.Vixie.Backends.Direct3D12.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Silk.NET.Direct3D12;

namespace Furball.Vixie.Backends.Direct3D12;

public unsafe class Direct3D12Renderer : VixieRenderer {
    private readonly Direct3D12Backend _backend;

    private const int QUADS_PER_BUFFER = 512;

    private CullFace _cullFace;

    private readonly Direct3D12BufferMapper _vtxMapper;
    private readonly Direct3D12BufferMapper _idxMapper;
    
    private readonly List<RenderBuffer> _renderBuffers = new List<RenderBuffer>();

    private Queue<Direct3D12Buffer> _vtxBufferQueue = new Queue<Direct3D12Buffer>();
    private Queue<Direct3D12Buffer> _idxBufferQueue = new Queue<Direct3D12Buffer>();
    
    private readonly List<RenderBuffer> _workingBuffers = new List<RenderBuffer>();

    private class RenderBuffer {
        public Direct3D12Buffer? Vtx;
        public Direct3D12Buffer? Idx;

        public uint IndexCount;

        public uint IndexOffset;
    }

    public Direct3D12Renderer(Direct3D12Backend backend) {
        this._backend = backend;

        this._vtxMapper =
            new Direct3D12BufferMapper(
                backend,
                (uint)(QUADS_PER_BUFFER * 4 * sizeof(Vertex)),
                ResourceStates.VertexAndConstantBuffer
            );
        this._idxMapper =
            new Direct3D12BufferMapper(
                backend,
                QUADS_PER_BUFFER * 6 * sizeof(ushort),
                ResourceStates.IndexBuffer
            );
    }

    private bool _isFirst = true;
    public override void Begin(CullFace cullFace = CullFace.CCW) {
        this._cullFace = cullFace;
        
        // Guard.EnsureNull(this._vtxMapper.Buffer, "this._vtxMapper._buffer");
        // Guard.EnsureNull(this._idxMapper.Buffer, "this._idxMapper._buffer");

        bool wasLastEmpty = this._renderBuffers.Count == 0;

        Direct3D12Buffer? lastVtx = null;
        Direct3D12Buffer? lastIdx = null;
        //Save all the buffers from the render queue
        foreach (RenderBuffer? x in this._renderBuffers) {
            Guard.EnsureNonNull(x.Vtx!, "x.Vtx");
            Guard.EnsureNonNull(x.Idx!, "x.Idx");

            if(lastVtx == x.Vtx && lastIdx == x.Idx)
                continue;

            this._vtxBufferQueue.Enqueue(x.Vtx!);
            this._idxBufferQueue.Enqueue(x.Idx!);

            lastVtx = x.Vtx;
            lastIdx = x.Idx;
            
            //We set these to null to ensure that they dont get disposed when `RenderBuffer.Dispose` is called by the
            //destructor
            x.Vtx = null;
            x.Idx = null;
        }
        //Clear the render buffer queue
        this._renderBuffers.Clear();

        if (this._vtxBufferQueue.Count == 0 || wasLastEmpty) {
            Guard.Assert(this._isFirst || wasLastEmpty);

            Direct3D12Buffer vtxBuf = this._vtxMapper.CopyMappedDataToNewBuffer();
            Direct3D12Buffer idxBuf = this._idxMapper.CopyMappedDataToNewBuffer();

            if (vtxBuf != null && idxBuf != null) {
                this._vtxBufferQueue.Enqueue(vtxBuf);
                this._idxBufferQueue.Enqueue(idxBuf);
            }

            this._isFirst = false;
        }
        else {
            Direct3D12Buffer vtxBuf = this._vtxBufferQueue.Dequeue();
            Direct3D12Buffer idxBuf = this._idxBufferQueue.Dequeue();

            this._vtxMapper.CopyMappedDataToExistingBufferAndReset(vtxBuf);
            this._idxMapper.CopyMappedDataToExistingBufferAndReset(idxBuf);

            if (vtxBuf != null && idxBuf != null) {
                this._vtxBufferQueue.Enqueue(vtxBuf);
                this._idxBufferQueue.Enqueue(idxBuf);
            }
        }

        this._indexCount   = 0;
        this._indexOffset  = 0;

        this._lastIndexOffset = 0;
        this._lastIndexCount  = 0;
    }

    private void DumpToBuffers() {
        if (this._indexCount == 0)
            return;

        Direct3D12Buffer vtx;
        Direct3D12Buffer idx;
        if (this._vtxBufferQueue.Count > 0) {
            vtx = this._vtxBufferQueue.Dequeue();
            idx = this._idxBufferQueue.Dequeue();
            
            this._vtxMapper.CopyMappedDataToExistingBufferAndReset(vtx);
            this._idxMapper.CopyMappedDataToExistingBufferAndReset(idx);
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
                    Vtx          = vtx,
                    Idx          = idx,
                    IndexCount   = this._indexCount,
                    IndexOffset  = this._lastIndexOffset
                }
            );
        }
        else {
            if (this._indexCount != this._lastIndexCount) {
                RenderBuffer buf = new RenderBuffer {
                    IndexOffset = this._lastIndexOffset,
                    IndexCount  = this._indexCount - this._lastIndexCount
                };

                this._lastIndexCount  = this._indexCount;
                this._lastIndexOffset = this._indexCount;
            
                this._workingBuffers.Add(buf); 
            }
            
            foreach (RenderBuffer workingBuffer in this._workingBuffers) {
                workingBuffer.Vtx = vtx;
                workingBuffer.Idx = idx;

                this._renderBuffers.Add(workingBuffer);
            }

            this._workingBuffers.Clear();
        }

        this._usedTextures = 0;
        this._indexOffset  = 0;
        this._indexCount   = 0;

        this._lastIndexCount  = 0;
        this._lastIndexOffset = 0;
    }
    
    public override void End() {
        this.DumpToBuffers();
    }

    private ushort _indexOffset;
    private uint   _indexCount;
    private int    _usedTextures;

    private uint _lastIndexOffset = 0;
    private uint _lastIndexCount  = 0;
    
    private int _reserveRecursionCount = 0;
    public override MappedData Reserve(ushort vertexCount, uint indexCount, VixieTexture vixieTex) {
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

        if (vixieTex is not Direct3D12Texture tex)
            throw new Exception($"Texture is not of type {nameof(Direct3D12Texture)}");
        
        void* vtx = this._vtxMapper.Reserve((nuint)(vertexCount * sizeof(Vertex)));
        void* idx = this._idxMapper.Reserve(indexCount * sizeof(ushort));

        long textureId = ((long)tex.SamplerHeapSlot << 32) + tex.SRVHeapSlot;
        
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

    public override void Draw() {
        //TODO: follow the cull mode

        ID3D12DescriptorHeap** heaps = stackalloc ID3D12DescriptorHeap*[2];

        heaps[0] = this._backend.CbvSrvUavHeap.Heap;
        heaps[1] = this._backend.SamplerHeap.Heap;
        
        //Bind the 2 descriptor heaps
        this._backend.CommandList.SetDescriptorHeaps(2, heaps);
        
        foreach (RenderBuffer buf in this._renderBuffers) {
            this._backend.CommandList.IASetVertexBuffers(0, 1, buf.Vtx!.VertexBufferView);
            this._backend.CommandList.IASetIndexBuffer(buf.Idx!.IndexBufferView);


            this._backend.CommandList.DrawIndexedInstanced(buf.IndexCount, 1, buf.IndexOffset, 0, 0);
        }
    }
    
    protected override void DisposeInternal() {
        this._vtxMapper.Dispose();
        this._idxMapper.Dispose();
        
        this._renderBuffers.Clear();
        this._idxBufferQueue.Clear();
        this._vtxBufferQueue.Clear();
    }
}