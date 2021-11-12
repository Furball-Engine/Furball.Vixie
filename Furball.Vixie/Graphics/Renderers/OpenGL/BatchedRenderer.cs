using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using FontStashSharp;
using Furball.Vixie.FontStashSharp;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Graphics.Renderers.OpenGL {
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

    public class BatchedRenderer : IDisposable, ITextureRenderer, ITextRenderer {
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
        /// <summary>
        /// Stores the Data used for the u_Textures uniform
        /// </summary>
        private readonly int[] _textureSlotIndicies;
        /// <summary>
        /// Stores whether or not the Batch has begun or not
        /// </summary>
        public bool IsBegun { get; set; }

        /// <summary>
        /// FontStashSharp renderer
        /// </summary>
        private VixieFontStashRenderer _textRenderer;

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

            //Create Lookups
            this._glTexIdToTexIdLookup = new Dictionary<uint, float>(this.MaxTexSlots);
            this._texIdToGlTexIdLookup = new Dictionary<float, uint>(this.MaxTexSlots);

            //Initialize a Array of Texture Slots, used to fill the u_Textures uniform
            this._textureSlotIndicies = new int[] {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32
            };

            //Change the Shader to the Default Shader
            this.ChangeShader(this._batchShader);

            //Create a Text Renderer, used for DrawString
            this._textRenderer = new VixieFontStashRenderer(this);
        }
        /// <summary>
        /// Initializes Constants
        /// </summary>
        /// <param name="quads">How many Quads do we allow to be drawn in 1 Batch</param>
        private void InitializeConstants(int quads) {
            this.MaxQuads     = quads;
            this.MaxVerticies = this.MaxQuads * 20 * 4;
            this.MaxIndicies  = (uint) this.MaxQuads * 6;
            this.MaxTexSlots  = Math.Min(31, Global.Device.MaxTextureImageUnits); //Adjusts based on how many Texture the GPU has
        }
        /// <summary>
        /// Changes the Currently used Shader
        /// </summary>
        /// <param name="shader">New Shader to use</param>
        public void ChangeShader(Shader shader) {
            //Unlock old Shader
            this._currentShader?.UnlockingUnbind();
            //Set new Shader and Bind it
            this._currentShader = shader;
            this._currentShader.LockingBind();

            gl.Uniform1(this._currentShader.GetUniformLocation("u_Textures"), 32, this._textureSlotIndicies);

            //If the batch has been going on while this happened we need to restart it
            if (IsBegun) {
                this.End();
                this.Begin();
            }
        }
        /// <summary>
        /// Changes the Shader to the Default one
        /// </summary>
        public void ChangeToDefaultShader() {
            this.ChangeShader(this._batchShader);
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
        /// <summary>
        /// Currently in use Shader
        /// </summary>
        private Shader _currentShader;
        /// <summary>
        /// Initializes Caches and the Vertex Pointer, aswell as binds all the necessary shaders
        /// </summary>
        public unsafe void Begin() {
            this._glTexIdToTexIdLookup.Clear();
            this._texIdToGlTexIdLookup.Clear();

            fixed (BatchedVertex* data = this._localVertexBuffer)
                this._vertexPointer = data;

            //Bind everything
            this._vertexArray.LockingBind();
            this._indexBuffer.LockingBind();
            this._vertexBuffer.LockingBind();
            this._currentShader.LockingBind();

            gl.Uniform1(this._currentShader.GetUniformLocation("u_Textures"), 32, this._textureSlotIndicies);

            this.IsBegun = true;
        }

        //These members exist to not redefine variables in Draw every time, possibly speeding stuff up

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
        private float _posY;
        /// <summary>
        /// Width
        /// </summary>
        private float _sizeX;
        /// <summary>
        /// Height
        /// </summary>
        private float _sizeY;
        /// <summary>
        /// Rotation Matrix
        /// </summary>
        private Matrix4x4 _rotationMatrix;
        /// <summary>
        /// Vertex 1 Position
        /// </summary>
        private Vector2 _pos1;
        /// <summary>
        /// Vertex 2 Position
        /// </summary>
        private Vector2 _pos2;
        /// <summary>
        /// Vertex 3 Position
        /// </summary>
        private Vector2 _pos3;
        /// <summary>
        /// Vertex 4 Position
        /// </summary>
        private Vector2 _pos4;

        /// <summary>
        /// Draws a Texture
        /// </summary>
        /// <param name="texture">Texture to Draw</param>
        /// <param name="position">Where to Draw</param>
        /// <param name="size">How big to draw, leave null to get Texture Size</param>
        /// <param name="scale">How much to scale it up, Leave null to draw at standard scale</param>
        /// <param name="rotation">Rotation in Radians, leave 0 to not rotate</param>
        /// <param name="colorOverride">Color Tint, leave null to not tint</param>
        /// <param name="sourceRect">What part of the texture to draw? Leave null to draw whole texture</param>
        /// <param name="texFlip">Horizontally/Vertically flip the Drawn Texture</param>
        public unsafe void Draw(Texture texture, Vector2 position, Vector2? size = null, Vector2? scale = null, float rotation = 0f, Color? colorOverride = null, Rectangle? sourceRect = null, TextureFlip texFlip = TextureFlip.None) {
            if (!IsBegun)
                throw new Exception("Cannot call Draw before Calling Begin in BatchRenderer!");

            //If we ran out of Texture Slots or are out of space in out Vertex/Index buffer, flush whats already there and start a new Batch
            if (this._indexCount >= this.MaxIndicies || this._textureSlotIndex >= this.MaxTexSlots - 1) {
                this.End();
                this.Begin();
            }

            //Default Scale
            if(scale == null || scale == Vector2.Zero)
                scale = Vector2.One;
            //Default Texture Size
            if (size == null || size == Vector2.Zero)
                size = texture.Size;
            //Set Size to the Source Rectangle
            if (sourceRect.HasValue)
                size = new Vector2(sourceRect.Value.Width, sourceRect.Value.Height);
            //Default Tint Color
            if(colorOverride == null)
                colorOverride = Color.White;
            //Default Rectangle
            if (sourceRect == null)
                sourceRect = new Rectangle(0, 0, (int) size.Value.X, (int) size.Value.Y);
            //Apply Scale
            size *= scale.Value;

            this._posX  = position.X;
            this._posY  = position.Y;
            this._sizeX = size.Value.X;
            this._sizeY = size.Value.Y;

            //Texture Lookup
            if (!this._glTexIdToTexIdLookup.TryGetValue(texture.TextureId, out this._textureIndex)) {
                this._glTexIdToTexIdLookup.Add(texture.TextureId, this._textureSlotIndex);
                this._texIdToGlTexIdLookup.Add(this._textureSlotIndex, texture.TextureId);

                this._textureIndex = this._textureSlotIndex;

                this._textureSlotIndex++;
            }

            //Apply Rotation
            this._rotationMatrix = Matrix4x4.CreateRotationZ(rotation, new Vector3(position.X, position.Y, 0));
            this._pos1           = Vector2.Transform(new Vector2(this._posX, this._posY + this._sizeY), this._rotationMatrix);
            this._pos2           = Vector2.Transform(new Vector2(this._posX + this._sizeX, this._posY + this._sizeY), this._rotationMatrix);
            this._pos3           = Vector2.Transform(new Vector2(this._posX + this._sizeX, this._posY), this._rotationMatrix);
            this._pos4           = Vector2.Transform(new Vector2(this._posX, this._posY), this._rotationMatrix);

            Vector2 topLeft = Vector2.Zero;
            Vector2 botRight = Vector2.Zero;

            //Apply FLipping
            switch (texFlip) {
                default:
                case TextureFlip.None:
                    topLeft  = new Vector2(sourceRect.Value.X * (1.0f / texture.Size.X), (sourceRect.Value.Y + sourceRect.Value.Height) * (1.0f / texture.Size.Y));
                    botRight = new Vector2((sourceRect.Value.X+ sourceRect.Value.Width)  * (1.0f / texture.Size.X), sourceRect.Value.Y * (1.0f / texture.Size.Y));
                    break;
                case TextureFlip.FlipVertical:
                    topLeft  = new Vector2(sourceRect.Value.X                            * (1.0f / texture.Size.X), sourceRect.Value.Y                             * (1.0f / texture.Size.Y));
                    botRight = new Vector2((sourceRect.Value.X + sourceRect.Value.Width) * (1.0f / texture.Size.X), (sourceRect.Value.Y + sourceRect.Value.Height) * (1.0f / texture.Size.Y));
                    break;
                case TextureFlip.FlipHorizontal:
                    botRight = new Vector2(sourceRect.Value.X                            * (1.0f / texture.Size.X), sourceRect.Value.Y                             * (1.0f / texture.Size.Y));
                    topLeft  = new Vector2((sourceRect.Value.X + sourceRect.Value.Width) * (1.0f / texture.Size.X), (sourceRect.Value.Y + sourceRect.Value.Height) * (1.0f / texture.Size.Y));
                    break;
            }

            //Vertex 1, Bottom Left
            this._vertexPointer->Positions[0] = this._pos1.X;
            this._vertexPointer->Positions[1] = this._pos1.Y;
            this._vertexPointer->TexCoords[0] = topLeft.X;
            this._vertexPointer->TexCoords[1] = botRight.Y;
            this._vertexPointer->TexId        = this._textureIndex;
            this._vertexPointer->Color[0]     = colorOverride.Value.R;
            this._vertexPointer->Color[1]     = colorOverride.Value.G;
            this._vertexPointer->Color[2]     = colorOverride.Value.B;
            this._vertexPointer->Color[3]     = colorOverride.Value.A;
            this._vertexPointer++;

            //Vertex 2, Bottom Right
            this._vertexPointer->Positions[0] = this._pos2.X;
            this._vertexPointer->Positions[1] = this._pos2.Y;
            this._vertexPointer->TexCoords[0] = botRight.X;
            this._vertexPointer->TexCoords[1] = botRight.Y;
            this._vertexPointer->TexId        = this._textureIndex;
            this._vertexPointer->Color[0]     = colorOverride.Value.R;
            this._vertexPointer->Color[1]     = colorOverride.Value.G;
            this._vertexPointer->Color[2]     = colorOverride.Value.B;
            this._vertexPointer->Color[3]     = colorOverride.Value.A;
            this._vertexPointer++;

            //Vertex 3, Top Right
            this._vertexPointer->Positions[0] = this._pos3.X;
            this._vertexPointer->Positions[1] = this._pos3.Y;
            this._vertexPointer->TexCoords[0] = botRight.X;
            this._vertexPointer->TexCoords[1] = topLeft.Y;
            this._vertexPointer->TexId        = this._textureIndex;
            this._vertexPointer->Color[0]     = colorOverride.Value.R;
            this._vertexPointer->Color[1]     = colorOverride.Value.G;
            this._vertexPointer->Color[2]     = colorOverride.Value.B;
            this._vertexPointer->Color[3]     = colorOverride.Value.A;
            this._vertexPointer++;

            //Vertex 4, Top Left
            this._vertexPointer->Positions[0] = this._pos4.X;
            this._vertexPointer->Positions[1] = this._pos4.Y;
            this._vertexPointer->TexCoords[0] = topLeft.X;
            this._vertexPointer->TexCoords[1] = topLeft.Y;
            this._vertexPointer->TexId        = this._textureIndex;
            this._vertexPointer->Color[0]     = colorOverride.Value.R;
            this._vertexPointer->Color[1]     = colorOverride.Value.G;
            this._vertexPointer->Color[2]     = colorOverride.Value.B;
            this._vertexPointer->Color[3]     = colorOverride.Value.A;
            this._vertexPointer++;

            this._indexCount        += 6;
            this._vertexBufferIndex += 160;
        }
        /// <summary>
        /// Batches Text to the Screen
        /// </summary>
        /// <param name="font">Font to Use</param>
        /// <param name="text">Text to Write</param>
        /// <param name="position">Where to Draw</param>
        /// <param name="color">What color to draw</param>
        /// <param name="rotation">Rotation of the text</param>
        /// <param name="scale">Scale of the text, leave null to draw at standard scale</param>
        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation = 0f, Vector2? scale = null) {
            //Default Scale
            if(scale == null || scale == Vector2.Zero)
                scale = Vector2.One;

            //Draw
            font.DrawText(this._textRenderer, text, position, System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B), scale.Value, rotation);
        }
        /// <summary>
        /// Batches Text to the Screen
        /// </summary>
        /// <param name="font">Font to Use</param>
        /// <param name="text">Text to Write</param>
        /// <param name="position">Where to Draw</param>
        /// <param name="color">What color to draw</param>
        /// <param name="rotation">Rotation of the text</param>
        /// <param name="scale">Scale of the text, leave null to draw at standard scale</param>
        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color color, float rotation = 0f, Vector2? scale = null) {
            //Default Scale
            if(scale == null || scale == Vector2.Zero)
                scale = Vector2.One;

            //Draw
            font.DrawText(this._textRenderer, text, position, color, scale.Value, rotation);
        }
        /// <summary>
        /// Batches Colorful text to the Screen
        /// </summary>
        /// <param name="font">Font to Use</param>
        /// <param name="text">Text to Write</param>
        /// <param name="position">Where to Draw</param>
        /// <param name="colors">What colors to use</param>
        /// <param name="rotation">Rotation of the text</param>
        /// <param name="scale">Scale of the text, leave null to draw at standard scale</param>
        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color[] colors, float rotation = 0f, Vector2? scale = null) {
            //Default Scale
            if(scale == null || scale == Vector2.Zero)
                scale = Vector2.One;

            //Draw
            font.DrawText(this._textRenderer, text, position, colors, scale.Value, rotation);
        }

        /// <summary>
        /// Ends the Batch and draws contents to the Screen
        /// </summary>
        public unsafe void End() {
            //Bind all textures
            for (uint i = 0; i != this._textureSlotIndex; i++) {
                TextureUnit textureSlot = (TextureUnit)((uint)TextureUnit.Texture0 + i);
                //Find Texture
                uint lookup = this._texIdToGlTexIdLookup[i];
                //Set as Active and Bind
                this.gl.ActiveTexture(textureSlot);
                this.gl.BindTexture(GLEnum.Texture2D, lookup);
                //Make sure to set BoundTextures to this, so its always clear what texture is bound where
                Texture.BoundTextures[textureSlot] = lookup;
            }

            //Upload new vertex data to the GPU
            fixed (void* data = this._localVertexBuffer) {
                this._vertexBuffer
                    .SetSubData(data, (nuint) (this._vertexBufferIndex));
            }

            //Bind the Shader and provide the Window projection matrix, to give us normal pixel space from 0,0 to whatever the window size is in the bottom right
            this._batchShader
                .SetUniform("vx_WindowProjectionMatrix", UniformType.GlMat4F, Global.GameInstance.WindowManager.ProjectionMatrix);

            //Draw
            this.gl.DrawElements(PrimitiveType.Triangles, (uint) this._indexCount, DrawElementsType.UnsignedInt, null);

            //Reset counts
            this._indexCount        = 0;
            this._textureSlotIndex  = 0;
            this._vertexBufferIndex = 0;

            //Unlock all
            this._vertexArray.Unlock();
            this._indexBuffer.Unlock();
            this._vertexBuffer.Unlock();
            this._batchShader.Unlock();

            //Reset Begun Flag
            this.IsBegun = false;
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

                //Dispose
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