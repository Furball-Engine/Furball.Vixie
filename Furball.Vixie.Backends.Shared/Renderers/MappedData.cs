namespace Furball.Vixie.Backends.Shared.Renderers;

public unsafe class MappedData {
    public readonly Vertex* VertexPtr;
    public readonly ushort* IndexPtr;

    public readonly ushort VertexCount;
    public readonly uint   IndexCount;
    
    public MappedData(Vertex* vertexPtr, ushort* indexPtr, ushort vertexCount, uint indexCount) {
        this.VertexPtr   = vertexPtr;
        this.IndexPtr    = indexPtr;
        this.VertexCount = vertexCount;
        this.IndexCount  = indexCount;
    }
}