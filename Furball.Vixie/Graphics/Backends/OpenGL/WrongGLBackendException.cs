using System;

namespace Furball.Vixie.Graphics.Backends.OpenGL_ {
    public class WrongGLBackendException : Exception {
        public WrongGLBackendException() : base("That GL type is not available on the current backend!") {
            
        }
    }
}
