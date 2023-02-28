namespace Furball.Vixie.Backends.Shared.Tracy.Structs;

public struct GpuZoneBeginCallstackData {
    public ulong  SrcLoc;
    public int    Depth;
    public ushort QueryId;
    public byte   Context;
}