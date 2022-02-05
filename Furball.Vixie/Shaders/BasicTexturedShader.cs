using Furball.Vixie.Helpers;
using Silk.NET.OpenGLES;
using UniformType=Furball.Vixie.Graphics.UniformType;

namespace Furball.Vixie.Shaders {
    /// <summary>
    /// Basic Textured Shader which expects a Texture bound at index 0
    /// </summary>
    public class BasicTexturedShader : Graphics.Shader {
        public BasicTexturedShader() : base() {
            string vertexSource   = ResourceHelpers.GetStringResource("ShaderCode/BasicTexturedShader/VertexShader.glsl", true);
            string fragmentSource = ResourceHelpers.GetStringResource("ShaderCode/BasicTexturedShader/PixelShader.glsl",  true);

            this.AttachShader(ShaderType.VertexShader,   vertexSource)
                .AttachShader(ShaderType.FragmentShader, fragmentSource)
                .Link()
                .Bind()
                .SetUniform("vx_ModifierX", UniformType.GlFloat, Global.GameInstance.WindowManager.PositionMultiplier.X)
                .SetUniform("vx_ModifierY", UniformType.GlFloat, Global.GameInstance.WindowManager.PositionMultiplier.Y)
                .SetUniform("u_Texture",    UniformType.GlInt,   0);
        }
    }
}
