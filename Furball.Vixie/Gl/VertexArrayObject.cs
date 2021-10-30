using System;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Gl {
    public class VertexArrayObject<pVertexType, pIndexType> : IDisposable
        where pVertexType : unmanaged
        where pIndexType : unmanaged
    {
        private GL gl;

        private uint _arrayId;

        public VertexArrayObject(BufferObject<pVertexType> vertexBufferObject, BufferObject<pIndexType> indexBufferObject) {
            this.gl = Global.Gl;

            this._arrayId = gl.GenVertexArray();
            gl.BindVertexArray(this._arrayId);

            vertexBufferObject.Bind();
            indexBufferObject.Bind();
        }

        public VertexArrayObject<pVertexType, pIndexType> Bind() {
            gl.BindVertexArray(this._arrayId);

            return this;
        }

        #region Attributes

        private uint _attribIndex       = 0;
        private long _attribOffset      = 0;
        private int  _attribOffsetCount = 0;

        public unsafe VertexArrayObject<pVertexType, pIndexType> AddAttribute(int count, VertexAttribPointerType type, uint vertexSize)
        {
            //Setting up a vertex attribute pointer
            gl.VertexAttribPointer(this._attribIndex, count, type, false, vertexSize * (uint) sizeof(pVertexType), (void*) this._attribOffset);
            gl.EnableVertexAttribArray(this._attribIndex);

            this._attribIndex++;
            this._attribOffsetCount += count;
            this._attribOffset      += (this._attribOffsetCount * sizeof(pVertexType));

            return this;
        }

        #endregion

        public void Dispose() {
            gl.DeleteVertexArray(this._arrayId);
        }
    }
}
