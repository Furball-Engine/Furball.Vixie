namespace Furball.Vixie.Backends.Shared.Renderers;

public unsafe class MappedData {
    public readonly Vertex* VertexPtr;
    public readonly ushort* IndexPtr;

    public readonly ushort VertexCount;
    public readonly uint   IndexCount;

    public readonly uint IndexOffset;

    public readonly long TextureId;
    
    public MappedData(Vertex* vertexPtr, ushort* indexPtr, ushort vertexCount, uint indexCount, uint indexOffset, long textureId) {
        this.VertexPtr   = vertexPtr;
        this.IndexPtr    = indexPtr;
        this.VertexCount = vertexCount;
        this.IndexCount  = indexCount;
        this.IndexOffset = indexOffset;
        this.TextureId   = textureId;
    }
}