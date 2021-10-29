using System;
using Silk.NET.OpenGL;
using Gl=Furball.Vixie.Global;

namespace Furball.Vixie.Gl {
    public class VertexBuffer<pT> : IDisposable where pT : unmanaged {
        /// <summary>
        /// OpenGL's unique ID for this Vertex Buffer
        /// </summary>
        private uint           _bufferId;
        /// <summary>
        /// OpenGL API
        /// </summary>
        private GL             gl;
        /// <summary>
        /// Buffer Usage hint for the OpenGL implementation
        /// </summary>
        private BufferUsageARB _usage;

        private pT[] _data;

        /// <summary>
        /// Creates a Block of Memory on the GPU
        /// </summary>
        /// <param name="usage">How is this Buffer going to be used?</param>
        public VertexBuffer(BufferUsageARB usage) {
            this.gl = Global.Gl;

            this._usage = usage;

            gl.GenBuffers(1, out this._bufferId);
        }

        /// <summary>
        /// Binds or Selects this buffer
        /// </summary>
        public VertexBuffer<pT> Bind() {
            gl.BindBuffer(GLEnum.ArrayBuffer, this._bufferId);

            return this;
        }

        /// <summary>
        /// Sets the Data of the Buffer
        /// <remarks>Buffer has to be bound for SetData to function</remarks>
        /// </summary>
        /// <param name="data">Data to Upload to the Buffer</param>
        public unsafe VertexBuffer<pT> SetData(pT[] data) {
            fixed (void* d = data)
            {
                gl.BufferData(GLEnum.ArrayBuffer, (nuint) (data.Length * sizeof(pT)), d, BufferUsageARB.StaticDraw);
            }

            this._data = data;

            return this;
        }

        #region Attributes

        private uint _attribIndex  = 0;
        private uint _attribOffset = 0;

        public unsafe void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offSet)
        {
            gl.VertexAttribPointer(index, count, type, false, vertexSize * (uint)sizeof(pT), (void*)(offSet * sizeof(pT)));
            gl.EnableVertexAttribArray(index);
        }

        public unsafe VertexBuffer<pT> AddAttribute<pAttribType>(int size) where pAttribType : unmanaged {
            GLEnum type = Type.GetTypeCode(typeof(pAttribType)) switch {
                TypeCode.Single => GLEnum.Float,
                TypeCode.Byte   => GLEnum.Byte,
                TypeCode.UInt32 => GLEnum.UnsignedInt,
                TypeCode.Int16  => GLEnum.Short,
                TypeCode.UInt16 => GLEnum.UnsignedShort,
                TypeCode.Int32  => GLEnum.Int
            };

            uint stride = (uint) (this._data.Length * sizeof(pAttribType));

            //gl.VertexAttribPointer(this._attribIndex, size, type, false, stride, 1);


            gl.VertexAttribPointer(this._attribIndex, size, type, false, stride, (void*)(this._attribOffset));

            gl.EnableVertexAttribArray(this._attribIndex);

            this._attribOffset += stride;
            this._attribIndex++;

            return this;
        }

        #endregion
        
        public void Dispose() {
            gl.DeleteBuffer(this._bufferId);
        }
    }
}
