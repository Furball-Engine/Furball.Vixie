using System;
using Silk.NET.OpenGLES;

namespace Furball.Vixie.Graphics {
    public class UniformBuffer {
        private readonly uint   _bufferId;
        private readonly Shader _shader;

        private readonly GL gl;

        public UniformBuffer(nuint bufferSize, Shader shader) {
            this.gl      = Global.Gl;
            this._shader = shader;

            this._bufferId = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.UniformBuffer, this._bufferId);
            gl.BufferData(BufferTargetARB.UniformBuffer, bufferSize, 0, BufferUsageARB.DynamicDraw);
        }

        public UniformBuffer SetBlockBinding(uint blockIndex, uint binding) {
            gl.UniformBlockBinding(this._shader.ProgramId, blockIndex, binding);

            return this;
        }

        public UniformBuffer Bind() {
            gl.BindBuffer(BufferTargetARB.UniformBuffer, this._bufferId);

            return this;
        }

        /// <summary>
        /// Puts data into the Buffer
        /// </summary>
        /// <param name="data">Data to put there</param>
        /// <param name="size">Size of the Data</param>
        /// <returns></returns>
        public unsafe UniformBuffer SetData(void* data, nuint size) {
            this.gl.BufferData(BufferTargetARB.UniformBuffer, size, data, BufferUsageARB.DynamicDraw);

            return this;
        }

        public unsafe UniformBuffer SetSubData(void* data, nuint size, nint offset = 0) {
            this.gl.BufferSubData(BufferTargetARB.UniformBuffer, offset, size, data);
            return this;
        }

        public unsafe UniformBuffer SetSubData<pDataType>(Span<pDataType> data) where pDataType : unmanaged {
            fixed (void* d = data) {
                this.SetSubData(d, (nuint)(data.Length * sizeof(pDataType)));
            }

            return this;
        }
    }
}
