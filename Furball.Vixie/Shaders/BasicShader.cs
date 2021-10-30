using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;
using Shader=Furball.Vixie.Gl.Shader;

namespace Furball.Vixie.Shaders {
    public class BasicShader : Shader {
        public BasicShader() : base() {
            string vertexSource = ResourceHelpers.GetStringResource("ShaderCode/BasicVertexShader.glsl", true);
            string fragmentSource = ResourceHelpers.GetStringResource("ShaderCode/BasicPixelShader.glsl", true);

            this.AttachShader(ShaderType.VertexShader,   vertexSource)
                .AttachShader(ShaderType.FragmentShader, fragmentSource)
                .Link();
        }
    }
}
