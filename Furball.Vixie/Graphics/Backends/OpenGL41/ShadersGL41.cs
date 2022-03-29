using System.Collections.Immutable;
using System.Text;
using Furball.Vixie.Helpers;

namespace Furball.Vixie.Graphics.Backends.OpenGL41 {
    public static class ShadersGL41 {
        public static string GetFragment(GraphicsBackend backend) {
            string orig = ResourceHelpers.GetStringResource("ShaderCode/OpenGL41/InstancedRenderer/FragmentShader.glsl");

            StringBuilder uniformBuilder = new();
            StringBuilder ifBuilder = new();

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
}
