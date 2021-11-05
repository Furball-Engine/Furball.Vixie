using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;
using Shader=Furball.Vixie.Graphics.Shader;

namespace Furball.Vixie.Shaders {
    /// <summary>
    /// Basic Filled Color Shader, expects a uniform u_Color to be set as the Color
    /// </summary>
    public class BasicShader : Graphics.Shader {
        public BasicShader() : base() {
            string vertexSource   = ResourceHelpers.GetStringResource("ShaderCode/BasicShader/VertexShader.glsl", true);
            string fragmentSource = ResourceHelpers.GetStringResource("ShaderCode/BasicShader/PixelShader.glsl",  true);

            this.AttachShader(ShaderType.VertexShader,   vertexSource)
                .AttachShader(ShaderType.FragmentShader, fragmentSource)
                .Link();
        }
    }
}
