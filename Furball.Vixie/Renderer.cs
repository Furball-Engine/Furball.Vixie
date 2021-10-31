using Furball.Vixie.Gl;
using Silk.NET.OpenGL;
using Shader=Furball.Vixie.Gl.Shader;
using UniformType=Furball.Vixie.Gl.UniformType;

namespace Furball.Vixie {
    public class Renderer {
        private GL gl;

        public Renderer() {
            this.gl = Global.Gl;
        }

        public unsafe void Draw(BufferObject<float> vertexBuffer, BufferObject<uint> indexBuffer, Shader shader) {
            vertexBuffer.Bind();
            indexBuffer.Bind();
            shader
                .Bind()
                //vx_WindowProjectionMatrix is a uniform provided by Vixie which can optionally be used to scale things into the window
                .SetUniform("vx_WindowProjectionMatrix", UniformType.GlMat4f, Global.GameInstance.WindowManager.ProjectionMatrix);

            gl.DrawElements(PrimitiveType.Triangles, indexBuffer.DataCount, DrawElementsType.UnsignedInt, null);
        }

        public void Clear() {
            gl.Clear(ClearBufferMask.ColorBufferBit);
        }
    }
}
