using System.Runtime.CompilerServices;
using Furball.Vixie.Backends.Shared.Tracy.Structs;
using Silk.NET.Core.Native;

namespace Furball.Vixie.Backends.Shared.Tracy;

public unsafe partial class Tracy {
    public static TracyCZoneContext Zone(
        int active, [CallerMemberName] string functionName = "", [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = ""
    ) {
        SourceLocationData sourceLocationData = new SourceLocationData {
            Name     = null,
            Function = (byte*)SilkMarshal.StringToPtr(functionName),
            Color    = 0,
            File     = (byte*)SilkMarshal.StringToPtr(file),
            Line     = (uint)lineNumber
        };

        TracyCZoneContext ctx = EmitZoneBegin(&sourceLocationData, active);
        ctx.SourceLocationData = sourceLocationData;

        return ctx;
    }

    public static TracyCZoneContext Zone(
        int active, string name, [CallerMemberName] string functionName = "", [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = ""
    ) {
        SourceLocationData sourceLocationData = new SourceLocationData {
            Name     = (byte*)SilkMarshal.StringToPtr(name),
            Function = (byte*)SilkMarshal.StringToPtr(functionName),
            Color    = 0,
            File     = (byte*)SilkMarshal.StringToPtr(file),
            Line     = (uint)lineNumber
        };

        TracyCZoneContext ctx = EmitZoneBegin(&sourceLocationData, active);
        ctx.SourceLocationData = sourceLocationData;

        return ctx;
    }

    public static TracyCZoneContext Zone(
        int                    active,         string name, Color color, [CallerMemberName] string functionName = "",
        [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string file = ""
    ) {
        SourceLocationData sourceLocationData = new SourceLocationData {
            Name     = (byte*)SilkMarshal.StringToPtr(name),
            Function = (byte*)SilkMarshal.StringToPtr(functionName),
            Color    = color.ToUint(),
            File     = (byte*)SilkMarshal.StringToPtr(file),
            Line     = (uint)lineNumber
        };

        TracyCZoneContext ctx = EmitZoneBegin(&sourceLocationData, active);
        ctx.SourceLocationData = sourceLocationData;

        return ctx;
    }

    public static void EndZone(TracyCZoneContext ctx) {
        if (ctx.SourceLocationData.Function != null)
            SilkMarshal.FreeString((nint)ctx.SourceLocationData.Function);
        if (ctx.SourceLocationData.File != null)
            SilkMarshal.FreeString((nint)ctx.SourceLocationData.File);
        if (ctx.SourceLocationData.Name != null)
            SilkMarshal.FreeString((nint)ctx.SourceLocationData.Name);

        EmitZoneEnd(ctx);
    }

    public static TracyCZoneContext Zone(
        int active, Color color, [CallerMemberName] string functionName = "", [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = ""
    ) {
        SourceLocationData sourceLocationData = new SourceLocationData {
            Name     = null,
            Function = (byte*)SilkMarshal.StringToPtr(functionName),
            Color    = color.ToUint(),
            File     = (byte*)SilkMarshal.StringToPtr(file),
            Line     = (uint)lineNumber
        };

        TracyCZoneContext ctx = EmitZoneBegin(&sourceLocationData, active);
        ctx.SourceLocationData = sourceLocationData;

        return ctx;
    }
}