namespace Furball.Vixie.Backends.Shared.Tracy.Structs;

public unsafe struct GpuContextNameData {
    public byte   Context;
    public byte*  Name;
    public ushort Len;
}