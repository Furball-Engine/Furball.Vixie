using System.Collections.Generic;

namespace Furball.Vixie.Backends.Vulkan {
    public sealed class ExtensionSet {
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
}
