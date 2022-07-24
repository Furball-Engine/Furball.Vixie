using System;

namespace Furball.Vixie.Backends.OpenGL.Abstractions; 

internal sealed class WrongGLBackendException : Exception {
    public WrongGLBackendException() : base("That GL type is not available on the current backend!") {
            
    }
}