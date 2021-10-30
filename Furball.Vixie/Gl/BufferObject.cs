using System;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Gl {
    public class BufferObject<pDataType> : IDisposable where pDataType : unmanaged {
        private uint            _bufferId;
        private BufferTargetARB _bufferType;

        private GL gl;

        public unsafe BufferObject(Span<pDataType> data, BufferTargetARB bufferType) {
            gl               = Global.Gl;
            this._bufferType = bufferType;

            this._bufferId = gl.GenBuffer();
            gl.BindBuffer(this._bufferType, this._bufferId);

            fixed (void* d = data) {
                gl.BufferData(this._bufferType, (nuint) (data.Length * sizeof(pDataType)), d, BufferUsageARB.StaticDraw);
            }
        }

        public BufferObject<pDataType> Bind() {
            gl.BindBuffer(this._bufferType, this._bufferId);

            return this;
        }

        public void Dispose() {
            gl.DeleteBuffer(this._bufferId);
        }
    }
}
