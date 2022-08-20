using System.Diagnostics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Veldrid;

namespace Furball.Vixie.Backends.Veldrid; 

public unsafe class RendererVeldrid : IRenderer {
    private readonly VeldridBackend _backend;
    
    private readonly VeldridBufferMapper _vtxMapper;
    private readonly VeldridBufferMapper _idxMapper;

    private const int QUAD_COUNT = 256;

    public RendererVeldrid(VeldridBackend backend) {
        this._backend = backend;

        this._vtxMapper = new VeldridBufferMapper(backend, QUAD_COUNT * 4, BufferUsage.VertexBuffer);
        this._idxMapper = new VeldridBufferMapper(backend, QUAD_COUNT * 6, BufferUsage.IndexBuffer);
    }
    
    public override void Begin() {
        this._vtxMapper.Map();
        this._idxMapper.Map();
    }
    
    public override void End() {
        this._vtxMapper.Unmap();
        this._idxMapper.Unmap();
    }

    public override long GetTextureId(VixieTexture tex) {
        return 0;
    }

    public override MappedData Reserve(ushort vertexCount, uint indexCount) {
        Debug.Assert(vertexCount != 0, "vertexCount != 0");
        Debug.Assert(indexCount  != 0, "indexCount != 0");
        
        Debug.Assert(vertexCount * sizeof(Vertex) < (int)this._vtxMapper.SizeInBytes, "vertexCount * sizeof(Vertex) < this._vtxMapper.SizeInBytes");
        Debug.Assert(indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes, "indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes");

        //TODO
        return null;
    }
    public override void Draw() {
        
    }
    protected override void DisposeInternal() {
        
    }
}