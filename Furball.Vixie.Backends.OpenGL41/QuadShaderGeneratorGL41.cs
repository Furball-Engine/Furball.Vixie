using System.Text;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Helpers.Helpers;

namespace Furball.Vixie.Backends.OpenGL41; 

public static class QuadShaderGeneratorGL41 {
    public static string GetFragment(IGraphicsBackend backend) {
        string orig = ResourceHelpers.GetStringResource("Shaders/QuadRenderer/FragmentShader.glsl");

        StringBuilder uniformBuilder = new();
        StringBuilder ifBuilder      = new();

        for (int i = 0; i < backend.QueryMaxTextureUnits(); i++) {
            uniformBuilder.Append($"uniform sampler2D tex_{i};\n");

            if (i != 0) ifBuilder.Append("else ");

            ifBuilder.Append($"if(fs_in_texid == {i}) {{ OutputColor = texture(tex_{i}, fs_in_tex) * fs_in_col; }}");
        }

        return orig
              .Replace("${UNIFORMS}", uniformBuilder.ToString())
              .Replace("${IF}", ifBuilder.ToString());
    }
}