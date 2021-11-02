using System;
using System.Collections.Generic;
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
    public unsafe struct BatchedVertex {
        public fixed float Positions[2];
        public fixed float TexCoords[2];
        public float   TexId;
    }

    public class BatchedRenderer {
        /// <summary>
        /// How many Quads are allowed to be drawn in 1 draw
        /// </summary>
        public const int MAX_QUADS     = 8192;
        /// <summary>
        /// How many Verticies are gonna be stored inside the Vertex Buffer
        /// </summary>
        public const int MAX_VERTICIES = (MAX_QUADS * 20) * 4;
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
        private readonly VertexArrayObject _vertexArray;
        /// <summary>
        /// Vertex Buffer which holds all the Verticies
        /// </summary>
        private readonly BufferObject _vertexBuffer;
        /// <summary>
        /// Index Buffer which holds all the indicies
        /// </summary>
        private readonly BufferObject _indexBuffer;
        /// <summary>
        /// Shader used to draw everything
        /// </summary>
        private readonly Shader _batchShader;
        /// <summary>
        /// Local Vertex Buffer
        /// </summary>
        private readonly BatchedVertex[] _localVertexBuffer;
        /// <summary>
        /// Cache for Texture ID lookups
        /// </summary>
        private readonly Dictionary<uint, float> _glTexIdToTexIdLookup;
        /// <summary>
        /// Cache for OpenGL Texture ID Lookups
        /// </summary>
        private readonly Dictionary<float, uint> _texIdToGlTexIdLookup;

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

            //Prepare a Local Vertex Buffer, this is what will be uploaded to the GPU each frame
            this._localVertexBuffer = new BatchedVertex[MAX_VERTICIES];

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

            this._glTexIdToTexIdLookup = new Dictionary<uint, float>(MAX_TEX_SLOTS);
            this._texIdToGlTexIdLookup = new Dictionary<float, uint>(MAX_TEX_SLOTS);
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
        /// Index into the Local Vertex Buffer (technically doesnt exist but its here for size calc)
        /// </summary>
        private int _vertexBufferIndex = 0;
        /// <summary>
        /// Current Pointer into the Local Vertex Buffer
        /// </summary>
        private unsafe BatchedVertex* _vertexPointer;

        public unsafe void Begin(bool clear = true) {
            this._glTexIdToTexIdLookup.Clear();
            this._texIdToGlTexIdLookup.Clear();

            fixed (BatchedVertex* data = this._localVertexBuffer)
                this._vertexPointer = data;

            if (clear) {
                Global.Gl.Clear(ClearBufferMask.ColorBufferBit);
                //Clear stats
                this.DrawCalls  = 0;
                this.QuadsDrawn = 0;
            }
        }

        private float _textureIndex;
        private float _posX;
        private float _posy;
        private float _sizeX;
        private float _sizeY;

        public unsafe void Draw(Texture texture, Vector2 position, Vector2 size) {
            //If we ran out of Texture Slots or are out of space in out Vertex/Index buffer, flush whats already there and start a new Batch
            if (this._indexCount >= MAX_INDICIES || this._textureSlotIndex >= MAX_TEX_SLOTS - 1) {
                this.End();
                this.Begin(false);
            }

            this._posX  = position.X;
            this._posy  = position.Y;
            this._sizeX = size.X;
            this._sizeY = size.Y;

            if (this._glTexIdToTexIdLookup.TryGetValue(texture._textureId, out this._textureIndex)) {
                this._glTexIdToTexIdLookup.Add(texture._textureId, this._textureSlotIndex);
                this._texIdToGlTexIdLookup.Add(this._textureSlotIndex, texture._textureId);

                this._textureSlotIndex++;
            }

            //Vertex 1
            this._vertexPointer->Positions[0] = this._posX;
            this._vertexPointer->Positions[1] = this._posy + this._sizeY;
            this._vertexPointer->TexCoords[0] = 0f;
            this._vertexPointer->TexCoords[1] = 0f;
            this._vertexPointer->TexId        = this._textureIndex;
            this._vertexPointer++;

            //Vertex 2
            this._vertexPointer->Positions[0] = this._posX + this._sizeX;
            this._vertexPointer->Positions[1] = this._posy + this._sizeY;
            this._vertexPointer->TexCoords[0] = 1f;
            this._vertexPointer->TexCoords[1] = 0f;
            this._vertexPointer->TexId        = this._textureIndex;
            this._vertexPointer++;

            //Vertex 3
            this._vertexPointer->Positions[0] = this._posX + this._sizeX;
            this._vertexPointer->Positions[1] = this._posy;
            this._vertexPointer->TexCoords[0] = 1f;
            this._vertexPointer->TexCoords[1] = 1f;
            this._vertexPointer->TexId        = this._textureIndex;
            this._vertexPointer++;

            //Vertex 4
            this._vertexPointer->Positions[0] = this._posX;
            this._vertexPointer->Positions[1] = this._posy;
            this._vertexPointer->TexCoords[0] = 0f;
            this._vertexPointer->TexCoords[1] = 1f;
            this._vertexPointer->TexId        = this._textureIndex;
            this._vertexPointer++;

            this._indexCount        += 6;
            this._vertexBufferIndex += 80;
            this.QuadsDrawn++;
        }

        public unsafe void End() {
            //Bind all textures
            for (uint i = 0; i != this._textureSlotIndex; i++) {
                Global.Gl.BindTextureUnit(i, this._texIdToGlTexIdLookup[i]);
            }

            //Calculate how many verticies have to be uploaded to the GPU
            nuint size = (nuint) (this._vertexBufferIndex);

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
            this._textureSlotIndex  = 0;
            this._vertexBufferIndex = 0;

            this.DrawCalls++;
        }
    }
}