using Kettu;

namespace Furball.Vixie.Backends.Vulkan {
    public class LoggerLevelVeldrid : LoggerLevel {
        public override string Name => "Vulkan";

        private enum Channel {
            Info,
            Warning,
            Error
        }

        public static readonly LoggerLevelVeldrid InstanceInfo    = new(Channel.Info);
        public static readonly LoggerLevelVeldrid InstanceWarning = new(Channel.Warning);
        public static readonly LoggerLevelVeldrid InstanceError   = new(Channel.Error);
        
        private LoggerLevelVeldrid(Channel channel) {
            base.Channel = channel.ToString();
        }
    }
}
