namespace Furball.Vixie.Backends.Shared.Tracy.Structs;

public struct TracyCZoneContext {
    public uint Id;
    public int  Active;

    /// <summary>
    /// not part of tracy, used to free the strings
    /// </summary>
    public SourceLocationData SourceLocationData;
}