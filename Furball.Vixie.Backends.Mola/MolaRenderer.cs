using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Silk.NET.Core.Native;

namespace Furball.Vixie.Backends.Mola;

public class MolaVixieRenderer : VixieRenderer {
    private readonly MolaBackend _backend;

    private readonly Queue<(IntPtr vertex, IntPtr index)> _freeBuffers = new Queue<(IntPtr vertex, IntPtr index)>();

    private struct RenderBatch {
        public unsafe Vertex* VertexPtr;
        public unsafe ushort* IndexPtr;

        public ushort VertexCount;
        public uint   IndexCount;
    }

    private static readonly unsafe int VtxBufSize = sizeof(Vertex) * 512;
    private static readonly unsafe int IdxBufSize = sizeof(Vertex) * 512 * 3;

    private RenderBatch _currentBatch;

    private readonly List<RenderBatch> _batches = new List<RenderBatch>();

    private bool _begun;

    public unsafe MolaVixieRenderer(MolaBackend backend) {
        this._backend = backend;

        this._currentBatch.VertexPtr = (Vertex*)SilkMarshal.Allocate(VtxBufSize);
        this._currentBatch.IndexPtr  = (ushort*)SilkMarshal.Allocate(IdxBufSize);
    }

    public override unsafe void Begin() {
        //If we are able to, reuse the first batch
        if (this._batches.Count != 0) {
            this._currentBatch = this._batches[0];

            this._currentBatch.VertexCount = 0;
            this._currentBatch.IndexCount  = 0;
        }

        //We use the first element if it exists, so start at the second element
        //Dump all remaining buffers into the free buffers list
        for (int i = 1; i < this._batches.Count; i++) {
            RenderBatch batch = this._batches[i];
            this._freeBuffers.Enqueue(((IntPtr)batch.VertexPtr, (IntPtr)batch.IndexPtr));
        }

        this._batches.Clear();

        this._begun = true;
    }

    public override void End() {
        this.FlushToBuffers(false);

        this._begun = false;
    }

    private unsafe void FlushToBuffers(bool createNew) {
        this._batches.Add(this._currentBatch);

        this._currentBatch = new RenderBatch {
            IndexPtr  = (ushort*)0,
            VertexPtr = (Vertex*)0
        };

        if (!createNew)
            return;

        if (this._freeBuffers.Count != 0) {
            (IntPtr vertex, IntPtr index) bufs = this._freeBuffers.Dequeue();
            this._currentBatch = new RenderBatch {
                VertexPtr = (Vertex*)bufs.vertex,
                IndexPtr  = (ushort*)bufs.index
            };

            return;
        }

        this._currentBatch = new RenderBatch {
            VertexPtr = (Vertex*)SilkMarshal.Allocate(VtxBufSize),
            IndexPtr  = (ushort*)SilkMarshal.Allocate(IdxBufSize)
        };
    }

    public override unsafe MappedData Reserve(ushort vertexCount, uint indexCount) {
        if (sizeof(Vertex) * vertexCount > VtxBufSize || sizeof(ushort) * indexCount > IdxBufSize)
            throw new Exception();

        if (sizeof(Vertex) * vertexCount + sizeof(Vertex) * this._currentBatch.VertexCount > VtxBufSize
         || sizeof(ushort) * indexCount  + sizeof(ushort) * this._currentBatch.IndexCount  > IdxBufSize)
            this.FlushToBuffers(true);

        this._currentBatch.VertexCount += vertexCount;
        this._currentBatch.IndexCount  += indexCount;

        return new MappedData(
            this._currentBatch.VertexPtr + this._currentBatch.VertexCount - vertexCount,
            this._currentBatch.IndexPtr  + this._currentBatch.IndexCount - indexCount,
            vertexCount,
            indexCount,
            (uint)(this._currentBatch.VertexCount - vertexCount)
        );
    }

    public override unsafe long GetTextureId(VixieTexture tex) {
        if (tex is not MolaTexture molaTexture)
            throw new ArgumentException($"Texture should be a {typeof(MolaTexture)}", nameof (tex));

        return (long)molaTexture.RenderBitmap;
    }

    public override unsafe void Draw() {
        Guard.Assert(!this._begun, "!this._begun");

        foreach (RenderBatch renderBatch in this._batches)
            Furball.Mola.Bindings.Mola.DrawOntoBitmap(this._backend.BitmapToRenderTo,
                                                      (Furball.Mola.Bindings.Vertex*)renderBatch.VertexPtr,
                                                      renderBatch.IndexPtr, renderBatch.IndexCount);
    }

    protected override unsafe void DisposeInternal() {
        if (this._begun)
            this.End();

        foreach (RenderBatch renderBatch in this._batches) {
            SilkMarshal.Free((IntPtr)renderBatch.VertexPtr);
            SilkMarshal.Free((IntPtr)renderBatch.IndexPtr);
        }
        foreach ((IntPtr vertex, IntPtr index) in this._freeBuffers) {
            SilkMarshal.Free(vertex);
            SilkMarshal.Free(index);
        }
    }
}