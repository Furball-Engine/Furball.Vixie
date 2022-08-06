using Kettu;

namespace Furball.Vixie.Backends.Direct3D9;

internal class LoggerLevelD3D9 : LoggerLevel {
    public override string Name => "Direct3D9";

    private enum Channel {
        Error,
        Warning,
        Info
    }

    public static readonly LoggerLevelD3D9 InstanceError   = new(Channel.Error);
    public static readonly LoggerLevelD3D9 InstanceWarning = new(Channel.Warning);
    public static readonly LoggerLevelD3D9 InstanceInfo    = new(Channel.Info);
        
    private LoggerLevelD3D9(Channel channel) {
        base.Channel = channel.ToString();
    }
}