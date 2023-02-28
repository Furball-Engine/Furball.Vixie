namespace Furball.Vixie.Backends.Shared.Tracy.Structs;

public struct GpuNewContextData {
    public long  GpuTime;
    public float Period;
    public byte  Context;
    public byte  Flags;
    public byte  Type;
}