using System;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGLES;

namespace Furball.Vixie.Graphics {
    public class VertexArrayObject : IDisposable {
        /// <summary>
        /// Current Bound VAO
        /// </summary>
        internal static VertexArrayObject CurrentlyBound;
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

        public VertexArrayObject() {
            OpenGLHelper.CheckThread();
            
            this.gl = Global.Gl;
            //Generate Vertex Array
            this.ArrayId = this.gl.GenVertexArray();
            OpenGLHelper.CheckError();
        }

        /// <summary>
        /// Adds a VertexBuffer with a certain Layout to this Vertex Array
        /// </summary>
        /// <param name="vertexBuffer">Vertex Buffer to add</param>
        /// <param name="layout">Layout of said Vertex Buffer</param>
        public unsafe VertexArrayObject AddBuffer(BufferObject vertexBuffer, VertexBufferLayout layout) {
            OpenGLHelper.CheckThread();
            
            //Bind both this and the Vertex Buffer
            this.Bind();
            vertexBuffer.Bind();
            //Get all the elements
            var elements = layout.GetElements();

            uint offset = 0;
            //Loop over the elements
            for (uint i = 0; i != elements.Count; i++) {
                LayoutElement currentElement = elements[(int) i];
                //Define the Layout of this Element
                this.gl.EnableVertexAttribArray(i);
                this.gl.VertexAttribPointer(i, currentElement.Count, currentElement.Type, currentElement.Normalized, layout.GetStride(), (void*) offset);

                offset += (uint) currentElement.Count * LayoutElement.GetSizeOfType(currentElement.Type);
            }
            OpenGLHelper.CheckError();

            return this;
        }
        /// <summary>
        /// Binds or Selects this current Vertex Array
        /// </summary>
        public VertexArrayObject Bind() {
            OpenGLHelper.CheckThread();
            
            if (this.Locked)
                return null;

            this.gl.BindVertexArray(this.ArrayId);
            OpenGLHelper.CheckError();

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
        internal VertexArrayObject LockingBind() {
            this.Bind();
            this.Lock();

            return this;
        }
        /// <summary>
        /// Locks the Texture so that other Textures cannot be bound/unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal VertexArrayObject Lock() {
            this.Locked = true;

            return this;
        }
        /// <summary>
        /// Unlocks the Texture, so that other Textures can be bound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal VertexArrayObject Unlock() {
            this.Locked = false;

            return this;
        }
        /// <summary>
        /// Uninds and unlocks the Texture so that other Textures can be bound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal VertexArrayObject UnlockingUnbind() {
            this.Unlock();
            this.Unbind();

            return this;
        }

        /// <summary>
        /// Unbinds all Vertex Arrays
        /// </summary>
        public VertexArrayObject Unbind() {
            OpenGLHelper.CheckThread();
            
            if (this.Locked)
                return null;

            this.gl.BindVertexArray(0);
            OpenGLHelper.CheckError();

            CurrentlyBound = null;

            return this;
        }
        /// <summary>
        /// Disposes this Vertex Array
        /// </summary>
        public void Dispose() {
            OpenGLHelper.CheckThread();
            
            if (this.Bound)
                this.UnlockingUnbind();

            try {
                this.gl.DeleteVertexArray(this.ArrayId);
                OpenGLHelper.CheckError();
            }
            catch {

            }
        }
    }
}
