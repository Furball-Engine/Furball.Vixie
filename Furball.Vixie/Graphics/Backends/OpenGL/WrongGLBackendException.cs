using System;

namespace Furball.Vixie.Graphics.Backends.OpenGL {
    public class WrongGLBackendException : Exception {
        public WrongGLBackendException() : base("That GL type is not available on the current backend!") {
            
        }
    }
}
