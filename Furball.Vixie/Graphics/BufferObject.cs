using System;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGLES;

namespace Furball.Vixie.Graphics {
    /// <summary>
    /// OpenGL Buffer Objecct
    /// </summary>
    public class BufferObject : IDisposable {
        internal static BufferObject CurrentlyBound;
        public bool Bound => CurrentlyBound == this;

        /// <summary>
        /// Unique Identifier for this Buffer object used by OpenGL to distingluish different buffers
        /// </summary>
        internal uint            BufferId;
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
        public uint DataCount { get; set; }
        /// <summary>
        /// Creates a Empty buffer of size `size`
        /// </summary>
        /// <param name="size">Size of the Buffer</param>
        /// <param name="bufferType">What kind of buffer is it?</param>
        /// <param name="usage">How is this buffer going to be used?</param>
        public unsafe BufferObject(int size, BufferTargetARB bufferType, BufferUsageARB usage = BufferUsageARB.StreamDraw) {
            OpenGLHelper.CheckThread();
            
            this.gl                = Global.Gl;
            this._bufferType  = bufferType;
            this._bufferUsage = usage;
            //Generate Buffer
            this.BufferId = this.gl.GenBuffer();
            //Select buffer, as we're going to allocate memory in it
            this.gl.BindBuffer(this._bufferType, this.BufferId);
            //Allocate Memory
            this.gl.BufferData(this._bufferType, (nuint) size, null, this._bufferUsage);
            OpenGLHelper.CheckError();
        }
        /// <summary>
        /// Creates an uninitialized buffer
        /// </summary>
        /// <param name="bufferType">What kind of Buffer is it</param>
        /// <param name="usage">How is this buffer going to be used?</param>
        public BufferObject(BufferTargetARB bufferType, BufferUsageARB usage = BufferUsageARB.StreamDraw) {
            OpenGLHelper.CheckThread();
            
            this.gl                = Global.Gl;
            this._bufferType  = bufferType;
            this._bufferUsage = usage;
            //Generate Buffer
            this.BufferId = this.gl.GenBuffer();
            OpenGLHelper.CheckError();
        }

        /// <summary>
        /// Puts data into the Buffer
        /// </summary>
        /// <param name="data">Data to put there</param>
        /// <param name="size">Size of the Data</param>
        /// <returns></returns>
        public unsafe BufferObject SetData(void* data, nuint size) {
            OpenGLHelper.CheckThread();
            
            this.gl.BufferData(this._bufferType, size, data, this._bufferUsage);
            OpenGLHelper.CheckError();

            return this;
        }

        public unsafe BufferObject SetSubData(void* data, nuint size, nint offset = 0) {
            OpenGLHelper.CheckThread();
            
            this.gl.BufferSubData(this._bufferType, offset, size, data);
            OpenGLHelper.CheckError();

            return this;
        }

        public unsafe BufferObject SetSubData<pDataType>(Span<pDataType> data) where pDataType : unmanaged {
            fixed (void* d = data) {
                this.SetSubData(d, (nuint)(data.Length * sizeof(pDataType)));
            }
            OpenGLHelper.CheckError();

            return this;
        }

        /// <summary>
        /// Puts data into the buffer in a easier way
        /// </summary>
        /// <param name="data">Data to put</param>
        /// <typeparam name="pDataType">Type of data to put</typeparam>
        /// <returns>Self, used for chaining Methods</returns>
        public unsafe BufferObject SetData<pDataType>(Span<pDataType> data) where pDataType : unmanaged {
            fixed (void* d = data) {
                this.SetData(d, (nuint)(data.Length * sizeof(pDataType)));
            }
            OpenGLHelper.CheckError();

            return this;
        }
        /// <summary>
        /// Creates a BufferObject with the old constructor
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="bufferType">What type of buffer is it?</param>
        /// <param name="usage">How is this buffer going to be used?</param>
        /// <typeparam name="pDataType">Type of Data to initially store</typeparam>
        /// <returns>Self, used for chaining Methods</returns>
        public static unsafe BufferObject CreateNew<pDataType>(Span<pDataType> data, BufferTargetARB bufferType, BufferUsageARB usage = BufferUsageARB.StreamDraw)
            where pDataType : unmanaged
        {
            BufferObject bufferObject = new BufferObject(bufferType, usage);
            bufferObject.Bind();

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
            OpenGLHelper.CheckThread();
            
            if (this.Locked)
                return null;

            this.gl.BindBuffer(this._bufferType, this.BufferId);
            OpenGLHelper.CheckError();

            CurrentlyBound = this;

            return this;
        }

        /// <summary>
        /// Indicates whether Object is Locked or not,
        /// This is done internally to not be able to switch Buffers while a Batch is happening
        /// or really anything that would possibly get screwed over by switching Buffers
        /// </summary>
        internal bool Locked = false;

        /// <summary>
        /// Binds and sets a Lock so that the Buffer cannot be unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal BufferObject LockingBind() {
            this.Bind();
            this.Lock();

            return this;
        }

        /// <summary>
        /// Locks the Buffer so that other Buffers cannot be bound/unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal BufferObject Lock() {
            this.Locked = true;

            return this;
        }

        /// <summary>
        /// Unlocks the Buffer, so that other buffers can be bound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal BufferObject Unlock() {
            this.Locked = false;

            return this;
        }
        /// <summary>
        /// Uninds and unlocks the Buffer so that other buffers can be bound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal BufferObject UnlockingUnbind() {
            this.Unlock();
            this.Unbind();

            return this;
        }
        /// <summary>
        /// Unbinds any bound Buffer
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        public BufferObject Unbind() {
            OpenGLHelper.CheckThread();
            if (this.Locked)
                return null;

            this.gl.BindBuffer(this._bufferType, 0);
            OpenGLHelper.CheckError();

            CurrentlyBound = null;

            return this;
        }
        /// <summary>
        /// Disposes the Buffer
        /// </summary>
        public void Dispose() {
            if (this.Bound)
                this.UnlockingUnbind();

            try {
                this.gl.DeleteBuffer(this.BufferId);
            }
            catch {

            }
            OpenGLHelper.CheckError();
        }
    }
}
