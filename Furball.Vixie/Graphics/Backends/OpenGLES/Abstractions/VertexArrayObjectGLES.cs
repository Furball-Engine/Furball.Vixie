using System;
using Furball.Vixie.Graphics.Backends.OpenGL_;
using Silk.NET.OpenGLES;

namespace Furball.Vixie.Graphics.Backends.OpenGLES.Abstractions {
    public class VertexArrayObjectGLES : IDisposable {
        private readonly OpenGLESBackend _backend;
        /// <summary>
        /// Current Bound VAO
        /// </summary>
        internal static VertexArrayObjectGLES CurrentlyBound;
        /// <summary>
        /// Getter to check whether this VAO is bound
        /// </summary>
        public bool Bound => CurrentlyBound == this;
        /// <summary>
        /// OpenGL Api, used to not have to write Global.GL.function everytime
        /// </summary>
        private GL gl;
        /// <summary>
        /// Unique Identifier for this Array Object
        /// </summary>
        internal uint ArrayId;

        public VertexArrayObjectGLES(OpenGLESBackend backend) {
            this._backend = backend;
            this._backend.CheckThread();

            this.gl = backend.GetGlApi();
            //Generate Vertex Array
            this.ArrayId = this.gl.GenVertexArray();
            this._backend.CheckError();
        }

        ~VertexArrayObjectGLES() {
            DisposeQueue.Enqueue(this);
        }

        /// <summary>
        /// Adds a VertexBuffer with a certain Layout to this Vertex Array
        /// </summary>
        /// <param name="vertexBuffer">Vertex Buffer to add</param>
        /// <param name="layoutGles">Layout of said Vertex Buffer</param>
        public unsafe VertexArrayObjectGLES AddBuffer(BufferObjectGL vertexBuffer, VertexBufferLayoutGLES layoutGles) {
            this._backend.CheckThread();
            
            //Bind both this and the Vertex Buffer
            this.Bind();
            vertexBuffer.Bind();
            //Get all the elements
            var elements = layoutGles.GetElements();

            uint offset = 0;
            //Loop over the elements
            for (uint i = 0; i != elements.Count; i++) {
                LayoutElement currentElement = elements[(int) i];
                //Define the Layout of this Element
                this.gl.EnableVertexAttribArray(i);

                if (currentElement.Type != VertexAttribPointerType.Int)
                    this.gl.VertexAttribPointer(i, currentElement.Count, currentElement.Type, currentElement.Normalized, layoutGles.GetStride(), (void*)offset);
                else
                    this.gl.VertexAttribIPointer(i, currentElement.Count, VertexAttribIType.Int, layoutGles.GetStride(), (void*)offset);

                offset += (uint) currentElement.Count * LayoutElement.GetSizeOfType(currentElement.Type);
            }
            this._backend.CheckError();

            return this;
        }
        /// <summary>
        /// Binds or Selects this current Vertex Array
        /// </summary>
        public VertexArrayObjectGLES Bind() {
            this._backend.CheckThread();
            
            if (this.Locked)
                return null;

            this.gl.BindVertexArray(this.ArrayId);
            this._backend.CheckError();

            CurrentlyBound = this;

            return this;
        }

        /// <summary>
        /// Indicates whether Object is Locked or not,
        /// This is done internally to not be able to switch VAOs while a Batch is happening
        /// or really anything that would possibly get screwed over by switching VAOs
        /// </summary>
        internal bool Locked = false;

        /// <summary>
        /// Binds and sets a Lock so that the Texture cannot be unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal VertexArrayObjectGLES LockingBind() {
            this.Bind();
            this.Lock();

            return this;
        }
        /// <summary>
        /// Locks the Texture so that other Textures cannot be bound/unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal VertexArrayObjectGLES Lock() {
            this.Locked = true;

            return this;
        }
        /// <summary>
        /// Unlocks the Texture, so that other Textures can be bound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal VertexArrayObjectGLES Unlock() {
            this.Locked = false;

            return this;
        }
        /// <summary>
        /// Uninds and unlocks the Texture so that other Textures can be bound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal VertexArrayObjectGLES UnlockingUnbind() {
            this.Unlock();
            this.Unbind();

            return this;
        }

        /// <summary>
        /// Unbinds all Vertex Arrays
        /// </summary>
        public VertexArrayObjectGLES Unbind() {
            this._backend.CheckThread();
            
            if (this.Locked)
                return null;

            this.gl.BindVertexArray(0);
            this._backend.CheckError();

            CurrentlyBound = null;

            return this;
        }

        private bool _isDisposed = false;

        /// <summary>
        /// Disposes this Vertex Array
        /// </summary>
        public void Dispose() {
            this._backend.CheckThread();
            
            if (this.Bound)
                this.UnlockingUnbind();

            if(this._isDisposed)
                return;

            this._isDisposed = true;

            try {
                this.gl.DeleteVertexArray(this.ArrayId);
                this._backend.CheckError();
            }
            catch {

            }
        }
    }
}
