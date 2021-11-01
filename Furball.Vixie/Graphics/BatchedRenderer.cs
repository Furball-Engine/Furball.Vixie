using Furball.Vixie.Gl;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Graphics {
    public class BatchedRenderer {
        /// <summary>
        /// How many Quads are allowed to be drawn in 1 draw
        /// </summary>
        public const int MAX_QUADS     = 2048;
        /// <summary>
        /// How many Verticies are gonna be stored inside the Vertex Buffer
        /// </summary>
        public const int MAX_VERTICIES = MAX_QUADS * 4;
        /// <summary>
        /// How many Indicies are gonna be stored inside the Index Buffer
        /// </summary>
        public const int MAX_INDICIES  = MAX_QUADS * 6;
        /// <summary>
        /// Max amount of Texture Slots, 16 to support a bit older GPUs
        /// </summary>
        public const int MAX_TEX_SLOTS = 16;

        /// <summary>
        /// Vertex Array that holds the Index and Vertex Buffers
        /// </summary>
        private VertexArrayObject _vertexArray;
        /// <summary>
        /// Vertex Buffer which holds all the Verticies
        /// </summary>
        private BufferObject      _vertexBuffer;
        /// <summary>
        /// Index Buffer which holds all the indicies
        /// </summary>
        private BufferObject      _indexBuffer;
        /// <summary>
        /// All Available Texture Slots
        /// </summary>
        private uint[] _textureSlots;

        public unsafe BatchedRenderer() {
            int vertexSize = (4 * sizeof(float)) + 1 * sizeof(int);

            this._vertexBuffer = new BufferObject(vertexSize * MAX_VERTICIES, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);

            VertexBufferLayout layout = new VertexBufferLayout();

            layout
                .AddElement<float>(2) //Position
                .AddElement<float>(2) //Tex Coord
                .AddElement<int>(1); //Tex Id

            uint[] indicies = new uint[MAX_INDICIES];
            uint offset = 0;

            for (int i = 0; i < MAX_INDICIES; i += 6) {
                indicies[i + 0] = 0 + offset;
                indicies[i + 1] = 1 + offset;
                indicies[i + 2] = 2 + offset;
                indicies[i + 3] = 2 + offset;
                indicies[i + 4] = 3 + offset;
                indicies[i + 5] = 0 + offset;

                offset += 4;
            }

            this._indexBuffer = new BufferObject(BufferTargetARB.ElementArrayBuffer);

            fixed (void* data = indicies) {
                this._indexBuffer.SetData(data, MAX_INDICIES * sizeof(uint));
            }

            this._textureSlots = new uint[MAX_TEX_SLOTS];

            for (uint i = 0; i != MAX_TEX_SLOTS; i++) {
                this._textureSlots[i] = 0;
            }


        }
    }
}
