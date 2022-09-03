using System;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Backends.OpenGL.Abstractions; 

internal sealed class VertexArrayObjectGl : IDisposable {
    private readonly IGlBasedBackend _backend;
    /// <summary>
    /// Current Bound VAO
    /// </summary>
    internal static VertexArrayObjectGl CurrentlyBound;
    /// <summary>
    /// Getter to check whether this VAO is bound
    /// </summary>
    public bool Bound => CurrentlyBound == this;
    /// <summary>
    /// Unique Identifier for this Array Object
    /// </summary>
    internal uint ArrayId;

    public VertexArrayObjectGl(IGlBasedBackend backend) {
        this._backend = backend;
        this._backend.GlCheckThread();

        //Generate Vertex Array
        this.ArrayId = this._backend.GenVertexArray();
        this._backend.CheckError("gen vertex arr");
    }

    ~VertexArrayObjectGl() {
        DisposeQueue.Enqueue(this);
    }

    /// <summary>
    /// Adds a VertexBuffer with a certain Layout to this Vertex Array
    /// </summary>
    /// <param name="vertexBuffer">Vertex Buffer to add</param>
    /// <param name="layoutGl41">Layout of said Vertex Buffer</param>
    public unsafe VertexArrayObjectGl AddBuffer(BufferObjectGl vertexBuffer, VertexBufferLayoutGl layoutGl41, uint iOffset = 0) {
        this._backend.GlCheckThread();
        //Bind both this and the Vertex Buffer
        this.Bind();
        vertexBuffer.Bind();
        //Get all the elements
        var elements = layoutGl41.GetElements();

        uint offset = 0;
        //Loop over the elements
        for (uint i = 0; i != elements.Count; i++) {
            LayoutElement currentElement = elements[(int) i];
            //Define the Layout of this Element
            this._backend.EnableVertexAttribArray(i + iOffset);
            this._backend.CheckError("vertexattribarr");

            if (currentElement.Type != VertexAttribPointerType.Int)
                this._backend.VertexAttribPointer(i + iOffset, currentElement.Count, currentElement.Type, currentElement.Normalized, layoutGl41.GetStride(), (void*)offset);
            else
                this._backend.VertexAttribIPointer(i + iOffset, currentElement.Count, VertexAttribIType.Int, layoutGl41.GetStride(), (void*)offset);
            this._backend.CheckError("vertex attrib ptr");

            if (currentElement.InstanceDivisor != uint.MaxValue)
                this._backend.VertexAttribDivisor(i + iOffset, currentElement.InstanceDivisor);

            offset += (uint) currentElement.Count * LayoutElement.GetSizeOfType(currentElement.Type);
        }
        this._backend.CheckError("add layout to buffer");

        return this;
    }
    /// <summary>
    /// Binds or Selects this current Vertex Array
    /// </summary>
    public VertexArrayObjectGl Bind() {
        this._backend.GlCheckThread();
        if (this.Locked)
            return null;

        this._backend.BindVertexArray(this.ArrayId);
        this._backend.CheckError("bind VAO");

        CurrentlyBound = this;

        return this;
    }

    /// <summary>
    /// Indicates whether Object is Locked or not,
    /// This is done internally to not be able to switch VAOs while a Batch is happening
    /// or really anything that would possibly get screwed over by switching VAOs
    /// </summary>
    public bool Locked = false;

    /// <summary>
    /// Binds and sets a Lock so that the Texture cannot be unbound/rebound
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    public VertexArrayObjectGl LockingBind() {
        this._backend.GlCheckThread();
        this.Bind();
        this.Lock();

        return this;
    }
    /// <summary>
    /// Locks the Texture so that other Textures cannot be bound/unbound/rebound
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    internal VertexArrayObjectGl Lock() {
        this._backend.GlCheckThread();
        this.Locked = true;

        return this;
    }
    /// <summary>
    /// Unlocks the Texture, so that other Textures can be bound
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    public VertexArrayObjectGl Unlock() {
        this._backend.GlCheckThread();
        this.Locked = false;

        return this;
    }
    /// <summary>
    /// Uninds and unlocks the Texture so that other Textures can be bound/rebound
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    internal VertexArrayObjectGl UnlockingUnbind() {
        this._backend.GlCheckThread();
        this.Unlock();
        this.Unbind();

        return this;
    }

    /// <summary>
    /// Unbinds all Vertex Arrays
    /// </summary>
    public VertexArrayObjectGl Unbind() {
        this._backend.GlCheckThread();
        if (this.Locked)
            return null;

        this._backend.BindVertexArray(0);
        this._backend.CheckError("unbind VAO");

        CurrentlyBound = null;

        return this;
    }

    private bool _isDisposed = false;

    /// <summary>
    /// Disposes this Vertex Array
    /// </summary>
    public void Dispose() {
        this._backend.GlCheckThread();

        if (this.Bound)
            this.UnlockingUnbind();

        if(this._isDisposed)
            return;

        this._isDisposed = true;

        this._backend.DeleteVertexArray(this.ArrayId);
        this._backend.CheckError("dispose VAO");
        
        GC.SuppressFinalize(this);
    }
}