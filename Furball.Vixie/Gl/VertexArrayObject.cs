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
        /// <summary>
        /// Selects this Vertex Array
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        public VertexArrayObject<pVertexType, pIndexType> Bind() {
            gl.BindVertexArray(this._arrayId);

            return this;
        }

        #region Attributes

        private uint _attribIndex       = 0;
        private long _attribOffset      = 0;
        private int  _attribOffsetCount = 0;

        /// <summary>
        /// Adds a Vertex Attribute, used for memory segmentation
        /// </summary>
        /// <param name="count">Count of Elements per Vertex</param>
        /// <param name="type">Type of Element</param>
        /// <remarks>Buffer needs to be Bound for this to work</remarks>
        /// <returns>Self, used for chaining Methods</returns>
        public unsafe VertexArrayObject<pVertexType, pIndexType> AddAttribute<pAttribType>(int count) where pAttribType : unmanaged
        {
            VertexAttribPointerType type = Type.GetTypeCode(typeof(pAttribType)) switch {
                TypeCode.Single => VertexAttribPointerType.Float,
                TypeCode.Byte   => VertexAttribPointerType.Byte,
                TypeCode.UInt32 => VertexAttribPointerType.UnsignedInt,
                TypeCode.Int16  => VertexAttribPointerType.Short,
                TypeCode.UInt16 => VertexAttribPointerType.UnsignedShort,
                TypeCode.Int32  => VertexAttribPointerType.Int
            };
            //Setting up a vertex attribute pointer
            gl.VertexAttribPointer(this._attribIndex, count, type, false, (uint) count * (uint) sizeof(pAttribType), (void*) this._attribOffset);
            gl.EnableVertexAttribArray(this._attribIndex);

            this._attribIndex++;
            this._attribOffsetCount += count;
            this._attribOffset      += (this._attribOffsetCount * sizeof(pAttribType));

            return this;
        }

        #endregion
        /// <summary>
        /// Disposes the Vertex Array
        /// </summary>
        public void Dispose() {
            gl.DeleteVertexArray(this._arrayId);
        }
    }
}
