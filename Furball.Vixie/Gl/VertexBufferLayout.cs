using System;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Gl {
    public struct LayoutElement {
        public int                     Count;
        public VertexAttribPointerType Type;
        public bool                    Normalized;

        public static uint GetSizeOfType(VertexAttribPointerType type) {
            switch (type) {
                case VertexAttribPointerType.Float:
                    return sizeof(float);
                case VertexAttribPointerType.Byte:
                    return sizeof(byte);
                case VertexAttribPointerType.UnsignedInt:
                    return sizeof(uint);
                case VertexAttribPointerType.Short:
                    return sizeof(short);
                case VertexAttribPointerType.UnsignedShort:
                    return sizeof(ushort);
                case VertexAttribPointerType.Int:
                    return sizeof(int);
            }

            return 0;
        }
    }

    public class VertexBufferLayout {
        private List<LayoutElement> _elements;
        private uint                _stride;

        public VertexBufferLayout() {
            this._elements = new List<LayoutElement>();
        }

        public unsafe VertexBufferLayout AddElement<pElementType>(int count, bool normalized = false) where pElementType : unmanaged {
            VertexAttribPointerType type = Type.GetTypeCode(typeof(pElementType)) switch {
                TypeCode.Single => VertexAttribPointerType.Float,
                TypeCode.Byte   => VertexAttribPointerType.Byte,
                TypeCode.UInt32 => VertexAttribPointerType.UnsignedInt,
                TypeCode.Int16  => VertexAttribPointerType.Short,
                TypeCode.UInt16 => VertexAttribPointerType.UnsignedShort,
                TypeCode.Int32  => VertexAttribPointerType.Int
            };

            this._elements.Add(new LayoutElement {
                Count      = count,
                Normalized = normalized,
                Type       = type
            });

            this._stride += (uint) (LayoutElement.GetSizeOfType(type) * count);

            return this;
        }

        public uint GetStride() => this._stride;

        public List<LayoutElement> GetElements() => this._elements;
    }
}
