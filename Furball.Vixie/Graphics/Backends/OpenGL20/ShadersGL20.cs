using System.Collections.Immutable;
using System.Text;
using Furball.Vixie.Helpers;

namespace Furball.Vixie.Graphics.Backends.OpenGL20 {
    public static class ShadersGL20 {
        public static string GetFragment(GraphicsBackend backend) {
            string orig = ResourceHelpers.GetStringResource("ShaderCode/OpenGL20/FragmentShader.glsl");

            StringBuilder uniformBuilder = new();
            StringBuilder ifBuilder = new();

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
}
