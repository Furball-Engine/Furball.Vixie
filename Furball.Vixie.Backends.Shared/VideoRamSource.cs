namespace Furball.Vixie.Backends.Shared;

public abstract class VideoMemorySource {
    public abstract ulong TotalVideoMemory();
    public abstract ulong UsedVideoMemory();
}