using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Kettu;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;

namespace Furball.Vixie.Backends.Vulkan {
    public unsafe class VulkanBackend : IGraphicsBackend {

        private class QueueInfo : IDisposable {
            public int QueueFamilyIndex { get; }
            public int QueueIndex { get; }
            public Queue Handle { get; }
            public QueueFamilyProperties FamilyProperties { get; }

            public QueueInfo(
                int queueFamilyIndex,
                int queueIndex,
                Queue handle,
                QueueFamilyProperties familyProperties
            ) {
                this.QueueFamilyIndex = queueFamilyIndex;
                this.QueueIndex       = queueIndex;
                this.Handle           = handle;
                this.FamilyProperties = familyProperties;
            }
            public virtual void Dispose() {}
        }

        private sealed class QueuePool : IDisposable {

            private sealed class QueueReference : QueueInfo, IDisposable {
                #if DEBUG
                private bool _released = false;
                #endif
                private readonly QueuePool _parent;
                public QueueReference(
                    QueuePool parent,
                    int queueFamilyIndex,
                    int queueIndex,
                    Queue handle,
                    QueueFamilyProperties familyProperties
                ) : base(queueFamilyIndex, queueIndex, handle, familyProperties) {
                    this._parent = parent;
                }

                public override void Dispose() {
                    base.Dispose();
                    this._parent.Release(this);
#if DEBUG
                    if (this._released) {
                        Logger.Log($"Double Release of Queue {Handle} detected.", LoggerLevelVulkan.InstanceWarning);
                    }
                    this._released = true;
#endif
                }
            }

            private readonly ReadOnlyMemory<QueueInfo> _queueInfos;
            private readonly Memory<int>               _usageCount;

            public QueuePool(ReadOnlyMemory<QueueInfo> queueInfos) {
                this._queueInfos = queueInfos;
                this._usageCount = new int[queueInfos.Length];
            }

            private void Release(QueueReference reference) {
                var s = this._queueInfos.Span;
                int i;
                for (i = 0; i < s.Length; i++) {
                    if (s[i].Handle.Handle == reference.Handle.Handle) {
                        break;
                    }
                }

                this._usageCount.Span[i]--;
            }

            public QueueInfo GetReferenceTo(QueueInfo info) {
                var s = this._queueInfos.Span;
                int i;
                for (i = 0; i < s.Length; i++) {
                    if (s[i].Handle.Handle == info.Handle.Handle) {
                        break;
                    }
                }

                var v = new QueueReference(this,
                                           s[i].QueueFamilyIndex,
                                           s[i].QueueIndex,
                                           s[i].Handle,
                                           s[i].FamilyProperties);
                this._usageCount.Span[i]++;
                return v;
            }

            private bool TryFindNext(QueueFlags requiredFlags, out QueueInfo best) {
                var s1 = this._queueInfos.Span;
                var s2 = this._usageCount.Span;
                Debug.Assert(s1.Length == s2.Length);

                int iMax = 0;
                int max = int.MaxValue;
                for (int i = 0; i < s1.Length; i++) {
                    if (s2[i] < max && (s1[i].FamilyProperties.QueueFlags & requiredFlags) == requiredFlags) {
                        iMax = i;
                        max  = s2[i];
                    }
                }

                if (max < 0) {
                    best = null;
                    return false;
                }

                best = new QueueReference(this,
                                          s1[iMax].QueueFamilyIndex,
                                          s1[iMax].QueueIndex,
                                          s1[iMax].Handle,
                                          s1[iMax].FamilyProperties);
                s2[iMax]++;
                return true;
            }
            public QueueInfo NextTransferQueue() {
                if (!TryFindNext(QueueFlags.QueueTransferBit, out var queueInfo)) {
                    Logger.Log("Could not find Transfer queue", LoggerLevelVulkan.InstanceFatal);
                    return null!;
                }
                return queueInfo;
            }
            public QueueInfo NextComputeQueue() {
                if (!TryFindNext(QueueFlags.QueueComputeBit, out var queueInfo)) {
                    Logger.Log("Could not find Transfer queue", LoggerLevelVulkan.InstanceFatal);
                    return null!;
                }
                return queueInfo;
            }
            public QueueInfo NextGraphicsQueue() {
                if (!TryFindNext(QueueFlags.QueueGraphicsBit, out var queueInfo)) {
                    Logger.Log("Could not find Transfer queue", LoggerLevelVulkan.InstanceFatal);
                    return null!;
                }
                return queueInfo;
            }

            public void Dispose() {
                var s = this._queueInfos.Span;
                var s2 = this._usageCount.Span;
                Debug.Assert(s.Length == s2.Length);
                for (var index = 0; index < s.Length; index++) {
                    s[index].Dispose();
                    if (s2[index] > 0) {
                        Logger.Log($"Queue {s[index].Handle} still had open references. R={s2[index]}",
                                   LoggerLevelVulkan.InstanceWarning);
                    }
                }
            }
        }

        private sealed class ExtensionSet {
            public IReadOnlyCollection<string> RequiredExtensions { get; }
            public IReadOnlyCollection<string> OptionalExtensions { get; }

            public ExtensionSet(
                IReadOnlyCollection<string> requiredExtensions,
                IReadOnlyCollection<string> optionalExtensions
            ) {
                this.RequiredExtensions = requiredExtensions;
                this.OptionalExtensions = optionalExtensions;
            }
        }

        private sealed class PhysicalDeviceInfo {
            public PhysicalDevice Handle { get; }
            public string Name { get; }
            public PhysicalDeviceFeatures Features { get; }
            public PhysicalDeviceProperties Properties { get; }
            public ReadOnlyMemory<QueueInfo> Queues { get; }

            public PhysicalDeviceMemoryProperties MemoryProperties { get; }

            public PhysicalDeviceInfo(
                PhysicalDevice handle,
                string name,
                PhysicalDeviceFeatures features,
                PhysicalDeviceProperties properties,
                ReadOnlyMemory<QueueInfo> queues,
                PhysicalDeviceMemoryProperties memoryProperties
            ) {
                this.Handle           = handle;
                this.Name             = name;
                this.Features         = features;
                this.Properties       = properties;
                this.Queues           = queues;
                this.MemoryProperties = memoryProperties;
            }
        }

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
            this._vk.GetPhysicalDeviceProperties(handle, out var properties);
            this._vk.GetPhysicalDeviceFeatures(handle, out var features);
            this._vk.GetPhysicalDeviceMemoryProperties(handle, out var memoryProperties);

            uint queueFamilyCount = 0;
            this._vk.GetPhysicalDeviceQueueFamilyProperties(handle, ref queueFamilyCount, null);
            var queueFamilyProperties = new QueueFamilyProperties[queueFamilyCount];

            fixed (QueueFamilyProperties* pQueueFamilyProperties = queueFamilyProperties)
                this._vk.GetPhysicalDeviceQueueFamilyProperties(handle, ref queueFamilyCount, pQueueFamilyProperties);

            var queueInfos = queueFamilyProperties
                .SelectMany((x, i) => Enumerable.Range(0, (int)x.QueueCount)
                                .Select(j => new QueueInfo(i, j, default, x)))
                .ToArray();

            var name = SilkMarshal.PtrToString((nint)properties.DeviceName);

            return new PhysicalDeviceInfo(handle,
                                          name!,
                                          features,
                                          properties,
                                          queueInfos.AsMemory().Slice(0, (int)queueFamilyCount),
                                          memoryProperties);
        }

        private Instance                _instance;
        private Vk                      _vk;
        private IWindow                 _window;
        private PhysicalDeviceInfo      _physicalDeviceInfo;
        private Device                  _device;
        private SurfaceKHR              _surface;
        private bool                    _debug;
        private DebugUtilsMessengerEXT? _messenger = null;
        private QueuePool               _queuePool;
        private QueueInfo               _presentationQueueInfo;

        // Extensions:
        private ExtDebugUtils  _extDebugUtils;
        private KhrSurface     _vkSurface;
        private ExtToolingInfo _vkToolingInfo;

        private static readonly unsafe PfnDebugUtilsMessengerCallbackEXT _DebugCallback = new(DebugCallback);

        private ExtensionSet GetInstanceExtensions() {
            var requiredExtensions = new List<string>();
            var optionalExtensions = new List<string>();

            Debug.Assert(this._window.VkSurface is not null);
            requiredExtensions.AddRange(SilkMarshal.PtrToStringArray(
                                            (nint)this._window.VkSurface!.GetRequiredExtensions(
                                                out var windowExtensionCount),
                                            (int)windowExtensionCount));
            requiredExtensions.Add(KhrSurface.ExtensionName);

            if (_debug) {
                optionalExtensions.Add(ExtDebugUtils.ExtensionName);
            }

            optionalExtensions.Add(ExtToolingInfo.ExtensionName);

            return new ExtensionSet(requiredExtensions, optionalExtensions);
        }

        private ExtensionSet GetDeviceExtensions() {
            var requiredExtensions = new List<string>();
            var optionalExtensions = new List<string>();

            return new ExtensionSet(requiredExtensions, optionalExtensions);
        }

        private static bool TryExtractUsedExtensions(
            ExtensionSet extensionSet,
            Func<string, bool> verifyExtension,
            out int count,
            /* [NotNullWhen(true)] */
            out GlobalMemory? memory
        ) {
            var usedExtensions = new HashSet<string>();

            bool allRequiredPresent = true;
            foreach (var required in extensionSet.RequiredExtensions) {
                if (!verifyExtension(required)) {
                    Logger.Log("Required Extension not Present: \"" + required + "\"",
                               LoggerLevelVulkan.InstanceWarning);
                    allRequiredPresent = false;
                } else {
                    usedExtensions.Add(required);
                }
            }
            if (!allRequiredPresent) {
                memory = null;
                count  = 0;
                return false;
            }

            foreach (var optional in extensionSet.OptionalExtensions) {
                if (!verifyExtension(optional)) {
                    Logger.Log("Optional Extension not Present: \"" + optional + "\"", LoggerLevelVulkan.InstanceInfo);
                } else {
                    usedExtensions.Add(optional);
                }
            }

            memory = SilkMarshal.StringArrayToMemory(usedExtensions.ToArray());
            count  = usedExtensions.Count;
            return true;
        }

        private bool TryCreateInstance(out Instance instance) {

            // TODO: Allow non-conformant devices using new loader extension (2022)

            var extensionSet = GetInstanceExtensions();
            if (!TryExtractUsedExtensions(extensionSet,
                                          this._vk.IsInstanceExtensionPresent,
                                          out var extensionCount,
                                          out var extensionMemory)) {
                Logger.Log("Could not extract instance extensions", LoggerLevelVulkan.InstanceError);
                instance = default;
                return false;
            }

            var entryAssemblyname = Assembly.GetEntryAssembly()!.GetName();
            var applicationNameMem = SilkMarshal.StringToMemory(entryAssemblyname.FullName);
            var engineNameMem = SilkMarshal.StringToMemory("Furball.Vixie");

            ApplicationInfo appInfo = new ApplicationInfo {
                SType              = StructureType.ApplicationInfo,
                PApplicationName   = applicationNameMem.AsPtr<byte>(),
                ApplicationVersion = (Version32)entryAssemblyname.Version,
                PEngineName        = engineNameMem.AsPtr<byte>(),
                EngineVersion      = new Version32(1, 0, 0),
                ApiVersion         = Vk.Version10
            };

            InstanceCreateInfo createInfo = new InstanceCreateInfo {
                SType                   = StructureType.InstanceCreateInfo, PApplicationInfo = &appInfo,
                EnabledExtensionCount   = (uint)extensionCount,
                PpEnabledExtensionNames = (byte**)(extensionMemory?.Handle ?? 0)
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

            var infos = devices.Take((int)deviceCount)
                .Select(this.QueryPhysicalDeviceInfos)
                .Where(x => {
                    foreach (var e in extensionSet.RequiredExtensions) {
                        if (!this._vk.IsDeviceExtensionPresent(x.Handle, e)) {
                            Logger.Log($"Rejecting {x.Name} as it does not support required extension {e}",
                                       LoggerLevelVulkan.InstanceInfo);
                            return false;
                        }
                    }
                    return true;
                })
                .Where(x => {
                    foreach (var v in x.Queues.Span) {
                        this._vkSurface.GetPhysicalDeviceSurfaceSupport(x.Handle,
                                                                        (uint)v.QueueFamilyIndex,
                                                                        this._surface,
                                                                        out var supported);
                        if (supported) return true;
                    }
                    Logger.Log($"Rejecting {x.Name} as it does not support presentation",
                               LoggerLevelVulkan.InstanceInfo);
                    return false;
                });

            PhysicalDeviceInfo? bestInfo = null;
            try {
                var rated = infos.Select(x => (x, RateDeviceInfo(x)
                                                  // Each optional present extension is worth 100 for now
                                                  + (ulong)extensionSet.OptionalExtensions.Sum(
                                                      e => this._vk.IsDeviceExtensionPresent(x.Handle, e) ? 100 : 0)))
                    .ToArray();

                var maxV = ulong.MinValue;
                foreach (var (x, rating) in rated) {
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
            var physicalQueues = this._physicalDeviceInfo.Queues.Span;
            var queueCreates = new DeviceQueueCreateInfo[physicalQueues.Length];
            int queueCreateCount = -1;
            foreach (QueueInfo q in physicalQueues) {
                if (q.QueueFamilyIndex > queueCreateCount) queueCreateCount = q.QueueFamilyIndex;
                queueCreates[q.QueueFamilyIndex] = new DeviceQueueCreateInfo(
                    queueCount: q.FamilyProperties.QueueCount,
                    queueFamilyIndex: (uint)q.QueueFamilyIndex,
                    pQueuePriorities: prio);
            }

            var enabledFeature = new PhysicalDeviceFeatures() {};

            if (!TryExtractUsedExtensions(extensionSet,
                                          e => _vk.IsDeviceExtensionPresent(this._physicalDeviceInfo.Handle, e),
                                          out var enabledExtensionCount,
                                          out var extensionMem)) {
                device     = default;
                queuePool  = null;
                queueInfos = default;
                return false;
            }

            fixed (DeviceQueueCreateInfo* pQueueCreateInfos = queueCreates)
                this._vk.CreateDevice(this._physicalDeviceInfo.Handle,
                                      new DeviceCreateInfo(queueCreateInfoCount: (uint)(queueCreateCount + 1),
                                                           pQueueCreateInfos: pQueueCreateInfos,
                                                           enabledExtensionCount: (uint)enabledExtensionCount,
                                                           ppEnabledExtensionNames: (byte**)extensionMem.Handle,
                                                           pEnabledFeatures: &enabledFeature),
                                      null,
                                      out device);

            var newQueueInfos = new QueueInfo[this._physicalDeviceInfo.Queues.Length];

            for (int i = 0; i < newQueueInfos.Length; i++) {
                this._vk.GetDeviceQueue(device,
                                        (uint)physicalQueues[i].QueueFamilyIndex,
                                        (uint)physicalQueues[i].QueueIndex,
                                        out var queue);
                newQueueInfos[i] = new QueueInfo(physicalQueues[i].QueueFamilyIndex,
                                                 physicalQueues[i].QueueIndex,
                                                 queue,
                                                 physicalQueues[i].FamilyProperties);
            }

            queueInfos = newQueueInfos;
            queuePool  = new QueuePool(queueInfos);
            return true;
        }

        public override unsafe void Initialize(IWindow window, IInputContext inputContext) {
            this._window = window;

            if (_window.VkSurface == null) {
                throw new NotSupportedException("The window was created without Vulkan support");
            }

            _vk = Vk.GetApi();

            _debug = window.API.Flags.HasFlag(ContextFlags.Debug);

            if (_debug)
                Logger.Log("Using Vulkan debug tools, this may hurt performance", LoggerLevelVulkan.InstanceWarning);

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

            this._surface = _window.VkSurface!.Create<AllocationCallbacks>(_instance.ToHandle(), null).ToSurface();

            if (_debug)
                this.CreateDebugMessenger();

            var deviceExtensionSet = this.GetDeviceExtensions();

            if (!TryGetPhysicalDevice(deviceExtensionSet, out this._physicalDeviceInfo!)) {
                Logger.Log("Failed to pick physical device", LoggerLevelVulkan.InstanceFatal);
                return;
            }

            var physicalDeviceProperties = this._physicalDeviceInfo.Properties; // to access fixed-size buffers
            Logger.Log($"Picked vulkan device {SilkMarshal.PtrToString((nint)physicalDeviceProperties.DeviceName)}",
                       LoggerLevelVulkan.InstanceInfo);

            if (this._vkToolingInfo is not null) {
                uint toolCount = 0;
                this._vkToolingInfo.GetPhysicalDeviceToolProperties(this._physicalDeviceInfo.Handle,
                                                                    ref toolCount,
                                                                    null);
                var toolingProperties = new PhysicalDeviceToolProperties[toolCount];
                fixed (PhysicalDeviceToolProperties* pToolingProps = toolingProperties)
                    this._vkToolingInfo.GetPhysicalDeviceToolProperties(this._physicalDeviceInfo.Handle,
                                                                        ref toolCount,
                                                                        pToolingProps);

                for (int i = 0; i < toolCount; i++) {
                    var tool = toolingProperties[i];
                    Logger.Log(
                        $"Tool {SilkMarshal.PtrToString((nint)tool.Name)} {SilkMarshal.PtrToString((nint)tool.Version)} purposes: {tool.Purposes}\n" +
                        $"\"{SilkMarshal.PtrToString((nint)tool.Description)}\"",
                        LoggerLevelVulkan.InstanceInfo);
                }
            }

            if (!TryCreateLogicalDevice(deviceExtensionSet,
                                        out this._device,
                                        out this._queuePool,
                                        out var queueInfos)) {
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

            if (!TryGetPresentationQueue(out var presQueue)) {
                Logger.Log(
                    "Could not retrieve presentation queue. This should be prohibited by earlier code... Did you unplug your monitor?",
                    LoggerLevelVulkan.InstanceFatal);
                return;
            }

            this._presentationQueueInfo = this._queuePool!.GetReferenceTo(presQueue!);
            Logger.Log(
                $"Picked Presentation Queue from queue family {presQueue!.QueueFamilyIndex} and registered with pool",
                LoggerLevelVulkan.InstanceInfo);
        }
        private bool TryGetPresentationQueue(out QueueInfo? presentationQueueInfo) {
            var physicalQueues = this._physicalDeviceInfo.Queues.Span;
            foreach (var q in physicalQueues) {
                this._vkSurface.GetPhysicalDeviceSurfaceSupport(this._physicalDeviceInfo.Handle,
                                                                (uint)q.QueueFamilyIndex,
                                                                this._surface,
                                                                out var supported);
                if (supported) {
                    presentationQueueInfo = q;
                    return true;
                }
            }
            presentationQueueInfo = null;
            return false;
        }

        #region Debug Stuff

        private unsafe void CreateDebugMessenger() {
            DebugUtilsMessengerCreateInfoEXT createInfo = new() {
                SType = StructureType.DebugUtilsMessengerCreateInfoExt,
                MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt |
                                  DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityInfoBitExt    |
                                  DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt |
                                  DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt,
                MessageType = DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt     |
                              DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt |
                              DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt,
                PfnUserCallback = _DebugCallback
            };

            DebugUtilsMessengerEXT messenger;

            Result result =
                this._extDebugUtils.CreateDebugUtilsMessenger(this._instance, &createInfo, null, &messenger);

            if (result != Result.Success)
                throw new Exception($"Creating debug messenger failed! err{result}");

            this._messenger = messenger;
        }

        private static unsafe uint DebugCallback(
            DebugUtilsMessageSeverityFlagsEXT severity,
            DebugUtilsMessageTypeFlagsEXT type,
            DebugUtilsMessengerCallbackDataEXT* callbackData,
            void* userData
        ) {
            LoggerLevel? level = severity switch {
                DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt => null,
                DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityInfoBitExt => LoggerLevelVulkan
                    .InstanceCallbackInfo,
                DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt => LoggerLevelVulkan
                    .InstanceCallbackWarning,
                DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt => LoggerLevelVulkan
                    .InstanceCallbackError,
                _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
            };

            if (level is null) return 0;

            Logger.Log($"{SilkMarshal.PtrToString((nint)callbackData->PMessage)}", level);

            return 0;
        }

        #endregion

        public override unsafe void Cleanup() {

            this._presentationQueueInfo.Dispose();
            this._queuePool.Dispose();

            this._vk.DestroyDevice(this._device, null);
            if (this._messenger.HasValue)
                this._extDebugUtils.DestroyDebugUtilsMessenger(this._instance, this._messenger.Value, null);
            this._vkSurface.DestroySurface(this._instance, this._surface, null);
            this._vk.DestroyInstance(this._instance, null);
        }
        public override void HandleWindowSizeChange(int width, int height) {
            throw new NotImplementedException();
        }
        public override void HandleFramebufferResize(int width, int height) {
            throw new NotImplementedException();
        }
        public override IQuadRenderer CreateTextureRenderer() => throw new NotImplementedException();
        public override ILineRenderer CreateLineRenderer() => throw new NotImplementedException();
        public override int QueryMaxTextureUnits() => throw new NotImplementedException();
        public override void Clear() {
            throw new NotImplementedException();
        }
        public override void TakeScreenshot() {
            throw new NotImplementedException();
        }
        public override Rectangle ScissorRect { get; set; }
        public override void SetFullScissorRect() {
            throw new NotImplementedException();
        }
        public override TextureRenderTarget CreateRenderTarget(uint width, uint height)
            => throw new NotImplementedException();
        public override Texture CreateTexture(byte[] imageData, bool qoi = false)
            => throw new NotImplementedException();
        public override Texture CreateTexture(Stream stream) => throw new NotImplementedException();
        public override Texture CreateTexture(uint width, uint height) => throw new NotImplementedException();
        public override Texture CreateTexture(string filepath) => throw new NotImplementedException();
        public override Texture CreateWhitePixelTexture() => throw new NotImplementedException();
        public override void ImGuiUpdate(double deltaTime) {
            throw new NotImplementedException();
        }
        public override void ImGuiDraw(double deltaTime) {
            throw new NotImplementedException();
        }
    }
}