using System.Text;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Helpers.Helpers;

namespace Furball.Vixie.Backends.OpenGL20; 

internal static class FakeInstancingQuadShaderGenerator {
    public static string GetFragment(IGraphicsBackend backend) {
        string orig = ResourceHelpers.GetStringResource("Shaders/QuadRenderer/FragmentShader.glsl");

        StringBuilder uniformBuilder = new();
        StringBuilder ifBuilder      = new();

        for (int i = 0; i < backend.QueryMaxTextureUnits(); i++) {
            uniformBuilder.Append($"uniform sampler2D tex_{i};\n");

            if (i != 0) ifBuilder.Append("else ");

            ifBuilder.Append($"if(tex_id == {i}) {{ gl_FragColor = texture2D(tex_{i}, fs_in_tex) * fs_in_col; }}");
        }

        return orig
              .Replace("${UNIFORMS}", uniformBuilder.ToString())
              .Replace("${IF}", ifBuilder.ToString());
    }
}