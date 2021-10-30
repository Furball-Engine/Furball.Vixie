using Furball.Vixie.Gl;
using Silk.NET.OpenGL;
using Shader=Furball.Vixie.Gl.Shader;

namespace Furball.Vixie {
    public class Renderer {
        private GL gl;

        public Renderer() {
            this.gl = Global.Gl;
        }

        public unsafe void Draw(BufferObject<float> vertexBuffer, BufferObject<uint> indexBuffer, Shader shader) {
            vertexBuffer.Bind();
            indexBuffer.Bind();
            shader.Bind();

            gl.DrawElements(PrimitiveType.Triangles, indexBuffer.DataCount, DrawElementsType.UnsignedInt, null);
        }

        public void Clear() {
            gl.Clear(ClearBufferMask.ColorBufferBit);
        }
    }
}
