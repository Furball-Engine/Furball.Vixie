using System;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Graphics.Backends.OpenGL {
    public class VertexArrayObjectGL : IDisposable {
        private readonly IGLBasedBackend _backend;
        /// <summary>
        /// Current Bound VAO
        /// </summary>
        internal static VertexArrayObjectGL CurrentlyBound;
        /// <summary>
        /// Getter to check whether this VAO is bound
        /// </summary>
        public bool Bound => CurrentlyBound == this;
        /// <summary>
        /// Unique Identifier for this Array Object
        /// </summary>
        internal uint ArrayId;

        public VertexArrayObjectGL(IGLBasedBackend backend) {
            this._backend = backend;
            
            //Generate Vertex Array
            this.ArrayId = this._backend.GenVertexArray();
            this._backend.CheckError();
        }

        ~VertexArrayObjectGL() {
            DisposeQueue.Enqueue(this);
        }

        /// <summary>
        /// Adds a VertexBuffer with a certain Layout to this Vertex Array
        /// </summary>
        /// <param name="vertexBuffer">Vertex Buffer to add</param>
        /// <param name="layoutGl41">Layout of said Vertex Buffer</param>
        public unsafe VertexArrayObjectGL AddBuffer(BufferObjectGL vertexBuffer, VertexBufferLayoutGL layoutGl41) {
            
            
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
                this._backend.EnableVertexAttribArray(i);

                if (currentElement.Type != VertexAttribPointerType.Int)
                    this._backend.VertexAttribPointer(i, currentElement.Count, currentElement.Type, currentElement.Normalized, layoutGl41.GetStride(), (void*)offset);
                else
                    this._backend.VertexAttribIPointer(i, currentElement.Count, VertexAttribIType.Int, layoutGl41.GetStride(), (void*)offset);

                offset += (uint) currentElement.Count * LayoutElement.GetSizeOfType(currentElement.Type);
            }
            this._backend.CheckError();

            return this;
        }
        /// <summary>
        /// Binds or Selects this current Vertex Array
        /// </summary>
        public VertexArrayObjectGL Bind() {
            if (this.Locked)
                return null;

            this._backend.BindVertexArray(this.ArrayId);
            this._backend.CheckError();

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
        public VertexArrayObjectGL LockingBind() {
            this.Bind();
            this.Lock();

            return this;
        }
        /// <summary>
        /// Locks the Texture so that other Textures cannot be bound/unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal VertexArrayObjectGL Lock() {
            this.Locked = true;

            return this;
        }
        /// <summary>
        /// Unlocks the Texture, so that other Textures can be bound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        public VertexArrayObjectGL Unlock() {
            this.Locked = false;

            return this;
        }
        /// <summary>
        /// Uninds and unlocks the Texture so that other Textures can be bound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal VertexArrayObjectGL UnlockingUnbind() {
            this.Unlock();
            this.Unbind();

            return this;
        }

        /// <summary>
        /// Unbinds all Vertex Arrays
        /// </summary>
        public VertexArrayObjectGL Unbind() {
            
            
            if (this.Locked)
                return null;

            this._backend.BindVertexArray(0);
            this._backend.CheckError();

            CurrentlyBound = null;

            return this;
        }

        private bool _isDisposed = false;

        /// <summary>
        /// Disposes this Vertex Array
        /// </summary>
        public void Dispose() {
            
            
            if (this.Bound)
                this.UnlockingUnbind();

            if(this._isDisposed)
                return;

            this._isDisposed = true;

            try {
                this._backend.DeleteVertexArray(this.ArrayId);
                this._backend.CheckError();
            }
            catch {

            }
        }
    }
}
