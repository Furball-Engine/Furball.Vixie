using System.Text;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Helpers.Helpers;

namespace Furball.Vixie.Backends.OpenGL; 

public static class RendererShaderGenerator {
    public static string GetFragment(IGraphicsBackend backend) {
        string orig = ResourceHelpers.GetStringResource("Shaders/FragmentShader.glsl");

        StringBuilder uniformBuilder = new();
        StringBuilder ifBuilder      = new();

        for (int i = 0; i < backend.QueryMaxTextureUnits(); i++) {
            uniformBuilder.Append($"uniform sampler2D tex_{i};\n");

            if (i != 0) ifBuilder.Append("else ");

            ifBuilder.AppendLine(@$"if(_TextureId == {i}) {{ 
    OutputColor = texture(tex_{i}, _TextureCoordinate) * _Color; 
}}");
        }

        return orig
              .Replace("${UNIFORMS}", uniformBuilder.ToString())
              .Replace("${IF}",       ifBuilder.ToString());
    }
}