using System;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Backends.OpenGL.Shared {
    public class BufferObjectGL : IDisposable {
        internal static BufferObjectGL CurrentlyBound;
        public bool Bound => CurrentlyBound == this;

        /// <summary>
        /// Unique Identifier for this Buffer object used by OpenGL to distingluish different buffers
        /// </summary>
        internal uint            BufferId;
        /// <summary>
        /// Backend to which this belongs to
        /// </summary>
        private readonly IGLBasedBackend _backend;
        /// <summary>
        /// Type of Buffer, is it a Vertex Buffer? a Index Buffer? a different buffer entirely?
        /// </summary>
        private BufferTargetARB _bufferType;
        /// <summary>
        /// How is this buffer going to be used?
        /// </summary>
        private BufferUsageARB _bufferUsage;
        /// <summary>
        /// Amount of Data supplied in Constructor
        /// </summary>
        public uint DataCount { get; set; }
        /// <summary>
        /// Creates a Empty buffer of size `size`
        /// </summary>
        /// <param name="backend">OpenGL backend to which this belongs to</param>
        /// <param name="size">Size of the Buffer</param>
        /// <param name="bufferType">What kind of buffer is it?</param>
        /// <param name="usage">How is this buffer going to be used?</param>
        public unsafe BufferObjectGL(IGLBasedBackend backend, int size, BufferTargetARB bufferType, BufferUsageARB usage = BufferUsageARB.StreamDraw) {
            this._backend     = backend;
            
            this._bufferType  = bufferType;
            this._bufferUsage = usage;
            //Generate Buffer
            this.BufferId = this._backend.GenBuffer();
            //Select buffer, as we're going to allocate memory in it
            this._backend.BindBuffer(this._bufferType, this.BufferId);
            //Allocate Memory
            this._backend.BufferData(this._bufferType, (nuint) size, null, this._bufferUsage);
            this._backend.CheckError();
        }
        /// <summary>
        /// Creates an uninitialized buffer
        /// </summary>
        /// <param name="backend">OpenGL backend to which this belongs to</param>
        /// <param name="bufferType">What kind of Buffer is it</param>
        /// <param name="usage">How is this buffer going to be used?</param>
        public BufferObjectGL(IGLBasedBackend backend, BufferTargetARB bufferType, BufferUsageARB usage = BufferUsageARB.StreamDraw) {
            this._backend     = backend;
            
            this._bufferType  = bufferType;
            this._bufferUsage = usage;
            //Generate Buffer
            this.BufferId = this._backend.GenBuffer();
            this._backend.CheckError();
        }

        public delegate void VoidDelegate();

        ~BufferObjectGL() {
            DisposeQueue.Enqueue(this);
        }

        /// <summary>
        /// Puts data into the Buffer
        /// </summary>
        /// <param name="data">Data to put there</param>
        /// <param name="size">Size of the Data</param>
        /// <returns></returns>
        public unsafe BufferObjectGL SetData(void* data, nuint size) {
            this._backend.BufferData(this._bufferType, size, data, this._bufferUsage);
            this._backend.CheckError();

            return this;
        }

        public unsafe BufferObjectGL SetSubData(void* data, nuint size, nint offset = 0) {
            this._backend.BufferSubData(this._bufferType, offset, size, data);
            this._backend.CheckError();

            return this;
        }

        public unsafe BufferObjectGL SetSubData<pDataType>(Span<pDataType> data) where pDataType : unmanaged {
            fixed (void* d = data) {
                this.SetSubData(d, (nuint)(data.Length * sizeof(pDataType)));
            }
            this._backend.CheckError();

            return this;
        }

        /// <summary>
        /// Puts data into the buffer in a easier way
        /// </summary>
        /// <param name="data">Data to put</param>
        /// <typeparam name="pDataType">Type of data to put</typeparam>
        /// <returns>Self, used for chaining Methods</returns>
        public unsafe BufferObjectGL SetData<pDataType>(Span<pDataType> data) where pDataType : unmanaged {
            fixed (void* d = data) {
                this.SetData(d, (nuint)(data.Length * sizeof(pDataType)));
            }
            this._backend.CheckError();

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
        public static unsafe BufferObjectGL CreateNew<pDataType>(IGLBasedBackend backend, Span<pDataType> data, BufferTargetARB bufferType, BufferUsageARB usage = BufferUsageARB.StreamDraw)
            where pDataType : unmanaged
        {
            BufferObjectGL BufferObjectGL = new BufferObjectGL(backend, bufferType, usage);
            BufferObjectGL.Bind();

            fixed (void* d = data) {
                BufferObjectGL.SetData(d, (nuint)(data.Length * sizeof(pDataType)));
            }

            BufferObjectGL.DataCount = (uint) data.Length;

            return BufferObjectGL;
        }

        /// <summary>
        /// Selects this Buffer
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        public BufferObjectGL Bind() {
            if (this.Locked)
                return null;

            this._backend.BindBuffer(this._bufferType, this.BufferId);
            this._backend.CheckError();

            CurrentlyBound = this;

            return this;
        }

        /// <summary>
        /// Indicates whether Object is Locked or not,
        /// This is done internally to not be able to switch Buffers while a Batch is happening
        /// or really anything that would possibly get screwed over by switching Buffers
        /// </summary>
        public bool Locked = false;

        /// <summary>
        /// Binds and sets a Lock so that the Buffer cannot be unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        public BufferObjectGL LockingBind() {
            this.Bind();
            this.Lock();

            return this;
        }

        /// <summary>
        /// Locks the Buffer so that other Buffers cannot be bound/unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal BufferObjectGL Lock() {
            this.Locked = true;

            return this;
        }

        /// <summary>
        /// Unlocks the Buffer, so that other buffers can be bound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        public BufferObjectGL Unlock() {
            this.Locked = false;

            return this;
        }
        /// <summary>
        /// Uninds and unlocks the Buffer so that other buffers can be bound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal BufferObjectGL UnlockingUnbind() {
            this.Unlock();
            this.Unbind();

            return this;
        }
        /// <summary>
        /// Unbinds any bound Buffer
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        public BufferObjectGL Unbind() {
            if (this.Locked)
                return null;

            this._backend.BindBuffer(this._bufferType, 0);
            this._backend.CheckError();

            CurrentlyBound = null;

            return this;
        }

        private bool _isDisposed = false;
        public BufferObjectGL(IGLBasedBackend backend, int                               maxVerticies, Silk.NET.OpenGLES.BufferTargetARB arrayBuffer) : this(backend, maxVerticies, (BufferTargetARB)arrayBuffer) {}
        public BufferObjectGL(IGLBasedBackend backend, Silk.NET.OpenGLES.BufferTargetARB target,       Silk.NET.OpenGLES.BufferUsageARB  usage = Silk.NET.OpenGLES.BufferUsageARB.StreamDraw) : this(backend, (BufferTargetARB)target, (BufferUsageARB)usage) {}

        /// <summary>
        /// Disposes the Buffer
        /// </summary>
        public void Dispose() {
            if (this.Bound)
                this.UnlockingUnbind();

            if (this._isDisposed)
                return;

            this._isDisposed = true;

            try {
                this._backend.DeleteBuffer(this.BufferId);
            }
            catch {

            }
            this._backend.CheckError();
        }
    }
}
