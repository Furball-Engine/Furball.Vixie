using Kettu;

namespace Furball.Vixie.Backends.OpenGL20; 

internal class LoggerLevelOpenGL20 : LoggerLevel {
    public override string Name => "OpenGL 2.0";

    private enum Channel {
        Error,
        Warning,
        Info
    }

    public static readonly LoggerLevelOpenGL20 InstanceError   = new(Channel.Error);
    public static readonly LoggerLevelOpenGL20 InstanceWarning = new(Channel.Warning);
    public static readonly LoggerLevelOpenGL20 InstanceInfo    = new(Channel.Info);
        
    private LoggerLevelOpenGL20(Channel channel) {
        base.Channel = channel.ToString();
    }
}