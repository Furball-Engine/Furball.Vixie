using Kettu;

namespace Furball.Vixie.Backends.OpenGLES; 

internal class LoggerLevelOpenGLES : LoggerLevel {
    public override string Name => "OpenGLES";

    private enum Channel {
        Error,
        Warning,
        Info
    }

    public static readonly LoggerLevelOpenGLES InstanceError   = new(Channel.Error);
    public static readonly LoggerLevelOpenGLES InstanceWarning = new(Channel.Warning);
    public static readonly LoggerLevelOpenGLES InstanceInfo    = new(Channel.Info);
        
    private LoggerLevelOpenGLES(Channel channel) {
        base.Channel = channel.ToString();
    }
}