using System;
using Vanara.PInvoke;

namespace Furball.Vixie.Backends.Shared;

public class PdhVideoMemorySource : VideoMemorySource {
    public PdhVideoMemorySource() {
        //Connect to the local machine
        Pdh.PdhConnectMachine();

        //Open a query
        Win32Error result = Pdh.PdhOpenQuery(null, IntPtr.Zero, out Pdh.SafePDH_HQUERY query);
        if (result != Win32Error.ERROR_SUCCESS)
            throw new Exception("Failed to open PDH query!");
    }

    public override ulong TotalVideoMemory() => throw new NotImplementedException();
    public override ulong UsedVideoMemory()  => throw new NotImplementedException();
}