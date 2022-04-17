using Kettu;

namespace Furball.Vixie.Backends.Direct3D11 {
    internal class LoggerLevelD3D11 : LoggerLevel {
        public override string Name => "Direct3D11";

        private enum Channel {
            Error,
            Warning,
            Info
        }

        public static readonly LoggerLevelD3D11 InstanceError   = new(Channel.Error);
        public static readonly LoggerLevelD3D11 InstanceWarning = new(Channel.Warning);
        public static readonly LoggerLevelD3D11 InstanceInfo    = new(Channel.Info);
        
        private LoggerLevelD3D11(Channel channel) {
            base.Channel = channel.ToString();
        }
    }
}
