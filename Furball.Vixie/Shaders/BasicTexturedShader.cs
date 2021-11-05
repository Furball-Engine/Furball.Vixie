using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;
using Shader=Furball.Vixie.Graphics.Shader;
using UniformType=Furball.Vixie.Graphics.UniformType;

namespace Furball.Vixie.Shaders {
    /// <summary>
    /// Basic Textured Shader which expects a Texture bound at index 0
    /// </summary>
    public class BasicTexturedShader : Graphics.Shader {
        public BasicTexturedShader() : base() {
            string vertexSource = ResourceHelpers.GetStringResource("ShaderCode/BasicTexturedShader/BasicTexturedVertexShader.glsl", true);
            string fragmentSource = ResourceHelpers.GetStringResource("ShaderCode/BasicTexturedShader/BasicTexturedPixelShader.glsl", true);

            this.AttachShader(ShaderType.VertexShader,   vertexSource)
                .AttachShader(ShaderType.FragmentShader, fragmentSource)
                .Link()
                .Bind()
                .SetUniform("u_Texture", UniformType.GlInt, 0);
        }
    }
}
