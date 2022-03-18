using System.Security.Cryptography.X509Certificates;
using Kettu;

namespace Furball.Vixie.Graphics.Backends.OpenGL {
    internal class LoggerLevelOpenGL : LoggerLevel {
        public override string Name => "OpenGL";

        private enum Channel {
            Error,
            Warning,
            Info
        }

        public static readonly LoggerLevelOpenGL InstanceError   = new(Channel.Error);
        public static readonly LoggerLevelOpenGL InstanceWarning = new(Channel.Warning);
        public static readonly LoggerLevelOpenGL InstanceInfo    = new(Channel.Info);
        
        private LoggerLevelOpenGL(Channel channel) {
            base.Channel = channel.ToString();
        }
    }
}
