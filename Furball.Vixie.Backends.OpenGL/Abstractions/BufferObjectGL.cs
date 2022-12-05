using System;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Backends.OpenGL.Abstractions; 

internal sealed class BufferObjectGl : IDisposable {
    internal static BufferObjectGl CurrentlyBound;
    public          bool           Bound => CurrentlyBound == this;

    /// <summary>
    /// Unique Identifier for this Buffer object used by OpenGL to distingluish different buffers
    /// </summary>
    internal uint BufferId;
    /// <summary>
    /// Backend to which this belongs to
    /// </summary>
    private readonly IGlBasedBackend _backend;
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
    public unsafe BufferObjectGl(IGlBasedBackend backend, int size, BufferTargetARB bufferType, BufferUsageARB usage = BufferUsageARB.StreamDraw) {
        this._backend = backend;
        this._backend.GlCheckThread();

        this._bufferType  = bufferType;
        this._bufferUsage = usage;
        //Generate Buffer
        this.BufferId = this._backend.GenBuffer();
        //Select buffer, as we're going to allocate memory in it
        this._backend.BindBuffer(this._bufferType, this.BufferId);
        //Allocate Memory
        this._backend.BufferData(this._bufferType, (nuint) size, null, this._bufferUsage);
        this._backend.CheckError("create buffer object");
    }
    /// <summary>
    /// Creates an uninitialized buffer
    /// </summary>
    /// <param name="backend">OpenGL backend to which this belongs to</param>
    /// <param name="bufferType">What kind of Buffer is it</param>
    /// <param name="usage">How is this buffer going to be used?</param>
    public BufferObjectGl(IGlBasedBackend backend, BufferTargetARB bufferType, BufferUsageARB usage = BufferUsageARB.StreamDraw) {
        this._backend = backend;
        this._backend.GlCheckThread();

        this._bufferType  = bufferType;
        this._bufferUsage = usage;
        //Generate Buffer
        this.BufferId = this._backend.GenBuffer();
        this._backend.CheckError("create buffer");
    }

    public delegate void VoidDelegate();

    ~BufferObjectGl() {
        DisposeQueue.Enqueue(this);
    }

    /// <summary>
    /// Puts data into the Buffer
    /// </summary>
    /// <param name="data">Data to put there</param>
    /// <param name="size">Size of the Data</param>
    /// <returns></returns>
    public unsafe BufferObjectGl SetData(void* data, nuint size) {
        this._backend.GlCheckThread();
        this._backend.BufferData(this._bufferType, size, data, this._bufferUsage);
        this._backend.CheckError("set buffer data");

        return this;
    }

    public unsafe BufferObjectGl SetSubData(void* data, nuint size, nint offset = 0) {
        this._backend.GlCheckThread();
        this._backend.BufferSubData(this._bufferType, offset, size, data);
        this._backend.CheckError("set sub buffer data");

        return this;
    }

    public unsafe BufferObjectGl SetSubData<pDataType>(Span<pDataType> data) where pDataType : unmanaged {
        this._backend.GlCheckThread();
        fixed (void* d = data) {
            this.SetSubData(d, (nuint)(data.Length * sizeof(pDataType)));
        }
        this._backend.CheckError("set sub buffer data 2");

        return this;
    }
    
    public unsafe BufferObjectGl SetSubData<pDataType>(Span<pDataType> data, int count) where pDataType : unmanaged {
        this._backend.GlCheckThread();
        
        //Make sure that the count is not bigger than the span
        Guard.Assert(data.Length >= count, "data.Length >= count");
        
        fixed (void* d = data) {
            this.SetSubData(d, (nuint)(count * sizeof(pDataType)));
        }
        this._backend.CheckError("set sub buffer data 2");

        return this;
    }

    /// <summary>
    /// Puts data into the buffer in a easier way
    /// </summary>
    /// <param name="data">Data to put</param>
    /// <typeparam name="pDataType">Type of data to put</typeparam>
    /// <returns>Self, used for chaining Methods</returns>
    public unsafe BufferObjectGl SetData<pDataType>(Span<pDataType> data) where pDataType : unmanaged {
        this._backend.GlCheckThread();
        fixed (void* d = data) {
            this.SetData(d, (nuint)(data.Length * sizeof(pDataType)));
        }
        this._backend.CheckError("set data 2");

        return this;
    }
    /// <summary>
    /// Creates a BufferObject with the old constructor
    /// </summary>
    /// <param name="backend">The OpenGL backend</param>
    /// <param name="data">Data</param>
    /// <param name="bufferType">What type of buffer is it?</param>
    /// <param name="usage">How is this buffer going to be used?</param>
    /// <typeparam name="pDataType">Type of Data to initially store</typeparam>
    /// <returns>Self, used for chaining Methods</returns>
    public static unsafe BufferObjectGl CreateNew<pDataType>(IGlBasedBackend backend, Span<pDataType> data, BufferTargetARB bufferType, BufferUsageARB usage = BufferUsageARB.StreamDraw)
        where pDataType : unmanaged
    {
        backend.GlCheckThread();
        BufferObjectGl bufferObjectGl = new(backend, bufferType, usage);
        bufferObjectGl.Bind();

        fixed (void* d = data) {
            bufferObjectGl.SetData(d, (nuint)(data.Length * sizeof(pDataType)));
        }

        bufferObjectGl.DataCount = (uint) data.Length;

        return bufferObjectGl;
    }

    /// <summary>
    /// Selects this Buffer
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    public BufferObjectGl Bind() {
        this._backend.GlCheckThread();
        if (this.Locked)
            return null;

        this._backend.BindBuffer(this._bufferType, this.BufferId);
        this._backend.CheckError("bind buffer");

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
    public BufferObjectGl LockingBind() {
        this._backend.GlCheckThread();
        this.Bind();
        this.Lock();

        return this;
    }

    /// <summary>
    /// Locks the Buffer so that other Buffers cannot be bound/unbound/rebound
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    internal BufferObjectGl Lock() {
        this._backend.GlCheckThread();
        this.Locked = true;

        return this;
    }

    /// <summary>
    /// Unlocks the Buffer, so that other buffers can be bound
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    public BufferObjectGl Unlock() {
        this._backend.GlCheckThread();
        this.Locked = false;

        return this;
    }
    /// <summary>
    /// Uninds and unlocks the Buffer so that other buffers can be bound/rebound
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    internal BufferObjectGl UnlockingUnbind() {
        this._backend.GlCheckThread();
        this.Unlock();
        this.Unbind();

        return this;
    }
    /// <summary>
    /// Unbinds any bound Buffer
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    public BufferObjectGl Unbind() {
        this._backend.GlCheckThread();
        if (this.Locked)
            return null;

        this._backend.BindBuffer(this._bufferType, 0);
        this._backend.CheckError("unbind buffer");

        CurrentlyBound = null;

        return this;
    }

    private bool _isDisposed = false;
    public BufferObjectGl(IGlBasedBackend backend, int                               maxVerticies, Silk.NET.OpenGLES.BufferTargetARB arrayBuffer) : this(backend, maxVerticies, (BufferTargetARB)arrayBuffer) {}
    public BufferObjectGl(IGlBasedBackend backend, Silk.NET.OpenGLES.BufferTargetARB target,       Silk.NET.OpenGLES.BufferUsageARB  usage = Silk.NET.OpenGLES.BufferUsageARB.StreamDraw) : this(backend, (BufferTargetARB)target, (BufferUsageARB)usage) {}

    /// <summary>
    /// Disposes the Buffer
    /// </summary>
    public void Dispose() {
        this._backend.GlCheckThread();
        if (this.Bound)
            this.UnlockingUnbind();

        if (this._isDisposed)
            return;

        this._isDisposed = true;

        this._backend.DeleteBuffer(this.BufferId);
        this._backend.CheckError("dispose buffer");
        GC.SuppressFinalize(this);
    }
}