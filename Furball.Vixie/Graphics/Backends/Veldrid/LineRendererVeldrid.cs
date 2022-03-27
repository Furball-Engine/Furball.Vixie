using System.Numerics;
using System.Text;
using Furball.Vixie.Graphics.Renderers;
using Veldrid;
using Veldrid.SPIRV;

namespace Furball.Vixie.Graphics.Backends.Veldrid {
    public class LineRendererVeldrid : ILineRenderer {
        private readonly VeldridBackend _backend;
        
        public LineRendererVeldrid(VeldridBackend backend) {
            this._backend = backend;
            
            string vertexSource   = Helpers.ResourceHelpers.GetStringResource("ShaderCode/Veldrid/Lines/VertexShader.glsl",   true);
            string fragmentSource = Helpers.ResourceHelpers.GetStringResource("ShaderCode/Veldrid/Lines/FragmentShader.glsl", true);
            string geometrySource = Helpers.ResourceHelpers.GetStringResource("ShaderCode/Veldrid/Lines/GeometryShader.glsl", true);

            ShaderDescription vertexShaderDescription   = new ShaderDescription(ShaderStages.Vertex,   SpirvCompilation.CompileGlslToSpirv(vertexSource,   "VertexShader.glsl",   ShaderStages.Vertex,   GlslCompileOptions.Default).SpirvBytes, "main");
            ShaderDescription fragmentShaderDescription = new ShaderDescription(ShaderStages.Fragment, SpirvCompilation.CompileGlslToSpirv(fragmentSource, "FragmentShader.glsl", ShaderStages.Fragment, GlslCompileOptions.Default).SpirvBytes, "main");
            ShaderDescription geometryShaderDescription = new ShaderDescription(ShaderStages.Geometry, SpirvCompilation.CompileGlslToSpirv(geometrySource, "GeometryShader.glsl", ShaderStages.Geometry, GlslCompileOptions.Default).SpirvBytes, "main");

            Shader[] shaders = new Shader[3];
            
            shaders[0] = this._backend.ResourceFactory.CreateShader(vertexShaderDescription);
            shaders[1] = this._backend.ResourceFactory.CreateShader(fragmentShaderDescription);
            shaders[2] = this._backend.ResourceFactory.CreateShader(geometryShaderDescription);
        }
        
        public void Dispose() {
            throw new System.NotImplementedException();
        }
        public bool IsBegun {
            get;
            set;
        }
        public void Begin() {
            throw new System.NotImplementedException();
        }
        public void Draw(Vector2 begin, Vector2 end, float thickness, Color color) {
            throw new System.NotImplementedException();
        }
        public void End() {
            throw new System.NotImplementedException();
        }
    }
}
