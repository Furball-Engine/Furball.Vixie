using System.Runtime.CompilerServices;
using Furball.Vixie.Backends.Shared.Tracy.Structs;
using Silk.NET.Core.Native;

namespace Furball.Vixie.Backends.Shared.Tracy;

public unsafe partial class Tracy {
    public static TracyCZoneContext Zone(
        int active, string name, [CallerMemberName] string functionName = "", [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = ""
    ) {
        byte* ptrName     = (byte*)SilkMarshal.StringToPtr(name);
        byte* ptrFunction = (byte*)SilkMarshal.StringToPtr(functionName);
        byte* ptrFile     = (byte*)SilkMarshal.StringToPtr(file);

        ulong sourceLocation = AllocSourceLocationName(
        (uint)lineNumber,
        ptrFile,
        file.Length,
        ptrFunction,
        functionName.Length,
        ptrName,
        name.Length
        );

        SilkMarshal.FreeString((nint)ptrName);
        SilkMarshal.FreeString((nint)ptrFunction);
        SilkMarshal.FreeString((nint)ptrFile);

        TracyCZoneContext ctx = EmitZoneBegin((SourceLocationData*)sourceLocation, active);

        return ctx;
    }

    public static void EndZone(TracyCZoneContext ctx) {
        EmitZoneEnd(ctx);
    }
}