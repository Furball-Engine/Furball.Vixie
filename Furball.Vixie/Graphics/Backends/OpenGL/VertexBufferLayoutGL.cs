using System;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Graphics.Backends.OpenGL {
    public struct LayoutElement {
        /// <summary>
        /// Element Count, for example in a 2 component Vector this would be 2
        /// </summary>
        public int                     Count;
        /// <summary>
        /// Type of the Element
        /// </summary>
        public VertexAttribPointerType Type;
        /// <summary>
        /// Does it need to be Normalized? i.e. does it need to be put into 0.0-1.0 space?
        /// </summary>
        public bool                    Normalized;
        /// <summary>
        /// Returns the Size in bytes of `type`
        /// </summary>
        /// <param name="type">Type to get size for</param>
        /// <returns>Size in bytes</returns>
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

    public class VertexBufferLayoutGL {
        /// <summary>
        /// All of the Layout Elements
        /// </summary>
        private List<LayoutElement> _elements;
        /// <summary>
        /// Stride, i.e. how many bytes to go forward to go to the next element
        /// </summary>
        private uint                _stride;

        public VertexBufferLayoutGL() {
            this._elements = new List<LayoutElement>();
        }
        /// <summary>
        /// Adds an Element onto the Layout
        /// </summary>
        /// <param name="count">Count of Elements</param>
        /// <param name="normalized">Do they need to be Normalized?</param>
        /// <typeparam name="pElementType">Type of Element</typeparam>
        /// <returns></returns>
        public unsafe VertexBufferLayoutGL AddElement<pElementType>(int count, bool normalized = false) where pElementType : unmanaged {
            VertexAttribPointerType type = Type.GetTypeCode(typeof(pElementType)) switch {
                TypeCode.Single => VertexAttribPointerType.Float,
                TypeCode.Byte   => VertexAttribPointerType.Byte,
                TypeCode.UInt32 => VertexAttribPointerType.UnsignedInt,
                TypeCode.Int16  => VertexAttribPointerType.Short,
                TypeCode.UInt16 => VertexAttribPointerType.UnsignedShort,
                TypeCode.Int32  => VertexAttribPointerType.Int,
                _ => throw new ArgumentOutOfRangeException("pElementType", "Generic Argument pElementType currently not supported")
            };

            this._elements.Add(new LayoutElement {
                Count      = count,
                Normalized = normalized,
                Type       = type
            });

            this._stride += (uint) (LayoutElement.GetSizeOfType(type) * count);

            return this;
        }
        /// <summary>
        /// Gets the Current Stride
        /// </summary>
        /// <returns>Stride</returns>
        public uint GetStride() => this._stride;
        /// <summary>
        /// Gets all of the LayoutElements
        /// </summary>
        /// <returns>LayoutElements</returns>
        public List<LayoutElement> GetElements() => this._elements;
    }
}
