using System;

namespace Furball.Vixie.Backends.OpenGL.Abstractions; 

internal sealed class WrongGlBackendException : Exception {
    public WrongGlBackendException() : base("That GL type is not available on the current backend!") {
            
    }
}