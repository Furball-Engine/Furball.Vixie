using System;
using Silk.NET.Vulkan;

namespace Furball.Vixie.Backends.Vulkan {
    public sealed class PhysicalDeviceInfo {
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
}
