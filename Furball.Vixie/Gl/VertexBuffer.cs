using System;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Gl=Furball.Vixie.Global;

namespace Furball.Vixie.Gl {
    public class VertexBuffer<pT> : IDisposable where pT : unmanaged {
        /// <summary>
        /// OpenGL's unique ID for this Vertex Buffer
        /// </summary>
        private uint           _bufferId;
        /// <summary>
        /// OpenGL API
        /// </summary>
        private GL             gl;
        /// <summary>
        /// Buffer Usage hint for the OpenGL implementation
        /// </summary>
        private BufferUsageARB _usage;

        /// <summary>
        /// Creates a Block of Memory on the GPU
        /// </summary>
        /// <param name="usage">How is this Buffer going to be used?</param>
        public VertexBuffer(BufferUsageARB usage) {
            this.gl = Global.Gl;

            this._usage = usage;

            gl.GenBuffers(1, out this._bufferId);
        }

        /// <summary>
        /// Binds or Selects this buffer
        /// </summary>
        public void Bind() {
            gl.BindBuffer(GLEnum.ArrayBuffer, this._bufferId);
        }

        /// <summary>
        /// Sets the Data of the Buffer
        /// <remarks>Buffer has to be bound for SetData to function</remarks>
        /// </summary>
        /// <param name="data">Data to Upload to the Buffer</param>
        public unsafe void SetData(Span<pT> data) {
            fixed (void* d = data) {
                gl.BufferData(GLEnum.ArrayBuffer, (nuint) (data.Length * sizeof(pT)), d, this._usage);
            }
        }

        public void Dispose() {
            gl.DeleteBuffer(this._bufferId);
        }
    }
}
