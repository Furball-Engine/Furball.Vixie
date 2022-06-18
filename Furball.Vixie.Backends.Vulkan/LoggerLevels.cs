using Kettu;

namespace Furball.Vixie.Backends.Vulkan {
    public class LoggerLevelVulkan : LoggerLevel {
        public override string Name => "VulkanInfo";

        private enum Channel {
            Info,
            Warning,
            Error,
            Fatal,
            CallbackVerbose,
            CallbackInfo,
            CallbackWarning,
            CallbackError,
        }

        public static readonly LoggerLevelVulkan InstanceInfo            = new(Channel.Info);
        public static readonly LoggerLevelVulkan InstanceWarning         = new(Channel.Warning);
        public static readonly LoggerLevelVulkan InstanceError           = new(Channel.Error);
        public static readonly LoggerLevelVulkan InstanceFatal           = new(Channel.Fatal);
        public static readonly LoggerLevelVulkan InstanceCallbackVerbose = new(Channel.CallbackVerbose);
        public static readonly LoggerLevelVulkan InstanceCallbackInfo    = new(Channel.CallbackInfo);
        public static readonly LoggerLevelVulkan InstanceCallbackWarning = new(Channel.CallbackWarning);
        public static readonly LoggerLevelVulkan InstanceCallbackError   = new(Channel.CallbackError);
        
        private LoggerLevelVulkan(Channel channel) {
            base.Channel = channel.ToString();
        }
    }
}
