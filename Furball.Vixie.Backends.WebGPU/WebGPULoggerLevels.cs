using Kettu;

namespace Furball.Vixie.Backends.WebGPU; 

public class LoggerLevelWebGPU : LoggerLevel {
    public override string Name => "WebGPUInfo";

    private new enum Channel {
        Info,
        Warning,
        Error,
        Fatal,
        CallbackVerbose,
        CallbackInfo,
        CallbackWarning,
        CallbackError,
    }

    public static readonly LoggerLevelWebGPU InstanceInfo            = new(Channel.Info);
    public static readonly LoggerLevelWebGPU InstanceWarning         = new(Channel.Warning);
    public static readonly LoggerLevelWebGPU InstanceError           = new(Channel.Error);
    public static readonly LoggerLevelWebGPU InstanceFatal           = new(Channel.Fatal);
    public static readonly LoggerLevelWebGPU InstanceCallbackVerbose = new(Channel.CallbackVerbose);
    public static readonly LoggerLevelWebGPU InstanceCallbackInfo    = new(Channel.CallbackInfo);
    public static readonly LoggerLevelWebGPU InstanceCallbackWarning = new(Channel.CallbackWarning);
    public static readonly LoggerLevelWebGPU InstanceCallbackError   = new(Channel.CallbackError);
        
    private LoggerLevelWebGPU(Channel channel) {
        base.Channel = channel.ToString();
    }
}