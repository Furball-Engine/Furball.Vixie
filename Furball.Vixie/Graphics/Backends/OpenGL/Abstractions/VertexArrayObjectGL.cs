using System;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGLES;

namespace Furball.Vixie.Graphics.Backends.OpenGL.Abstractions {
    public class VertexArrayObjectGL : IDisposable {
        private readonly OpenGLESBackend _backend;
        /// <summary>
        /// Current Bound VAO
        /// </summary>
        internal static VertexArrayObjectGL CurrentlyBound;
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

        public VertexArrayObjectGL(OpenGLESBackend backend) {
            this._backend = backend;
            _backend.CheckThread();

            this.gl = backend.GetGlApi();
            //Generate Vertex Array
            this.ArrayId = this.gl.GenVertexArray();
            _backend.CheckError();
        }

        /// <summary>
        /// Adds a VertexBuffer with a certain Layout to this Vertex Array
        /// </summary>
        /// <param name="vertexBuffer">Vertex Buffer to add</param>
        /// <param name="layoutGl">Layout of said Vertex Buffer</param>
        public unsafe VertexArrayObjectGL AddBuffer(BufferObjectGL vertexBuffer, VertexBufferLayoutGL layoutGl) {
            _backend.CheckThread();
            
            //Bind both this and the Vertex Buffer
            this.Bind();
            vertexBuffer.Bind();
            //Get all the elements
            var elements = layoutGl.GetElements();

            uint offset = 0;
            //Loop over the elements
            for (uint i = 0; i != elements.Count; i++) {
                LayoutElement currentElement = elements[(int) i];
                //Define the Layout of this Element
                this.gl.EnableVertexAttribArray(i);

                if (currentElement.Type != VertexAttribPointerType.Int)
                    this.gl.VertexAttribPointer(i, currentElement.Count, currentElement.Type, currentElement.Normalized, layoutGl.GetStride(), (void*)offset);
                else
                    this.gl.VertexAttribIPointer(i, currentElement.Count, VertexAttribIType.Int, layoutGl.GetStride(), (void*)offset);

                offset += (uint) currentElement.Count * LayoutElement.GetSizeOfType(currentElement.Type);
            }
            _backend.CheckError();

            return this;
        }
        /// <summary>
        /// Binds or Selects this current Vertex Array
        /// </summary>
        public VertexArrayObjectGL Bind() {
            _backend.CheckThread();
            
            if (this.Locked)
                return null;

            this.gl.BindVertexArray(this.ArrayId);
            _backend.CheckError();

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
        internal VertexArrayObjectGL LockingBind() {
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
        internal VertexArrayObjectGL Unlock() {
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
            _backend.CheckThread();
            
            if (this.Locked)
                return null;

            this.gl.BindVertexArray(0);
            _backend.CheckError();

            CurrentlyBound = null;

            return this;
        }
        /// <summary>
        /// Disposes this Vertex Array
        /// </summary>
        public void Dispose() {
            _backend.CheckThread();
            
            if (this.Bound)
                this.UnlockingUnbind();

            try {
                this.gl.DeleteVertexArray(this.ArrayId);
                _backend.CheckError();
            }
            catch {

            }
        }
    }
}
