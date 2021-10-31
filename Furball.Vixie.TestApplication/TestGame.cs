using System.Numerics;
using Furball.Vixie.Gl;
using Furball.Vixie.Helpers;
using Furball.Vixie.ImGuiHelpers;
using Furball.Vixie.Shaders;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Shader=Furball.Vixie.Gl.Shader;
using Texture=Furball.Vixie.Gl.Texture;
using UniformType=Furball.Vixie.Gl.UniformType;

namespace Furball.Vixie.TestApplication {
    public class TestGame : Game {
        public TestGame(WindowOptions options) : base(options) {}

        protected override unsafe void Initialize() {
            this.Components.Add(new BaseTestSelector(this));

            base.Initialize();
        }
    }
}
