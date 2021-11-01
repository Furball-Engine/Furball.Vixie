using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Gl;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;
using Shader=Furball.Vixie.Gl.Shader;
using Texture=Furball.Vixie.Gl.Texture;
using UniformType=Furball.Vixie.Gl.UniformType;

namespace Furball.Vixie.Graphics {
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchedVertex {
        public Vector2 Position;
        public Vector2 TexCoord;
        public float   TexIndex;
    }

    public class BatchedRenderer {
        /// <summary>
        /// How many Quads are allowed to be drawn in 1 draw
        /// </summary>
        public const int MAX_QUADS     = 1024;
        /// <summary>
        /// How many Verticies are gonna be stored inside the Vertex Buffer
        /// </summary>
        public const int MAX_VERTICIES = MAX_QUADS * 20;
        /// <summary>
        /// How many Indicies are gonna be stored inside the Index Buffer
        /// </summary>
        public const int MAX_INDICIES  = MAX_QUADS * 6;
        /// <summary>
        /// Max amount of Texture Slots, 16 to support a bit older GPUs
        /// </summary>
        public const int MAX_TEX_SLOTS = 32;

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

        /// <summary>
        /// Purely for Statistics, stores how many Quads have been drawn
        /// </summary>
        public int QuadsDrawn = 0;
        /// <summary>
        /// Purely for Statistics, stores how many times DrawElements has been called
        /// </summary>
        public int DrawCalls  = 0;

        public unsafe BatchedRenderer() {
            //Size of 1 Vertex
            int vertexSize = (4 * sizeof(float)) + 1 * sizeof(int);
            //Create the Vertex Buffer
            this._vertexBuffer = new BufferObject(vertexSize * MAX_VERTICIES, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);

            //Define the Layout of the Vertex Buffer
            VertexBufferLayout layout = new VertexBufferLayout();

            layout
                .AddElement<float>(2) //Position
                .AddElement<float>(2) //Tex Coord
                .AddElement<float>(1); //Tex Id

            uint[] indicies = new uint[MAX_INDICIES];
            uint offset = 0;

            //Generate the Indicies
            for (int i = 0; i < MAX_INDICIES; i += 6) {
                indicies[i + 0] = 0 + offset;
                indicies[i + 1] = 1 + offset;
                indicies[i + 2] = 2 + offset;
                indicies[i + 3] = 2 + offset;
                indicies[i + 4] = 3 + offset;
                indicies[i + 5] = 0 + offset;

                offset += 4;
            }

            //Create the Index Buffer
            this._indexBuffer = new BufferObject(BufferTargetARB.ElementArrayBuffer);

            //Put the Indicies into the Index Buffer
            fixed (void* data = indicies) {
                this._indexBuffer.Bind();
                this._indexBuffer.SetData(data, MAX_INDICIES * sizeof(uint));
            }

            this._textureSlots = new uint[MAX_TEX_SLOTS];

            for (uint i = 0; i != MAX_TEX_SLOTS; i++) {
                this._textureSlots[i] = 0;
            }

            //Prepare a Local Vertex Buffer, this is what will be uploaded to the GPU each frame
            this._localVertexBuffer = new float[MAX_VERTICIES];

            //Prepare Shader Sources
            string vertSource = ResourceHelpers.GetStringResource("ShaderCode/BatchRenderer/BatchRendererVertexShader.glsl", true);
            string fragSource = ResourceHelpers.GetStringResource("ShaderCode/BatchRenderer/BatchRendererPixelShader.glsl", true);

            //Create BatchShader
            this._batchShader =
                new Shader()
                    .AttachShader(ShaderType.VertexShader, vertSource)
                    .AttachShader(ShaderType.FragmentShader, fragSource)
                    .Link();

            //Create VAO and put the layout defined earlier in it
            this._vertexArray = new VertexArrayObject();
            this._vertexArray.AddBuffer(this._vertexBuffer, layout);
        }

        /// <summary>
        /// How many Indicies have been processed
        /// </summary>
        private int _indexCount        = 0;
        /// <summary>
        /// Current Texture Slot
        /// </summary>
        private int _textureSlotIndex  = 0;
        /// <summary>
        /// Index into the Local Vertex Buffer
        /// </summary>
        private int _vertexBufferIndex = 0;

        public void Begin(bool clear = true) {
            if (clear) {
                Global.Gl.Clear(ClearBufferMask.ColorBufferBit);
                this.DrawCalls  = 0;
                this.QuadsDrawn = 0;
            }
        }

        public void Draw(Texture texture, Vector2 position, Vector2 size) {
            //If we ran out of Texture Slots or are out of space in out Vertex/Index buffer, flush whats already there and start a new Batch
            if (this._indexCount >= MAX_INDICIES || this._textureSlotIndex >= MAX_TEX_SLOTS - 1) {
                this.End();
                this.Begin(false);
            }

            float textureIndex = -1f;

            //See if the Texture has already been used
            for (int i = 0; i != this._textureSlotIndex; i++) {
                uint texId = texture.GetTextureId();

                if (this._textureSlots[i] == texId) {
                    textureIndex = i;
                    break;
                }
            }

            //If no, reserve a new slot for the Texture
            if (textureIndex == -1f) {
                textureIndex                               = this._textureSlotIndex;
                this._textureSlots[this._textureSlotIndex] = texture.GetTextureId();

                this._textureSlotIndex++;
            }

            //Theres most likely a way better way to deal with this but i dont quite know it yet

            //Vertex 1
            this._localVertexBuffer[this._vertexBufferIndex++] = position.X;
            this._localVertexBuffer[this._vertexBufferIndex++] = position.Y + size.Y;
            this._localVertexBuffer[this._vertexBufferIndex++] = 0f;
            this._localVertexBuffer[this._vertexBufferIndex++] = 0f;
            this._localVertexBuffer[this._vertexBufferIndex++] = textureIndex;

            //Vertex 2
            this._localVertexBuffer[this._vertexBufferIndex++] = position.X + size.X;
            this._localVertexBuffer[this._vertexBufferIndex++] = position.Y + size.Y;
            this._localVertexBuffer[this._vertexBufferIndex++] = 1f;
            this._localVertexBuffer[this._vertexBufferIndex++] = 0f;
            this._localVertexBuffer[this._vertexBufferIndex++] = textureIndex;

            //Vertex 3
            this._localVertexBuffer[this._vertexBufferIndex++] = position.X + size.X;
            this._localVertexBuffer[this._vertexBufferIndex++] = position.Y;
            this._localVertexBuffer[this._vertexBufferIndex++] = 1f;
            this._localVertexBuffer[this._vertexBufferIndex++] = 1f;
            this._localVertexBuffer[this._vertexBufferIndex++] = textureIndex;

            //Vertex 4
            this._localVertexBuffer[this._vertexBufferIndex++] = position.X;
            this._localVertexBuffer[this._vertexBufferIndex++] = position.Y;
            this._localVertexBuffer[this._vertexBufferIndex++] = 0f;
            this._localVertexBuffer[this._vertexBufferIndex++] = 1f;
            this._localVertexBuffer[this._vertexBufferIndex++] = textureIndex;

            this._indexCount += 6;
            this.QuadsDrawn++;
        }

        public unsafe void End() {
            //Bind all textures
            for(uint i = 0; i != this._textureSlotIndex; i++)
                Global.Gl.BindTextureUnit(i, this._textureSlots[i]);

            //Calculate how many verticies have to be uploaded to the GPU
            nuint size = (nuint) (this._localVertexBuffer.Length - this._vertexBufferIndex);

            fixed (void* data = this._localVertexBuffer) {
                this._vertexBuffer
                    .Bind()
                    .SetSubData(data, size);
            }

            //Bind everything
            this._vertexArray.Bind();
            this._indexBuffer.Bind();
            this._vertexBuffer.Bind();

            //Bind the Shader and provide the Window projection matrix, to give us normal pixel space from 0,0 to whatever the window size is in the bottom right
            this._batchShader
                .Bind()
                .SetUniform("vx_WindowProjectionMatrix", UniformType.GlMat4f, Global.GameInstance.WindowManager.ProjectionMatrix);

            //Draw
            Global.Gl.DrawElements(PrimitiveType.Triangles, (uint) this._indexCount, DrawElementsType.UnsignedInt, null);

            //Reset counts
            this._indexCount        = 0;
            this._textureSlotIndex  = 1;
            this._vertexBufferIndex = 0;

            this.DrawCalls++;
        }
    }
}