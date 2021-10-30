using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;
using Shader=Furball.Vixie.Gl.Shader;
using UniformType=Furball.Vixie.Gl.UniformType;

namespace Furball.Vixie.Shaders {
    public class BasicTexturedShader : Shader {
        public BasicTexturedShader() : base() {
            string vertexSource = ResourceHelpers.GetStringResource("ShaderCode/BasicTexturedVertexShader.glsl", true);
            string fragmentSource = ResourceHelpers.GetStringResource("ShaderCode/BasicTexturedPixelShader.glsl", true);

            this.AttachShader(ShaderType.VertexShader,   vertexSource)
                .AttachShader(ShaderType.FragmentShader, fragmentSource)
                .Link()
                .Bind()
                .SetUniform("u_Texture", UniformType.GlInt, 0);
        }
    }
}
