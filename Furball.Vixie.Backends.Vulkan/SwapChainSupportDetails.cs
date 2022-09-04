using Silk.NET.Vulkan;

namespace Furball.Vixie.Backends.Vulkan; 

public class SwapChainSupportDetails {
    public SurfaceCapabilitiesKHR SurfaceCapabilities;
    public SurfaceFormatKHR[]     Formats      = null!;
    public PresentModeKHR[]       PresentModes = null!;
}