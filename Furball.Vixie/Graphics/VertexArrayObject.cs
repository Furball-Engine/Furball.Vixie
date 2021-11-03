using System;
using Furball.Vixie.Graphics;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Gl {
    public class VertexArrayObject : IDisposable
    {
        /// <summary>
        /// OpenGL Api, used to not have to write Global.GL.function everytime
        /// </summary>
        private GL gl;
        /// <summary>
        /// Unique Identifier for this Array Object
        /// </summary>
        private uint _arrayId;

        public VertexArrayObject() {
            this.gl = Global.Gl;
            //Generate Vertex Array
            this._arrayId = gl.GenVertexArray();
        }
        /// <summary>
        /// Adds a VertexBuffer with a certain Layout to this Vertex Array
        /// </summary>
        /// <param name="vertexBuffer">Vertex Buffer to add</param>
        /// <param name="layout">Layout of said Vertex Buffer</param>
        public unsafe VertexArrayObject AddBuffer(BufferObject vertexBuffer, VertexBufferLayout layout) {
            //Bind both this and the Vertex Buffer
            this.Bind();
            vertexBuffer.Bind();
            //Get all the elements
            var elements = layout.GetElements();

            uint offset = 0;
            //Loop over the elements
            for (uint i = 0; i != elements.Count; i++) {
                LayoutElement currentElement = elements[(int) i];
                //Define the Layout of this Element
                gl.EnableVertexAttribArray(i);
                gl.VertexAttribPointer(i, currentElement.Count, currentElement.Type, currentElement.Normalized, layout.GetStride(), (void*) offset);

                offset += (uint) currentElement.Count * LayoutElement.GetSizeOfType(currentElement.Type);
            }

            return this;
        }
        /// <summary>
        /// Binds or Selects this current Vertex Array
        /// </summary>
        public VertexArrayObject Bind() {
            gl.BindVertexArray(this._arrayId);

            return this;
        }
        /// <summary>
        /// Unbinds all Vertex Arrays
        /// </summary>
        public VertexArrayObject Unbind() {
            gl.BindVertexArray(0);

            return this;
        }
        /// <summary>
        /// Disposes this Vertex Array
        /// </summary>
        public void Dispose() {
            gl.DeleteVertexArray(this._arrayId);
        }
    }
}
