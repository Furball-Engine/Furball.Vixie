namespace Furball.Vixie.Backends.Shared.Tracy.Structs;

public unsafe struct SourceLocationData {
    public byte* Name;
    public byte* Function;
    public byte* File;

    public uint Line;
    public uint Color;
}