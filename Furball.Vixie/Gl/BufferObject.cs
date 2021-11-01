using System;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Gl {
    /// <summary>
    /// OpenGL Buffer Objecct
    /// </summary>
    public class BufferObject {
        /// <summary>
        /// Unique Identifier for this Buffer object used by OpenGL to distingluish different buffers
        /// </summary>
        private uint            _bufferId;
        /// <summary>
        /// Type of Buffer, is it a Vertex Buffer? a Index Buffer? a different buffer entirely?
        /// </summary>
        private BufferTargetARB _bufferType;
        /// <summary>
        /// How is this buffer going to be used?
        /// </summary>
        private BufferUsageARB _bufferUsage;
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
        public unsafe BufferObject(nuint size, BufferTargetARB bufferType, BufferUsageARB usage = BufferUsageARB.StaticDraw) {
            gl                = Global.Gl;
            this._bufferType  = bufferType;
            this._bufferUsage = usage;
            //Generate Buffer
            this._bufferId = gl.GenBuffer();
            //Select buffer, as we're going to put data there now
            gl.BindBuffer(this._bufferType, this._bufferId);
            //Allocate Memory
            gl.BufferData(this._bufferType, size, null, this._bufferUsage);
        }

        public BufferObject(BufferTargetARB bufferType, BufferUsageARB usage = BufferUsageARB.StaticDraw) {
            gl                = Global.Gl;
            this._bufferType  = bufferType;
            this._bufferUsage = usage;
            //Generate Buffer
            this._bufferId = gl.GenBuffer();
            //Select buffer, as we're going to put data there now
            gl.BindBuffer(this._bufferType, this._bufferId);
        }

        public unsafe BufferObject SetData(void* data, nuint size) {
            gl.BufferData(this._bufferType, size, data, this._bufferUsage);

            return this;
        }

        public static unsafe BufferObject CreateNew<pDataType>(Span<pDataType> data, BufferTargetARB bufferType, BufferUsageARB usage = BufferUsageARB.StaticDraw)
            where pDataType : unmanaged
        {
            BufferObject bufferObject = new BufferObject(bufferType, usage);

            fixed (void* d = data) {
                bufferObject.SetData(d, (nuint)(data.Length * sizeof(pDataType)));
            }

            bufferObject.DataCount = (uint) data.Length;

            return bufferObject;
        }

        /// <summary>
        /// Selects this Buffer
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        public BufferObject Bind() {
            gl.BindBuffer(this._bufferType, this._bufferId);

            return this;
        }

        public BufferObject Unbind() {
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
