using System.Text;
using Furball.Vixie.Helpers.Helpers;

namespace Furball.Vixie.Backends.OpenGL; 

public static class RendererShaderGenerator {
    public static string GetFragment(OpenGLBackend backend) {
        string orig = ResourceHelpers.GetStringResource("Shaders/FragmentShader.glsl", typeof(OpenGLBackend));

        StringBuilder uniformBuilder = new();
        StringBuilder ifBuilder      = new();

        for (int i = 0; i < backend.QueryMaxTextureUnits(); i++) {
            uniformBuilder.Append($"uniform sampler2D tex_{i};\n");

            if (i != 0) ifBuilder.Append("else ");

            ifBuilder.AppendLine(@$"if(_TextureId == {i}.0) {{ 
    gl_FragColor = texture2D(tex_{i}, _TextureCoordinate) * _Color; 
}}");
        }

        return orig
              .Replace("${UNIFORMS}", uniformBuilder.ToString())
              .Replace("${IF}",       ifBuilder.ToString());
    }
}