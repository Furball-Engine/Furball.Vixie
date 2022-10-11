using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Backends.Vulkan.Abstractions;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using Kettu;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using Image=Silk.NET.Vulkan.Image;

namespace Furball.Vixie.Backends.Vulkan;

public unsafe class VulkanBackend : GraphicsBackend {
    private static ulong RateDeviceInfo(PhysicalDeviceInfo info) {
        // NOTE: This method is static to prevent you querying anything extra.
        // If information should be used during rating it should be on the device info,
        // as it should be used to put a limit on something else somewhere else for sure.

        // NOTE: Do not check extensions existance here. Each existing optional extension is worth 100 points, as defined below.

        ulong count = 0;

        count += info.Properties.DeviceType switch {

            PhysicalDeviceType.Other         => 0ul,
            PhysicalDeviceType.IntegratedGpu => 100ul,
            PhysicalDeviceType.DiscreteGpu   => 1000ul,
            PhysicalDeviceType.VirtualGpu    => 200ul,
            PhysicalDeviceType.Cpu           => 10ul,
            _                                => 0ul
        };

        return count;
    }

    private PhysicalDeviceInfo QueryPhysicalDeviceInfos(PhysicalDevice handle) {
        this._vk.GetPhysicalDeviceProperties(handle, out PhysicalDeviceProperties properties);
        this._vk.GetPhysicalDeviceFeatures(handle, out PhysicalDeviceFeatures features);
        this._vk.GetPhysicalDeviceMemoryProperties(handle, out PhysicalDeviceMemoryProperties memoryProperties);

        uint queueFamilyCount = 0;
        this._vk.GetPhysicalDeviceQueueFamilyProperties(handle, ref queueFamilyCount, null);

        QueueFamilyProperties[] queueFamilyProperties = new QueueFamilyProperties[queueFamilyCount];

        fixed (QueueFamilyProperties* queueFamilyPropertiesPtr = queueFamilyProperties)
            this._vk.GetPhysicalDeviceQueueFamilyProperties(handle, ref queueFamilyCount, queueFamilyPropertiesPtr);

        QueueInfo[] queueInfos = queueFamilyProperties
            .SelectMany((x, i) => Enumerable.Range(0, (int)x.QueueCount)
                .Select(j => new QueueInfo(i, j, default, x)))
            .ToArray();

        string? name = SilkMarshal.PtrToString((nint)properties.DeviceName);

        return new PhysicalDeviceInfo(
            handle,
            name!,
            features,
            properties,
            queueInfos.AsMemory().Slice(0, (int)queueFamilyCount),
            memoryProperties
        );
    }

    private Instance                _instance;
    private Vk                      _vk                 = null!;
    private IView                   _view               = null!;
    private PhysicalDeviceInfo      _physicalDeviceInfo = null!;
    private Device                  _device;
    private SurfaceKHR              _surface;
    private bool                    _debug;
    private DebugUtilsMessengerEXT? _messenger             = null;
    private QueuePool               _queuePool             = null!;
    private QueueInfo               _presentationQueueInfo = null!;

    private SwapchainKHR _swapchain;
    private Format       _swapchainImageFormat;
    private Extent2D     _swapchainExtent;
    private Image[]      _swapChainImages     = null!;
    private ImageView[]  _swapChainImageViews = null!;

    // Extensions:
    private ExtDebugUtils  _extDebugUtils = null!;
    private KhrSurface     _vkSurface     = null!;
    private KhrSwapchain   _vkSwapchain   = null!;
    private ExtToolingInfo _vkToolingInfo = null!;

    internal Device GetDevice() => this._device;
    internal Vk GetVk() => this._vk;

    private ExtensionSet GetInstanceExtensions() {
        List<string> requiredExtensions = new();
        List<string> optionalExtensions = new();

        Debug.Assert(this._view.VkSurface is not null);
        requiredExtensions.AddRange(SilkMarshal.PtrToStringArray(
            (nint)this._view.VkSurface!.GetRequiredExtensions(
                out uint windowExtensionCount),
            (int)windowExtensionCount));

        requiredExtensions.Add(KhrSurface.ExtensionName);

        if (_debug) {
            optionalExtensions.Add(ExtDebugUtils.ExtensionName);
        }

        optionalExtensions.Add(ExtToolingInfo.ExtensionName);

        //NOTE: this is where we add more optional or required extensions

        return new ExtensionSet(requiredExtensions, optionalExtensions);
    }

    private ExtensionSet GetDeviceExtensions() {
        List<string> requiredExtensions = new() {
            KhrSwapchain.ExtensionName
        };

        //NOTE: we pass in a blank list beacuse we dont have any optional extensions yet
        return new ExtensionSet(requiredExtensions, new List<string>());
    }

    private static bool TryExtractUsedExtensions(
        ExtensionSet extensionSet,
        Func<string, bool> verifyExtension,
        out int count,
        /* [NotNullWhen(true)] */
        out GlobalMemory? memory
    ) {
        HashSet<string> usedExtensions = new();

        bool allRequiredPresent = true;

        foreach (string? required in extensionSet.RequiredExtensions) {
            if (!verifyExtension(required)) {
                Logger.Log("Required Extension not Present: \"" + required + "\"",
                    LoggerLevelVulkan.InstanceWarning);
                allRequiredPresent = false;
            }
            else {
                usedExtensions.Add(required);
            }
        }
        if (!allRequiredPresent) {
            memory = null;
            count  = 0;
            return false;
        }

        foreach (string? optional in extensionSet.OptionalExtensions) {
            if (!verifyExtension(optional)) {
                Logger.Log("Optional Extension not Present: \"" + optional + "\"", LoggerLevelVulkan.InstanceInfo);
            }
            else {
                usedExtensions.Add(optional);
            }
        }

        memory = SilkMarshal.StringArrayToMemory(usedExtensions.ToArray());
        count  = usedExtensions.Count;

        return true;
    }

    private bool TryCreateInstance(out Instance instance) {
        // TODO: Allow non-conformant devices using new loader extension (2022)

        ExtensionSet extensionSet = GetInstanceExtensions();

        if (!TryExtractUsedExtensions(extensionSet,
                this._vk.IsInstanceExtensionPresent,
                out int extensionCount,
                out GlobalMemory? extensionMemory)) {
            Logger.Log("Could not extract instance extensions", LoggerLevelVulkan.InstanceError);
            instance = default;
            return false;
        }

        AssemblyName  entryAssemblyname  = Assembly.GetEntryAssembly()!.GetName();
        GlobalMemory? applicationNameMem = SilkMarshal.StringToMemory(entryAssemblyname.FullName);
        GlobalMemory? engineNameMem      = SilkMarshal.StringToMemory("Furball.Vixie");

        ApplicationInfo appInfo = new() {
            SType              = StructureType.ApplicationInfo,
            PApplicationName   = applicationNameMem.AsPtr<byte>(),
            ApplicationVersion = (Version32)entryAssemblyname.Version,
            PEngineName        = engineNameMem.AsPtr<byte>(),
            EngineVersion      = new Version32(1, 0, 0),
            ApiVersion         = Vk.Version11
        };

        InstanceCreateInfo createInfo = new() {
            SType                   = StructureType.InstanceCreateInfo, PApplicationInfo = &appInfo,
            EnabledExtensionCount   = (uint)extensionCount,
            PpEnabledExtensionNames = (byte**)(extensionMemory?.Handle ?? 0),
        };

        Result result = this._vk.CreateInstance(&createInfo, null, out instance);

        extensionMemory?.Dispose();

        if (result != Result.Success) {
            Logger.Log($"Creating vulkan instance failed! Vulkan Error: {result}", LoggerLevelVulkan.InstanceError);
        }

        return true;
    }

    private bool TryGetPhysicalDevice(ExtensionSet extensionSet, out PhysicalDeviceInfo? selectedDevice) {
        uint deviceCount;
        this._vk.EnumeratePhysicalDevices(this._instance, &deviceCount, null);

        if (deviceCount == 0) {
            Logger.Log("No Vulkan Devices found!", LoggerLevelVulkan.InstanceError);
            selectedDevice = null;
            return false;
        }

        PhysicalDevice[] devices = new PhysicalDevice[deviceCount];
        this._vk.EnumeratePhysicalDevices(this._instance, &deviceCount, devices);

        IEnumerable<PhysicalDeviceInfo> infos = devices.Take((int)deviceCount)
            .Select(this.QueryPhysicalDeviceInfos)
            .Where(x => {
                foreach (string? e in extensionSet.RequiredExtensions) {
                    if (!this._vk.IsDeviceExtensionPresent(x.Handle, e)) {
                        Logger.Log($"Rejecting {x.Name} as it does not support required extension {e}",
                            LoggerLevelVulkan.InstanceInfo);
                        return false;
                    }
                }

                return true;
            })
            .Where(x => {
                foreach (QueueInfo? v in x.Queues.Span) {
                    this._vkSurface.GetPhysicalDeviceSurfaceSupport(x.Handle, (uint)v.QueueFamilyIndex, this._surface,
                        out Bool32 supported);

                    if (supported)
                        return true;
                }
                Logger.Log($"Rejecting {x.Name} as it does not support presentation",
                    LoggerLevelVulkan.InstanceInfo);

                return false;
            });

        PhysicalDeviceInfo? bestInfo = null;
        try {
            //Get device rating, aswell as add 100 points for every extension we use and the device supports
            (PhysicalDeviceInfo x, ulong)[] rated = infos
                .Select(x => (x,
                    RateDeviceInfo(x) + (ulong)extensionSet.OptionalExtensions.Sum(e =>
                        this._vk.IsDeviceExtensionPresent(x.Handle, e) ? 100 : 0)))
                .ToArray();

            ulong maxV = ulong.MinValue;

            foreach ((PhysicalDeviceInfo x, ulong rating) in rated) {
                Logger.Log($"Rated {x.Name} as {rating}", LoggerLevelVulkan.InstanceInfo);

                if (rating > maxV) {
                    bestInfo = x;
                }
            }
        }
        catch {
            selectedDevice = null;
            return false;
        }

        if (bestInfo is null) {
            selectedDevice = null;
            return false;
        }

        selectedDevice = bestInfo;

        return true;
    }

    private bool TryCreateLogicalDevice(
        ExtensionSet extensionSet,
        out Device device,
        out QueuePool? queuePool,
        out ReadOnlyMemory<QueueInfo> queueInfos
    ) {

        // TODO: Allocate some smart priority stuffs
        // I can't be bothered.... This is 30x 1.0f, I doubt there are any devices with that many queues in one family
        float* prio = stackalloc float[] {
            1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f,
            1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f
        };

        ReadOnlySpan<QueueInfo> physicalQueues = this._physicalDeviceInfo.Queues.Span;
        DeviceQueueCreateInfo*  queueCreates   = stackalloc DeviceQueueCreateInfo[physicalQueues.Length];

        int queueCreateCount = -1;

        foreach (QueueInfo q in physicalQueues) {
            if (q.QueueFamilyIndex > queueCreateCount)
                queueCreateCount = q.QueueFamilyIndex;

            queueCreates[q.QueueFamilyIndex] = new DeviceQueueCreateInfo(
                queueCount: q.FamilyProperties.QueueCount,
                queueFamilyIndex: (uint)q.QueueFamilyIndex,
                pQueuePriorities: prio);
        }

        PhysicalDeviceFeatures enabledFeature = new();

        if (!TryExtractUsedExtensions(extensionSet,
                e => _vk.IsDeviceExtensionPresent(this._physicalDeviceInfo.Handle, e),
                out int enabledExtensionCount,
                out GlobalMemory? extensionMem)) {
            device     = default;
            queuePool  = null;
            queueInfos = default;
            return false;
        }

        this._vk.CreateDevice(this._physicalDeviceInfo.Handle,
            new DeviceCreateInfo(queueCreateInfoCount: (uint)(queueCreateCount + 1),
                pQueueCreateInfos: queueCreates,
                enabledExtensionCount: (uint)enabledExtensionCount,
                ppEnabledExtensionNames: (byte**)extensionMem!.Handle,
                pEnabledFeatures: &enabledFeature),
            null,
            out device);

        QueueInfo[] newQueueInfos = new QueueInfo[this._physicalDeviceInfo.Queues.Length];

        for (int i = 0; i < newQueueInfos.Length; i++) {
            this._vk.GetDeviceQueue(device,
                (uint)physicalQueues[i].QueueFamilyIndex,
                (uint)physicalQueues[i].QueueIndex,
                out Queue queue);

            newQueueInfos[i] = new QueueInfo(physicalQueues[i].QueueFamilyIndex,
                physicalQueues[i].QueueIndex,
                queue,
                physicalQueues[i].FamilyProperties);
        }

        queueInfos = newQueueInfos;
        queuePool  = new QueuePool(queueInfos);
        return true;
    }

    public override void Initialize(IView window, IInputContext inputContext) {
        this._view = window;

        if (this._view.VkSurface == null) {
            throw new NotSupportedException("The view was created without Vulkan support");
        }

        _vk = Vk.GetApi();

        _debug = window.API.Flags.HasFlag(ContextFlags.Debug);

        if (!TryCreateInstance(out _instance)) {
            Logger.Log("Failed to create instance", LoggerLevelVulkan.InstanceFatal);
            return;
        }

        Logger.Log($"Created Vulkan Instance {this._instance.Handle}", LoggerLevelVulkan.InstanceInfo);

        // Query instance extensions
        if (!this._vk.TryGetInstanceExtension(_instance, out this._vkSurface)) {
            throw new Exception("Impossible");
        }

        this._vk.TryGetInstanceExtension(this._instance, out this._extDebugUtils);
        this._vk.TryGetInstanceExtension(this._instance, out this._vkToolingInfo);

        this._surface = this._view.VkSurface!.Create<AllocationCallbacks>(_instance.ToHandle(), null).ToSurface();

        ExtensionSet deviceExtensionSet = this.GetDeviceExtensions();

        if (!TryGetPhysicalDevice(deviceExtensionSet, out this._physicalDeviceInfo!)) {
            Logger.Log("Failed to pick physical device", LoggerLevelVulkan.InstanceFatal);
            return;
        }

        PhysicalDeviceProperties
            physicalDeviceProperties = this._physicalDeviceInfo.Properties; // to access fixed-size buffers
        Logger.Log($"Picked vulkan device {SilkMarshal.PtrToString((nint)physicalDeviceProperties.DeviceName)}",
            LoggerLevelVulkan.InstanceInfo);

        if (this._vkToolingInfo is not null) {
            uint toolCount = 0;
            this._vkToolingInfo.GetPhysicalDeviceToolProperties(this._physicalDeviceInfo.Handle,
                ref toolCount,
                null);

            PhysicalDeviceToolProperties* toolingProperties = stackalloc PhysicalDeviceToolProperties[(int)toolCount];


            this._vkToolingInfo.GetPhysicalDeviceToolProperties(this._physicalDeviceInfo.Handle,
                ref toolCount,
                toolingProperties);

            for (int i = 0; i < toolCount; i++) {
                PhysicalDeviceToolProperties tool = toolingProperties[i];
                Logger.Log(
                    $"Tool {SilkMarshal.PtrToString((nint)tool.Name)} {SilkMarshal.PtrToString((nint)tool.Version)} purposes: {tool.Purposes}\n" +
                    $"\"{SilkMarshal.PtrToString((nint)tool.Description)}\"",
                    LoggerLevelVulkan.InstanceInfo);
            }
        }

        if (!TryCreateLogicalDevice(deviceExtensionSet,
                out this._device,
                out this._queuePool!,
                out ReadOnlyMemory<QueueInfo> queueInfos)) {
            Logger.Log("Failed to create logical device", LoggerLevelVulkan.InstanceFatal);
            return;
        }

        // Copy, add final queue infos with handles
        this._physicalDeviceInfo = new PhysicalDeviceInfo(this._physicalDeviceInfo.Handle,
            this._physicalDeviceInfo.Name,
            this._physicalDeviceInfo.Features,
            this._physicalDeviceInfo.Properties,
            queueInfos,
            this._physicalDeviceInfo.MemoryProperties);

        Logger.Log(
            $"Created Physical Device {this._device} with associated queue pool. Using {queueInfos.Length} queues.",
            LoggerLevelVulkan.InstanceInfo);

        if (!TryGetPresentationQueue(out QueueInfo? presentQueue)) {
            Logger.Log(
                "Could not retrieve presentation queue. This should be prohibited by earlier code... Did you unplug your monitor?",
                LoggerLevelVulkan.InstanceFatal);
            return;
        }

        this._presentationQueueInfo = this._queuePool.GetReferenceTo(presentQueue!);

        Logger.Log(
            $"Picked Presentation Queue from queue family {presentQueue!.QueueFamilyIndex} and registered with pool",
            LoggerLevelVulkan.InstanceInfo);

        if (!TryCreateSwapchain())
            throw new Exception("Failed to create SwapChain!");

        this.TryCreateSwapchainImageViews();
    }

    private bool TryCreateSwapchain() {
        this._vk.TryGetDeviceExtension(this._instance, this._device, out this._vkSwapchain);

        //Check SwapChain support details
        SwapChainSupportDetails swapChainSupportDetails = QuerySwapChainSupportDetails(this._physicalDeviceInfo.Handle);

        //pick best settings
        SurfaceFormatKHR surfaceFormat;
        PresentModeKHR   presentMode = PresentModeKHR.ImmediateKhr;
        Extent2D         extent;

        PresentModeKHR[] presentModeTryList = new PresentModeKHR[] {
            PresentModeKHR.FifoKhr,
            PresentModeKHR.MailboxKhr,
            PresentModeKHR.ImmediateKhr,
            PresentModeKHR.SharedContinuousRefreshKhr,
            PresentModeKHR.SharedDemandRefreshKhr,
            PresentModeKHR.FifoRelaxedKhr,
            PresentModeKHR.FifoKhr
        };

        surfaceFormat = swapChainSupportDetails.Formats[0];

        foreach (PresentModeKHR presentModeTry in presentModeTryList) {
            if (swapChainSupportDetails.PresentModes.Contains(presentModeTry)) {
                presentMode = presentModeTry;
                break;
            }
        }

        extent = new Extent2D((uint)this._view.FramebufferSize.X, (uint)this._view.FramebufferSize.Y);

        extent.Width = extent.Width.Clamp(swapChainSupportDetails.SurfaceCapabilities.MinImageExtent.Width,
            swapChainSupportDetails.SurfaceCapabilities.MaxImageExtent.Width);
        extent.Height = extent.Height.Clamp(swapChainSupportDetails.SurfaceCapabilities.MinImageExtent.Height,
            swapChainSupportDetails.SurfaceCapabilities.MaxImageExtent.Height);

        uint bufferCount = swapChainSupportDetails.SurfaceCapabilities.MinImageCount + 1;

        if (swapChainSupportDetails.SurfaceCapabilities.MaxImageCount > 0 &&
            bufferCount > swapChainSupportDetails.SurfaceCapabilities.MaxImageCount)
            bufferCount = swapChainSupportDetails.SurfaceCapabilities.MaxImageCount;

        SwapchainCreateInfoKHR swapchainCreateInfo = new() {
            SType            = StructureType.SwapchainCreateInfoKhr,
            Surface          = this._surface,
            MinImageCount    = bufferCount,
            ImageFormat      = surfaceFormat.Format,
            ImageColorSpace  = surfaceFormat.ColorSpace,
            ImageExtent      = extent,
            ImageArrayLayers = 1,
            ImageUsage       = ImageUsageFlags.ColorAttachmentBit
        };

        QueueInfo? graphicsQueue = this._queuePool.NextGraphicsQueue();
        
        Guard.EnsureNonNull(graphicsQueue, nameof(graphicsQueue));

        uint* queueFamilyIndicies = stackalloc uint[] {
            (uint)this._presentationQueueInfo.QueueFamilyIndex, (uint)graphicsQueue!.QueueFamilyIndex
        };

        if (graphicsQueue.QueueFamilyIndex != _presentationQueueInfo.QueueFamilyIndex) {
            swapchainCreateInfo.ImageSharingMode      = SharingMode.Concurrent;
            swapchainCreateInfo.QueueFamilyIndexCount = 2;
            swapchainCreateInfo.PQueueFamilyIndices   = queueFamilyIndicies;
        }
        else {
            swapchainCreateInfo.ImageSharingMode      = SharingMode.Exclusive;
            swapchainCreateInfo.QueueFamilyIndexCount = 0;
            swapchainCreateInfo.PQueueFamilyIndices   = null;
        }

        swapchainCreateInfo.PreTransform   = swapChainSupportDetails.SurfaceCapabilities.CurrentTransform;
        swapchainCreateInfo.CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr;
        swapchainCreateInfo.PresentMode    = presentMode;
        swapchainCreateInfo.Clipped        = true;

        SwapchainKHR swapChain;

        Result result = this._vkSwapchain.CreateSwapchain(this._device, swapchainCreateInfo, null, out swapChain);

        if (result != Result.Success) {
            Logger.Log("Swapchain creaation resulted in failure!", LoggerLevelVulkan.InstanceError);
            return false;
        }

        this._swapchain            = swapChain;
        this._swapchainImageFormat = surfaceFormat.Format;
        this._swapchainExtent      = extent;

        uint imageCount = 0;
        this._vkSwapchain.GetSwapchainImages(this._device, swapChain, ref imageCount, null);

        Image[] swapChainImages = new Image[imageCount];
        this._vkSwapchain.GetSwapchainImages(this._device, swapChain, &imageCount, swapChainImages);

        this._swapChainImages = swapChainImages;

        return true;
    }

    private void TryCreateSwapchainImageViews() {
        ImageView[] swapchainImageViews = new ImageView[this._swapChainImages.Length];

        for (var i = 0; i < this._swapChainImages.Length; i++) {
            Image image = this._swapChainImages[i];

            ImageViewCreateInfo imageViewCreateInfo = new() {
                SType    = StructureType.ImageViewCreateInfo,
                Image    = image,
                ViewType = ImageViewType.Type2D,
                Format   = this._swapchainImageFormat,
                Components = new ComponentMapping {
                    R = ComponentSwizzle.Identity,
                    G = ComponentSwizzle.Identity,
                    B = ComponentSwizzle.Identity,
                    A = ComponentSwizzle.Identity,
                },
                SubresourceRange = new ImageSubresourceRange {
                    AspectMask     = ImageAspectFlags.ColorBit,
                    BaseMipLevel   = 0,
                    LevelCount     = 1,
                    BaseArrayLayer = 0,
                    LayerCount     = 1
                }
            };

            if (this._vk.CreateImageView(this._device, imageViewCreateInfo, null, out swapchainImageViews[i]) !=
                Result.Success)
                throw new Exception("Failed to create SwapChain Image Views!");
        }

        this._swapChainImageViews = swapchainImageViews;

        this.TestStuff();
    }

    private SwapChainSupportDetails QuerySwapChainSupportDetails(PhysicalDevice device) {
        SwapChainSupportDetails details = new();

        this._vkSurface.GetPhysicalDeviceSurfaceCapabilities(device, this._surface, out details.SurfaceCapabilities);

        uint formatCount = 0;
        this._vkSurface.GetPhysicalDeviceSurfaceFormats(device, this._surface, ref formatCount, null);

        if (formatCount != 0) {
            details.Formats = new SurfaceFormatKHR[formatCount];
            Span<SurfaceFormatKHR> spanFormats = new(details.Formats);

            this._vkSurface.GetPhysicalDeviceSurfaceFormats(device, this._surface, &formatCount, spanFormats);
        }

        uint presentModeCount = 0;
        this._vkSurface.GetPhysicalDeviceSurfacePresentModes(device, this._surface, ref presentModeCount, null);

        if (presentModeCount != 0) {
            details.PresentModes = new PresentModeKHR[presentModeCount];
            Span<PresentModeKHR> spanPresentModes = new(details.PresentModes);

            this._vkSurface.GetPhysicalDeviceSurfacePresentModes(device, this._surface, &presentModeCount,
                spanPresentModes);
        }

        return details;
    }

    private bool TryGetPresentationQueue(out QueueInfo? presentationQueueInfo) {
        ReadOnlySpan<QueueInfo> physicalQueues = this._physicalDeviceInfo.Queues.Span;

        foreach (QueueInfo? q in physicalQueues) {
            this._vkSurface.GetPhysicalDeviceSurfaceSupport(this._physicalDeviceInfo.Handle,
                (uint)q.QueueFamilyIndex,
                this._surface,
                out Bool32 supported);
            if (supported) {
                presentationQueueInfo = q;
                return true;
            }
        }
        presentationQueueInfo = null;

        return false;
    }
    
    public override void Cleanup() {
        this._presentationQueueInfo.Dispose();
        this._queuePool.Dispose();

        this._vk.DestroyDevice(this._device, null);

        if (this._messenger.HasValue)
            this._extDebugUtils.DestroyDebugUtilsMessenger(this._instance, this._messenger.Value, null);

        this._vkSurface.DestroySurface(this._instance, this._surface, null);
        this._vkSwapchain.DestroySwapchain(this._device, this._swapchain, null);

        foreach (ImageView imageView in this._swapChainImageViews) {
            this._vk.DestroyImageView(this._device, imageView, null);
        }

        this._vk.DestroyPipelineLayout(this._device, this._pipelineLayout, null);

        this._vk.DestroyInstance(this._instance, null);
    }

    private Shader         _vertexShader   = null!;
    private Shader         _fragmentShader = null!;
    private PipelineLayout _pipelineLayout;

    private void TestStuff() {
        this._vertexShader = new Shader(this,
            ResourceHelpers.GetByteResource("ShaderCode/Compiled/HardcodedTriangle/VertexShader.spv", typeof(VulkanBackend)),
            ShaderStageFlags.VertexBit, "main");
        this._fragmentShader = new Shader(this,
            ResourceHelpers.GetByteResource("ShaderCode/Compiled/HardcodedTriangle/FragmentShader.spv", typeof(VulkanBackend)),
            ShaderStageFlags.FragmentBit, "main");

        ShaderStagePipelineCreateInfo vertCreateInfo = this._vertexShader.GetPipelineCreateInfo();
        ShaderStagePipelineCreateInfo fragCreateInfo = this._fragmentShader.GetPipelineCreateInfo();
        PipelineShaderStageCreateInfo* shaderStages = stackalloc PipelineShaderStageCreateInfo[] {
            vertCreateInfo.Info, fragCreateInfo.Info
        };

        PipelineVertexInputStateCreateInfo vertexInputInfo = new() {
            SType                           = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount   = 0,
            PVertexBindingDescriptions      = null,
            VertexAttributeDescriptionCount = 0,
            PVertexAttributeDescriptions    = null,
        };

        PipelineInputAssemblyStateCreateInfo inputAssembler = new() {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo, Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false,
        };

        Viewport viewport = new() {
            X        = 0,
            Y        = 0,
            Width    = this._swapchainExtent.Width,
            Height   = this._swapchainExtent.Height,
            MinDepth = 0,
            MaxDepth = 1
        };

        Rect2D scissorRectangle = new() {
            Offset = new Offset2D(0, 0), Extent = this._swapchainExtent
        };

        DynamicState* dynamicStates = stackalloc DynamicState[] {
            DynamicState.Viewport, DynamicState.Scissor
        };

        PipelineDynamicStateCreateInfo dynamicState = new() {
            SType = StructureType.PipelineDynamicStateCreateInfo, DynamicStateCount = 2, PDynamicStates = dynamicStates
        };

        PipelineViewportStateCreateInfo viewportState = new() {
            SType         = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            ScissorCount  = 1,
            PViewports    = &viewport,
            PScissors     = &scissorRectangle,
        };

        PipelineRasterizationStateCreateInfo rasterizerState = new() {
            SType                   = StructureType.PipelineRasterizationStateCreateInfo,
            RasterizerDiscardEnable = false,
            PolygonMode             = PolygonMode.Fill,
            LineWidth               = 1.0f,
            CullMode                = CullModeFlags.BackBit,
            FrontFace               = FrontFace.Clockwise,
            DepthBiasEnable         = false,
            DepthBiasConstantFactor = 0.0f,
            DepthBiasClamp          = 0.0f,
            DepthBiasSlopeFactor    = 0.0f,
        };

        PipelineMultisampleStateCreateInfo multisampleState = new(
            sampleShadingEnable: false,
            rasterizationSamples: SampleCountFlags.Count1Bit,
            minSampleShading: 1.0f,
            pSampleMask: null,
            alphaToCoverageEnable: false,
            alphaToOneEnable: false
        );

        PipelineColorBlendAttachmentState blendState = new(
            colorWriteMask: ColorComponentFlags.RBit |
                            ColorComponentFlags.GBit |
                            ColorComponentFlags.BBit |
                            ColorComponentFlags.ABit,
            blendEnable: true,
            srcColorBlendFactor: BlendFactor.SrcAlpha,
            dstColorBlendFactor: BlendFactor.OneMinusSrcAlpha,
            colorBlendOp: BlendOp.Add,
            srcAlphaBlendFactor: BlendFactor.One,
            dstAlphaBlendFactor: BlendFactor.OneMinusSrcAlpha,
            alphaBlendOp: BlendOp.Add
        );

        float* blendConstantArray = stackalloc float[] {
            0.0f, 0.0f, 0.0f, 0.0f
        };

        PipelineColorBlendStateCreateInfo colorBlendState = new(
            // logicOp: LogicOp.Copy,
            attachmentCount: 1,
            // logicOpEnable: true,
            pAttachments: &blendState
        );
        //todo: figure out why the normal way of doing it causes weird compile errors
        colorBlendState.BlendConstants[0] = blendConstantArray[0];
        colorBlendState.BlendConstants[1] = blendConstantArray[1];
        colorBlendState.BlendConstants[2] = blendConstantArray[2];
        colorBlendState.BlendConstants[3] = blendConstantArray[3];

        PipelineLayoutCreateInfo pipelineLayoutInfo = new(
            setLayoutCount: 0,
            pSetLayouts: null,
            pushConstantRangeCount: 0,
            pPushConstantRanges: null
        );

        Result result =
            this._vk.CreatePipelineLayout(this._device, &pipelineLayoutInfo, null, out this._pipelineLayout);

        if (result != Result.Success)
            throw new Exception("Failed to create Pipeline layout!");

        AttachmentReference attachmentReference = new(0, ImageLayout.ColorAttachmentOptimal);
        SubpassDescription subpass = new(SubpassDescriptionFlags.None, PipelineBindPoint.Graphics, 0,
            null, 1, &attachmentReference);
        
        AttachmentDescription attachmentDescription = new(AttachmentDescriptionFlags.None, Format.R8G8B8A8Unorm, SampleCountFlags.Count1Bit, AttachmentLoadOp.DontCare, AttachmentStoreOp.DontCare, AttachmentLoadOp.DontCare, AttachmentStoreOp.DontCare, ImageLayout.ColorAttachmentOptimal, ImageLayout.ColorAttachmentOptimal);
        RenderPassCreateInfo passCreateInfo = new(
            flags: RenderPassCreateFlags.None,
            attachmentCount: 1, 
            pAttachments: &attachmentDescription,
            subpassCount: 1,
            pSubpasses: &subpass
        );

        RenderPass pass = new();
        this._vk.CreateRenderPass(this._device, &passCreateInfo, null, &pass);
        
        GraphicsPipelineCreateInfo pipelineInfo = new(
            stageCount: 2,
            pStages: shaderStages,
            pVertexInputState: &vertexInputInfo,
            pInputAssemblyState: &inputAssembler,
            pViewportState: &viewportState,
            pRasterizationState: &rasterizerState,
            pMultisampleState: &multisampleState,
            pDepthStencilState: null,
            pColorBlendState: &colorBlendState,
            pDynamicState: &dynamicState,
            layout: this._pipelineLayout,
            renderPass: pass
        );

        PipelineCacheCreateInfo cacheCreateInfo = new(flags: PipelineCacheCreateFlags.None);

        PipelineCache cache = new();
        result = this._vk.CreatePipelineCache(this._device, &cacheCreateInfo, null, &cache);

        if (result != Result.Success)
            throw new Exception("Unable to create pipeline cache!");

        Pipeline pipeline = new();
        result = this._vk.CreateGraphicsPipelines(this._device, cache, 1, &pipelineInfo, null, &pipeline);

        if (result != Result.Success)
            throw new Exception("Unable to create graphics pipeline");
    }


    public override void HandleFramebufferResize(int width, int height) {}

    public override VixieRenderer CreateRenderer() => throw new NotImplementedException();


    public override int QueryMaxTextureUnits() => throw new NotImplementedException();

    public override void Clear() {
        // throw new NotImplementedException();
    }

    public override void TakeScreenshot() {
        // throw new NotImplementedException();
    }

    public override Rectangle ScissorRect { get; set; }

    public override void SetFullScissorRect() {
        // throw new NotImplementedException();
    }
    public override ulong GetVramUsage() => throw new NotImplementedException();
    public override ulong GetTotalVram() => throw new NotImplementedException();

    public override VixieTextureRenderTarget CreateRenderTarget(uint width, uint height) {
        throw new NotImplementedException();
    }

    public override VixieTexture CreateTextureFromByteArray(byte[] imageData, TextureParameters parameters = default) =>
        throw new NotImplementedException();

    public override VixieTexture CreateTextureFromStream(Stream stream, TextureParameters parameters = default) =>
        throw new NotImplementedException();

    public override VixieTexture CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default) =>
        throw new NotImplementedException();

    public override VixieTexture CreateWhitePixelTexture() {
        throw new NotImplementedException();
    }

#if USE_IMGUI
    public override void ImGuiUpdate(double deltaTime) {
        throw new NotImplementedException();
    }

    public override void ImGuiDraw(double deltaTime) {
        throw new NotImplementedException();
    }
#endif
}