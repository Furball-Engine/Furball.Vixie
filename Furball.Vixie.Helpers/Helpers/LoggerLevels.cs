using Kettu;

namespace Furball.Vixie.Helpers.Helpers; 

public class LoggerLevelDebugMessageCallback : LoggerLevel {
    public override string Name => "GLDebugMessageCallback";

    public enum Severity {
        High,
        Medium,
        Low,
        Notification
    }

    public static LoggerLevelDebugMessageCallback InstanceHigh         = new(Severity.High);
    public static LoggerLevelDebugMessageCallback InstanceMedium       = new(Severity.Medium);
    public static LoggerLevelDebugMessageCallback InstanceLow          = new(Severity.Low);
    public static LoggerLevelDebugMessageCallback InstanceNotification = new(Severity.Notification);
        
    private LoggerLevelDebugMessageCallback(Severity severity) {
        this.Channel = severity.ToString();
    }
}

public class LoggerLevelImageLoader : LoggerLevel {
    public override string Name => "ImageLoader";

    public static LoggerLevelImageLoader Instance = new();

    private LoggerLevelImageLoader() {}
}