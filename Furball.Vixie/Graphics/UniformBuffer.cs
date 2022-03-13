using System;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGLES;

namespace Furball.Vixie.Graphics {
    public class UniformBuffer {
        private readonly uint   _bufferId;
        private readonly Shader _shader;

        private readonly GL gl;

        public UniformBuffer(nuint bufferSize, Shader shader) {
            OpenGLHelper.CheckThread();
            
            this.gl      = Global.Gl;
            this._shader = shader;

            this._bufferId = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.UniformBuffer, this._bufferId);
            gl.BufferData(BufferTargetARB.UniformBuffer, bufferSize, 0, BufferUsageARB.DynamicDraw);
            OpenGLHelper.CheckError();
        }

        public UniformBuffer SetBlockBinding(uint blockIndex, uint binding) {
            OpenGLHelper.CheckThread();
            
            gl.UniformBlockBinding(this._shader.ProgramId, blockIndex, binding);
            OpenGLHelper.CheckError();

            return this;
        }

        public UniformBuffer Bind() {
            OpenGLHelper.CheckThread();
            
            gl.BindBuffer(BufferTargetARB.UniformBuffer, this._bufferId);
            OpenGLHelper.CheckError();

            return this;
        }

        /// <summary>
        /// Puts data into the Buffer
        /// </summary>
        /// <param name="data">Data to put there</param>
        /// <param name="size">Size of the Data</param>
        /// <returns></returns>
        public unsafe UniformBuffer SetData(void* data, nuint size) {
            OpenGLHelper.CheckThread();
            
            this.gl.BufferData(BufferTargetARB.UniformBuffer, size, data, BufferUsageARB.DynamicDraw);
            OpenGLHelper.CheckError();

            return this;
        }

        public unsafe UniformBuffer SetSubData(void* data, nuint size, nint offset = 0) {
            OpenGLHelper.CheckThread();
            
            this.gl.BufferSubData(BufferTargetARB.UniformBuffer, offset, size, data);
            OpenGLHelper.CheckError();

            return this;
        }

        public unsafe UniformBuffer SetSubData<pDataType>(Span<pDataType> data) where pDataType : unmanaged {
            fixed (void* d = data) {
                this.SetSubData(d, (nuint)(data.Length * sizeof(pDataType)));
            }
            OpenGLHelper.CheckError();

            return this;
        }
    }
}
