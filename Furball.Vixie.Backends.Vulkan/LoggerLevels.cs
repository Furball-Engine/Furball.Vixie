using Kettu;

namespace Furball.Vixie.Backends.Vulkan {
    public class LoggerLevelVulkan : LoggerLevel {
        public override string Name => "Vulkan";

        private enum Channel {
            Info,
            Warning,
            Error
        }

        public static readonly LoggerLevelVulkan InstanceInfo    = new(Channel.Info);
        public static readonly LoggerLevelVulkan InstanceWarning = new(Channel.Warning);
        public static readonly LoggerLevelVulkan InstanceError   = new(Channel.Error);
        
        private LoggerLevelVulkan(Channel channel) {
            base.Channel = channel.ToString();
        }
    }
}
