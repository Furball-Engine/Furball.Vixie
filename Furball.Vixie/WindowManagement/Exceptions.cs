using System;

namespace Furball.Vixie.WindowManagement; 

public class WindowCreationFailedException : Exception {
    public override string Message => "Failed to create window!";
}