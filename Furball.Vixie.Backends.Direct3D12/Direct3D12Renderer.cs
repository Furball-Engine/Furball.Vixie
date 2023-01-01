using Furball.Vixie.Backends.Direct3D12.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Silk.NET.Direct3D12;

namespace Furball.Vixie.Backends.Direct3D12;

public unsafe class Direct3D12Renderer : VixieRenderer {
    private readonly Direct3D12Backend _backend;

    private const int QUADS_PER_BUFFER = 512;

    private CullFace _cullFace;

    private readonly Direct3D12BufferMapper _vtxMapper;
    private readonly Direct3D12BufferMapper _idxMapper;

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

    public override void Begin(CullFace cullFace = CullFace.CCW) {
        this._cullFace = cullFace;
    }

    public override void End() {

    }

    public override MappedData Reserve(ushort vertexCount, uint indexCount, VixieTexture tex) {
        return new MappedData();
    }

    public override void Draw() {
        //TODO: follow the cull mode
    }
    protected override void DisposeInternal() {

    }
}