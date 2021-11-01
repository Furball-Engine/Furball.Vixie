using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;
using Shader=Furball.Vixie.Gl.Shader;

namespace Furball.Vixie.Shaders {
    /// <summary>
    /// Basic Filled Color Shader, expects a uniform u_Color to be set as the Color
    /// </summary>
    public class BasicShader : Shader {
        public BasicShader() : base() {
            string vertexSource = ResourceHelpers.GetStringResource("ShaderCode/BasicShader/BasicVertexShader.glsl", true);
            string fragmentSource = ResourceHelpers.GetStringResource("ShaderCode/BasicShader/BasicPixelShader.glsl", true);

            this.AttachShader(ShaderType.VertexShader,   vertexSource)
                .AttachShader(ShaderType.FragmentShader, fragmentSource)
                .Link();
        }
    }
}
