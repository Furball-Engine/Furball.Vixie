using Kettu;

namespace Furball.Vixie.Backends.OpenGL41 {
    internal class LoggerLevelOpenGL41 : LoggerLevel {
        public override string Name => "OpenGL 4.1";

        private enum Channel {
            Error,
            Warning,
            Info
        }

        public static readonly LoggerLevelOpenGL41 InstanceError   = new(Channel.Error);
        public static readonly LoggerLevelOpenGL41 InstanceWarning = new(Channel.Warning);
        public static readonly LoggerLevelOpenGL41 InstanceInfo    = new(Channel.Info);
        
        private LoggerLevelOpenGL41(Channel channel) {
            base.Channel = channel.ToString();
        }
    }
}
