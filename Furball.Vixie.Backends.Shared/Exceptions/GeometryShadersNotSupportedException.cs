using System;

namespace Furball.Vixie.Graphics.Exceptions {
    public class GeometryShadersNotSupportedException : Exception {
        public override string Message => "Your GPU does not support Geometry Shaders on this backend! If you are on Windows, try the D3D11 backend, otherwise, try the OpenGL 2.0 backend!";
    }
}
