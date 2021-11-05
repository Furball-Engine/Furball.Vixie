using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Graphics.Renderers {
    //Makes sure everything is layed out one after the other in memory,
    //Important because of how we're uploading data to the vertex buffer,
    //If this wasnt there there is a chance they wouldnt lie next to each other in memory
    //making the void* we take in End() be completly garbled and we'd be sending invalid data to the GPU
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BatchedVertex {
        /// <summary>
        /// Position of the Vertex
        /// </summary>
        public fixed float Positions[2];
        /// <summary>
        /// Texture Coordinate of the Vertex
        /// </summary>
        public fixed float TexCoords[2];
        /// <summary>
        /// Texture ID of the Vertex
        /// </summary>
        public float   TexId;
        /// <summary>
        /// Color Override
        /// </summary>
        public fixed float Color[4];
    }

    public class BatchedRenderer : IDisposable, ITextureRenderer {
        /// <summary>
        /// How many Quads are allowed to be drawn in 1 draw
        /// </summary>
        public int MaxQuads { get; private set; }
        /// <summary>
        /// How many Verticies are gonna be stored inside the Vertex Buffer
        /// </summary>
        public int MaxVerticies { get; private set; }
        /// <summary>
        /// How many Indicies are gonna be stored inside the Index Buffer
        /// </summary>
        public uint MaxIndicies { get; private set; }
        /// <summary>
        /// Max amount of Texture Slots
        /// </summary>
        public int MaxTexSlots { get; private set; }

        /// <summary>
        /// OpenGL API, used to shorten code
        /// </summary>
        private readonly GL gl;
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

        public unsafe BatchedRenderer(int capacity = 4096) {
            this.gl = Global.Gl;
            //Initializes MaxQuad/Vertex/Index counts and figures out how many texture slots to use max
            this.InitializeConstants(capacity);

            //Size of 1 Vertex
            int vertexSize = (4 * sizeof(float)) + 1 * sizeof(int);
            //Create the Vertex Buffer
            this._vertexBuffer = new BufferObject(vertexSize * this.MaxVerticies, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);

            //Define the Layout of the Vertex Buffer
            VertexBufferLayout layout = new VertexBufferLayout();

            layout
                .AddElement<float>(2)  //Position
                .AddElement<float>(2)  //Tex Coord
                .AddElement<float>(1)  //Tex Id
                .AddElement<float>(4); //Color

            uint[] indicies = new uint[this.MaxIndicies];
            uint offset = 0;

            //Generate the Indicies
            for (int i = 0; i < this.MaxIndicies; i += 6) {
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
                this._indexBuffer.SetData(data, this.MaxIndicies * sizeof(uint));
            }

            //Prepare a Local Vertex Buffer, this is what will be uploaded to the GPU each frame
            this._localVertexBuffer = new BatchedVertex[this.MaxVerticies];

            //Prepare Shader Sources
            string vertSource = ResourceHelpers.GetStringResource("ShaderCode/BatchRenderer/VertexShader.glsl", true);
            string fragSource = ResourceHelpers.GetStringResource("ShaderCode/BatchRenderer/PixelShader.glsl", true);

            //Create BatchShader
            this._batchShader =
                new Shader()
                    .AttachShader(ShaderType.VertexShader, vertSource)
                    .AttachShader(ShaderType.FragmentShader, fragSource)
                    .Link();

            //Create VAO and put the layout defined earlier in it
            this._vertexArray = new VertexArrayObject();
            this._vertexArray.AddBuffer(this._vertexBuffer, layout);

            this._glTexIdToTexIdLookup = new Dictionary<uint, float>(this.MaxTexSlots);
            this._texIdToGlTexIdLookup = new Dictionary<float, uint>(this.MaxTexSlots);
        }

        ~BatchedRenderer() {
            this.Dispose();
        }

        private void InitializeConstants(int quads) {
            this.MaxQuads     = quads;
            this.MaxVerticies = this.MaxQuads * 20 * 4;
            this.MaxIndicies  = (uint) this.MaxQuads * 6;
            this.MaxTexSlots  = Math.Min(31, Global.Device.MaxTextureImageUnits); //Adjusts based on how many Texture the GPU has
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

        public unsafe void Begin() {
            this._glTexIdToTexIdLookup.Clear();
            this._texIdToGlTexIdLookup.Clear();

            fixed (BatchedVertex* data = this._localVertexBuffer)
                this._vertexPointer = data;

            //Bind everything
            this._vertexArray.LockingBind();
            this._indexBuffer.LockingBind();
            this._vertexBuffer.LockingBind();
            this._batchShader.LockingBind();
        }

        //These members exist to not redefine variables in Draw every time

        /// <summary>
        /// Pulled Texture Index
        /// </summary>
        private float _textureIndex;
        /// <summary>
        /// X Position
        /// </summary>
        private float _posX;
        /// <summary>
        /// Y Position
        /// </summary>
        private float _posy;
        /// <summary>
        /// Width
        /// </summary>
        private float _sizeX;
        /// <summary>
        /// Height
        /// </summary>
        private float _sizeY;

        private Matrix4x4 _rotationMatrix;
        private Vector2 _pos1;
        private Vector2 _pos2;
        private Vector2 _pos3;
        private Vector2 _pos4;

        /// <summary>
        /// Batches a Texture to Draw
        /// </summary>
        /// <param name="texture">Texture to Draw</param>
        /// <param name="position">Where to Draw</param>
        /// <param name="size">How big to draw</param>
        /// <param name="scale">How much to scale it up</param>
        /// <param name="rotation">Rotation in Radians</param>
        /// <param name="colorOverride">Color Tint</param>
        public unsafe void Draw(Texture texture, Vector2 position, Vector2 size, Vector2 scale, float rotation = 0f, Color? colorOverride = null) {
            //If we ran out of Texture Slots or are out of space in out Vertex/Index buffer, flush whats already there and start a new Batch
            if (this._indexCount >= this.MaxIndicies || this._textureSlotIndex >= this.MaxTexSlots - 1) {
                this.End();
                this.Begin();
            }

            if(scale == Vector2.Zero)
                scale = Vector2.One;

            if (size == Vector2.Zero)
                size = texture.Size;

            if(colorOverride == null)
                colorOverride = Color.White;

            size *= scale;

            this._posX  = position.X;
            this._posy  = position.Y;
            this._sizeX = size.X;
            this._sizeY = size.Y;

            if (!this._glTexIdToTexIdLookup.TryGetValue(texture.TextureId, out this._textureIndex)) {
                this._glTexIdToTexIdLookup.Add(texture.TextureId, this._textureSlotIndex);
                this._texIdToGlTexIdLookup.Add(this._textureSlotIndex, texture.TextureId);

                this._textureSlotIndex++;
            }

            _rotationMatrix = Matrix4x4.CreateRotationZ(rotation, new Vector3(position.X, position.Y, 0));
            _pos1           = Vector2.Transform(new Vector2(this._posX,                 this._posy + this._sizeY),  _rotationMatrix);
            _pos2           = Vector2.Transform(new Vector2(this._posX + this._sizeX, this._posy + this._sizeY),  _rotationMatrix);
            _pos3           = Vector2.Transform(new Vector2(this._posX + this._sizeX, this._posy),                _rotationMatrix);
            _pos4           = Vector2.Transform(new Vector2(this._posX,                 this._posy),                _rotationMatrix);

            //Vertex 1
            this._vertexPointer->Positions[0] = _pos1.X;
            this._vertexPointer->Positions[1] = _pos1.Y;
            this._vertexPointer->TexCoords[0] = 0f;
            this._vertexPointer->TexCoords[1] = 0f;
            this._vertexPointer->TexId        = this._textureIndex;
            this._vertexPointer->Color[0]     = colorOverride.Value.R;
            this._vertexPointer->Color[1]     = colorOverride.Value.G;
            this._vertexPointer->Color[2]     = colorOverride.Value.B;
            this._vertexPointer->Color[3]     = colorOverride.Value.A;
            this._vertexPointer++;

            //Vertex 2
            this._vertexPointer->Positions[0] = _pos2.X;
            this._vertexPointer->Positions[1] = _pos2.Y;
            this._vertexPointer->TexCoords[0] = 1f;
            this._vertexPointer->TexCoords[1] = 0f;
            this._vertexPointer->TexId        = this._textureIndex;
            this._vertexPointer->Color[0]     = colorOverride.Value.R;
            this._vertexPointer->Color[1]     = colorOverride.Value.G;
            this._vertexPointer->Color[2]     = colorOverride.Value.B;
            this._vertexPointer->Color[3]     = colorOverride.Value.A;
            this._vertexPointer++;

            //Vertex 3
            this._vertexPointer->Positions[0] = _pos3.X;
            this._vertexPointer->Positions[1] = _pos3.Y;
            this._vertexPointer->TexCoords[0] = 1f;
            this._vertexPointer->TexCoords[1] = 1f;
            this._vertexPointer->TexId        = this._textureIndex;
            this._vertexPointer->Color[0]     = colorOverride.Value.R;
            this._vertexPointer->Color[1]     = colorOverride.Value.G;
            this._vertexPointer->Color[2]     = colorOverride.Value.B;
            this._vertexPointer->Color[3]     = colorOverride.Value.A;
            this._vertexPointer++;

            //Vertex 4
            this._vertexPointer->Positions[0] = _pos4.X;
            this._vertexPointer->Positions[1] = _pos4.Y;
            this._vertexPointer->TexCoords[0] = 0f;
            this._vertexPointer->TexCoords[1] = 1f;
            this._vertexPointer->TexId        = this._textureIndex;
            this._vertexPointer->Color[0]     = colorOverride.Value.R;
            this._vertexPointer->Color[1]     = colorOverride.Value.G;
            this._vertexPointer->Color[2]     = colorOverride.Value.B;
            this._vertexPointer->Color[3]     = colorOverride.Value.A;
            this._vertexPointer++;

            this._indexCount        += 6;
            this._vertexBufferIndex += 128;
        }
        /// <summary>
        /// Ends the Batch and draws contents to the Screen
        /// </summary>
        public unsafe void End() {
            //Bind all textures
            for (uint i = 0; i != this._textureSlotIndex; i++) {
                this.gl.ActiveTexture((GLEnum)((uint)GLEnum.Texture0 + i));
                this.gl.BindTexture(GLEnum.Texture2D, this._texIdToGlTexIdLookup[i]);
            }

            fixed (void* data = this._localVertexBuffer) {
                this._vertexBuffer
                    .SetSubData(data, (nuint) (this._vertexBufferIndex));
            }

            //Bind the Shader and provide the Window projection matrix, to give us normal pixel space from 0,0 to whatever the window size is in the bottom right
            this._batchShader
                .SetUniform("vx_WindowProjectionMatrix", UniformType.GlMat4f, Global.GameInstance.WindowManager.ProjectionMatrix);

            //Draw
            this.gl.DrawElements(PrimitiveType.Triangles, (uint) this._indexCount, DrawElementsType.UnsignedInt, null);

            //Reset counts
            this._indexCount        = 0;
            this._textureSlotIndex  = 0;
            this._vertexBufferIndex = 0;

            this._vertexArray.Unlock();
            this._indexBuffer.Unlock();
            this._vertexBuffer.Unlock();
            this._batchShader.Unlock();
        }
        public void Dispose() {
            try {
                //Unlock Shaders and other things
                if (this._batchShader.Locked)
                    this._batchShader.Unlock();
                if (this._vertexBuffer.Locked)
                    this._vertexBuffer.Unlock();
                if (this._vertexArray.Locked)
                    this._vertexArray.Unlock();
                if (this._indexBuffer.Locked)
                    this._indexBuffer.Unlock();

                this._vertexArray.Dispose();
                this._batchShader.Dispose();
                this._vertexBuffer.Dispose();
                this._indexBuffer.Dispose();
            }
            catch {

            }
        }
    }
}