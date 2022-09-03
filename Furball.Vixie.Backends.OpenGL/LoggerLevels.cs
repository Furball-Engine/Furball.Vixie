using Kettu;

namespace Furball.Vixie.Backends.OpenGL; 

internal class LoggerLevelOpenGl : LoggerLevel {
    public override string Name => "OpenGL";

    private new enum Channel {
        Error,
        Warning,
        Info
    }

    public static readonly LoggerLevelOpenGl InstanceError   = new(Channel.Error);
    public static readonly LoggerLevelOpenGl InstanceWarning = new(Channel.Warning);
    public static readonly LoggerLevelOpenGl InstanceInfo    = new(Channel.Info);
        
    private LoggerLevelOpenGl(Channel channel) {
        base.Channel = channel.ToString();
    }
}