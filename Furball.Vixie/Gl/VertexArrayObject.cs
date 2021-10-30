using System;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Gl {
    public class VertexArrayObject<pVertexType, pIndexType> : IDisposable
        where pVertexType : unmanaged
        where pIndexType : unmanaged
    {
        private GL gl;

        private uint _arrayId;

        public VertexArrayObject() {
            this.gl = Global.Gl;

            this._arrayId = gl.GenVertexArray();
        }

        public unsafe void AddBuffer(BufferObject<pVertexType> vertexBuffer, VertexBufferLayout layout) {
            this.Bind();
            vertexBuffer.Bind();

            var elements = layout.GetElements();

            uint offset = 0;

            for (uint i = 0; i != elements.Count; i++) {
                LayoutElement currentElement = elements[(int) i];

                gl.EnableVertexAttribArray(i);
                gl.VertexAttribPointer(i, currentElement.Count, currentElement.Type, currentElement.Normalized, layout.GetStride(), (void*) offset);

                offset += (uint) currentElement.Count * LayoutElement.GetSizeOfType(currentElement.Type);
            }
        }

        public void Bind() {
            gl.BindVertexArray(this._arrayId);
        }

        public void Unbind() {
            gl.BindVertexArray(0);
        }

        public void Dispose() {
            gl.DeleteVertexArray(this._arrayId);
        }
    }
}
