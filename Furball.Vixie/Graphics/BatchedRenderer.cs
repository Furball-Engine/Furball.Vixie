using System.Numerics;
using Furball.Vixie.Gl;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;
using Shader=Furball.Vixie.Gl.Shader;
using Texture=Furball.Vixie.Gl.Texture;

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
        /// Shader used to draw everything
        /// </summary>
        private Shader _batchShader;
        /// <summary>
        /// All Available Texture Slots
        /// </summary>
        private uint[] _textureSlots;
        /// <summary>
        /// Local Vertex Buffer
        /// </summary>
        private float[] _localVertexBuffer;

        public unsafe BatchedRenderer() {
            int vertexSize = (4 * sizeof(float)) + 1 * sizeof(int);

            this._vertexBuffer = new BufferObject(vertexSize * MAX_VERTICIES, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);

            VertexBufferLayout layout = new VertexBufferLayout();

            layout
                .AddElement<float>(2) //Position
                .AddElement<float>(2) //Tex Coord
                .AddElement<float>(1); //Tex Id

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

            this._localVertexBuffer = new float[MAX_VERTICIES];

            string vertSource = ResourceHelpers.GetStringResource("ShaderCode/BatchRendererVertexShader.glsl", true);
            string fragSource = ResourceHelpers.GetStringResource("ShaderCode/BatchRendererPixelShader.glsl", true);

            this._batchShader =
                new Shader()
                    .AttachShader(ShaderType.VertexShader, vertSource)
                    .AttachShader(ShaderType.FragmentShader, fragSource)
                    .Link();

            this._vertexArray = new VertexArrayObject();
            this._vertexArray.AddBuffer(this._vertexBuffer, layout);
        }

        private int _indexCount        = 0;
        private int _textureSlotIndex  = 0;
        private int _vertexBufferIndex = 0;

        public void Begin() {
            this._indexCount        = 0;
            this._textureSlotIndex  = 0;
            this._vertexBufferIndex = 0;

            Global.Gl.Clear(ClearBufferMask.ColorBufferBit);
        }

        public void Draw(Texture texture, Vector2 position, Vector2 size) {
            if (this._indexCount >= MAX_INDICIES || this._textureSlotIndex >= MAX_TEX_SLOTS - 1) {
                this.End();
                this.Begin();
            }

            float textureIndex = -1f;

            for (int i = 0; i != this._textureSlotIndex; i++) {
                uint texId = texture.GetTextureId();

                if (this._textureSlots[i] == texId) {
                    textureIndex = i;
                    break;
                }
            }

            if (textureIndex == -1f) {
                textureIndex = this._textureSlotIndex;

                this._textureSlotIndex++;
            }

            this._localVertexBuffer[this._vertexBufferIndex++] = position.X;
            this._localVertexBuffer[this._vertexBufferIndex++] = position.Y;
            this._localVertexBuffer[this._vertexBufferIndex++] = 0f;
            this._localVertexBuffer[this._vertexBufferIndex++] = 0f;

            this._localVertexBuffer[this._vertexBufferIndex++] = position.X + size.X;
            this._localVertexBuffer[this._vertexBufferIndex++] = position.Y;
            this._localVertexBuffer[this._vertexBufferIndex++] = 1f;
            this._localVertexBuffer[this._vertexBufferIndex++] = 0f;

            this._localVertexBuffer[this._vertexBufferIndex++] = position.X + size.X;
            this._localVertexBuffer[this._vertexBufferIndex++] = position.Y + size.Y;
            this._localVertexBuffer[this._vertexBufferIndex++] = 1f;
            this._localVertexBuffer[this._vertexBufferIndex++] = 1f;

            this._localVertexBuffer[this._vertexBufferIndex++] = position.X;
            this._localVertexBuffer[this._vertexBufferIndex++] = position.Y + size.Y;
            this._localVertexBuffer[this._vertexBufferIndex++] = 0f;
            this._localVertexBuffer[this._vertexBufferIndex++] = 1f;

            this._indexCount += 6;
        }

        public unsafe void End() {
            nuint size = (nuint) (this._localVertexBuffer.Length - this._vertexBufferIndex);

            fixed (void* data = this._localVertexBuffer) {

                this._vertexBuffer
                    .Bind()
                    .SetSubData(data, size);
            }

            this._vertexArray.Bind();
            this._vertexBuffer.Bind();
            this._indexBuffer.Bind();
            this._batchShader.Bind();

            Global.Gl.DrawElements(PrimitiveType.Triangles, (uint) this._indexCount, DrawElementsType.UnsignedInt, null);
        }
    }
}
