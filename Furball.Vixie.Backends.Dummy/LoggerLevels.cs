using Kettu;

namespace Furball.Vixie.Backends.Dummy;

internal class LoggerLevelDummy : LoggerLevel {
    public static readonly LoggerLevelDummy InstanceError   = new(Channel.Error);
    public static readonly LoggerLevelDummy InstanceWarning = new(Channel.Warning);
    public static readonly LoggerLevelDummy InstanceInfo    = new(Channel.Info);

    private LoggerLevelDummy(Channel channel) {
        base.Channel = channel.ToString();
    }
    public override string Name => "OpenGL";

    private new enum Channel {
        Error,
        Warning,
        Info
    }
}