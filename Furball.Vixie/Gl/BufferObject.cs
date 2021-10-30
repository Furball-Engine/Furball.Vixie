using System;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Gl {
    /// <summary>
    /// OpenGL Buffer Objecct
    /// </summary>
    /// <typeparam name="pDataType">What type of data is it going to store?</typeparam>
    public class BufferObject<pDataType> : IDisposable where pDataType : unmanaged {
        /// <summary>
        /// Unique Identifier for this Buffer object used by OpenGL to distingluish different buffers
        /// </summary>
        private uint            _bufferId;
        /// <summary>
        /// Type of Buffer, is it a Vertex Buffer? a Index Buffer? a different buffer entirely?
        /// </summary>
        private BufferTargetARB _bufferType;
        /// <summary>
        /// OpenGL api, used to not have to do Global.Gl.function everytime, saves time and makes code shorter
        /// </summary>
        private GL gl;
        /// <summary>
        /// Amount of Data supplied in Constructor
        /// </summary>
        public uint DataCount { get; internal set; }
        /// <summary>
        /// Creates a Buffer Object of type `bufferType`
        /// </summary>
        /// <param name="data">Data to put into the Buffer</param>
        /// <param name="bufferType">What kind of buffer is it?</param>
        public unsafe BufferObject(Span<pDataType> data, BufferTargetARB bufferType) {
            gl               = Global.Gl;
            this._bufferType = bufferType;
            //Generate Buffer
            this._bufferId = gl.GenBuffer();
            //Select buffer, as we're going to put data there now
            gl.BindBuffer(this._bufferType, this._bufferId);

            //Convert Data to void*
            fixed (void* d = data) {
                //Put the Data into the Buffer
                gl.BufferData(this._bufferType, (nuint) (data.Length * sizeof(pDataType)), d, BufferUsageARB.StaticDraw);
            }

            this.DataCount = (uint) data.Length;
        }
        /// <summary>
        /// Selects this Buffer
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        public BufferObject<pDataType> Bind() {
            gl.BindBuffer(this._bufferType, this._bufferId);

            return this;
        }

        public BufferObject<pDataType> Unbind() {
            gl.BindBuffer(this._bufferType, 0);

            return this;
        }
        /// <summary>
        /// Disposes the Buffer
        /// </summary>
        public void Dispose() {
            gl.DeleteBuffer(this._bufferId);
        }
    }
}
