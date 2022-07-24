using System;
using System.Drawing;
using System.Numerics;
using FontStashSharp;
using Furball.Vixie.Backends.OpenGL.Shared;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.FontStashSharp;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using Silk.NET.OpenGL.Legacy;
using Color=Furball.Vixie.Backends.Shared.Color;
using ShaderType=Silk.NET.OpenGL.ShaderType;
using Texture=Furball.Vixie.Backends.Shared.Texture;

namespace Furball.Vixie.Backends.OpenGL; 

internal class FakeInstancingQuadRenderer : IQuadRenderer {
    private readonly OpenGLBackend       _backend;
    private readonly GL                  _gl;
    private readonly VertexArrayObjectGL _vao;

    private readonly ShaderGL _program;
    private readonly BufferObjectGL     _vertexBuffer;

    private bool disposed = true;
    public void Dispose() {
        this._backend.CheckThread();
        if (this.disposed) return;
            
        this._program.Dispose();
        this._vertexBuffer.Dispose();
        this._vao.Dispose();

        this.disposed = true;
    }
        
    ~FakeInstancingQuadRenderer() {
        DisposeQueue.Enqueue(this);
    }

    public bool IsBegun {
        get;
        set;
    }

    internal FakeInstancingQuadRenderer(OpenGLBackend backend) {
        this._backend = backend;
        this._backend.CheckThread();
        this._gl = backend.GetLegacyGL();

        string vertex   = ResourceHelpers.GetStringResource("Shaders/FakeInstancingQuadRenderer/VertexShader.glsl");
        string fragment = FakeInstancingQuadShaderGenerator.GetFragment(backend);

        this._vao = new VertexArrayObjectGL(this._backend);
        this._vao.Bind();
        
        this._program = new(this._backend);

        this._program.AttachShader(ShaderType.VertexShader, vertex)
            .AttachShader(ShaderType.FragmentShader, fragment)
            .Link();
            
        this._program.Bind();
            
        this._backend.CheckError("create shaders");

        for (int i = 0; i < backend.QueryMaxTextureUnits(); i++) {
            this._program.BindUniformToTexUnit($"tex_{i}", i);
        }

        this._textRenderer = new VixieFontStashRenderer(this._backend, this);

        for (ushort i = 0; i < BATCH_COUNT; i++) {
            //Top left
            this.BatchedVertices[i * 4].VertexPosition.X          = 0;
            this.BatchedVertices[i * 4].VertexPosition.Y          = 0;
            this.BatchedVertices[i * 4].VertexTextureCoordinate.X = 0;
            this.BatchedVertices[i * 4].VertexTextureCoordinate.Y = 1;
            this.BatchedVertices[i * 4].VertexQuadIndex           = i;

            //Top right
            this.BatchedVertices[i * 4 + 1].VertexPosition.X          = 1;
            this.BatchedVertices[i * 4 + 1].VertexPosition.Y          = 0;
            this.BatchedVertices[i * 4 + 1].VertexTextureCoordinate.X = 1;
            this.BatchedVertices[i * 4 + 1].VertexTextureCoordinate.Y = 1;
            this.BatchedVertices[i * 4 + 1].VertexQuadIndex           = i;

            //Bottom left
            this.BatchedVertices[i * 4 + 2].VertexPosition.X          = 1;
            this.BatchedVertices[i * 4 + 2].VertexPosition.Y          = 1;
            this.BatchedVertices[i * 4 + 2].VertexTextureCoordinate.X = 1;
            this.BatchedVertices[i * 4 + 2].VertexTextureCoordinate.Y = 0;
            this.BatchedVertices[i * 4 + 2].VertexQuadIndex           = i;

            //Bottom right
            this.BatchedVertices[i * 4 + 3].VertexPosition.X          = 0;
            this.BatchedVertices[i * 4 + 3].VertexPosition.Y          = 1;
            this.BatchedVertices[i * 4 + 3].VertexTextureCoordinate.X = 0;
            this.BatchedVertices[i * 4 + 3].VertexTextureCoordinate.Y = 0;
            this.BatchedVertices[i * 4 + 3].VertexQuadIndex           = i;
        }

        for (ushort i = 0; i < BATCH_COUNT; i++) {
            //Top left
            this.BatchedIndicies[i * 6] = (ushort)(0 + i * 4);
            //Top right
            this.BatchedIndicies[i * 6 + 1] = (ushort)(1 + i * 4);
            //Bottom left
            this.BatchedIndicies[i * 6 + 2] = (ushort)(2 + i * 4);
            //Top right
            this.BatchedIndicies[i * 6 + 3] = (ushort)(2 + i * 4);
            //Bottom left
            this.BatchedIndicies[i * 6 + 4] = (ushort)(3 + i * 4);
            //Bottom right
            this.BatchedIndicies[i * 6 + 5] = (ushort)(0 + i * 4);
        }

        this._vertexBuffer = new BufferObjectGL(backend, Silk.NET.OpenGL.BufferTargetARB.ArrayBuffer, Silk.NET.OpenGL.BufferUsageARB.StaticDraw);

        this._vertexBuffer.Bind();
        this._vertexBuffer.SetData<BatchedVertex>(this.BatchedVertices);

        VertexBufferLayoutGL layout = new();
        layout.AddElement<float>(2);
        layout.AddElement<float>(2);
        layout.AddElement<float>(1);

        this._vao.AddBuffer(this._vertexBuffer, layout);

        this._vao.Unbind();
    }

    public unsafe void Begin() {
        this._backend.CheckThread();
        this.IsBegun = true;

        this._program.Bind();

        this._program.SetUniform("u_ProjectionMatrix", this._backend.ProjectionMatrix);

        this._vao.Bind();
        this._vertexBuffer.Bind();
    }

    private int GetTextureId(TextureGL tex) {
        this._backend.CheckThread();
        if (this.UsedTextures != 0)
            for (int i = 0; i < this.UsedTextures; i++) {
                uint tex2 = this.TextureArray[i];

                if (tex.TextureId == tex2) return i;
            }

        this.TextureArray[this.UsedTextures] = tex.TextureId;
        this.UsedTextures++;

        return this.UsedTextures - 1;
    }

    public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        this._backend.CheckThread();
        if (!this.IsBegun)
            throw new Exception("Begin() has not been called!");

        //Ignore calls with invalid textures
        if (texture is not TextureGL textureGl)
            return;

        if (scale.X == 0 || scale.Y == 0 || colorOverride.A == 0) return;

        // Checks if we have filled the current batch or run out of texture slots
        if (this.BatchedQuadCount == BATCH_COUNT || this.UsedTextures == this._backend.QueryMaxTextureUnits()) {
            this.Flush();
        }

        this.BatchedColors[this.BatchedQuadCount]               = colorOverride;
        this.BatchedPositions[this.BatchedQuadCount]            = position;
        this.BatchedSizes[this.BatchedQuadCount]                = texture.Size * scale;
        this.BatchedRotationOrigins[this.BatchedQuadCount]      = rotOrigin;
        this.BatchedRotations[this.BatchedQuadCount]            = rotation;
        this.BatchedTextureIds[this.BatchedQuadCount]           = this.GetTextureId(textureGl);
        this.BatchedTextureCoordinates[this.BatchedQuadCount].X = 0;
        this.BatchedTextureCoordinates[this.BatchedQuadCount].Y = 0;
        this.BatchedTextureCoordinates[this.BatchedQuadCount].Z = texFlip == TextureFlip.FlipHorizontal ? -1 : 1;
        this.BatchedTextureCoordinates[this.BatchedQuadCount].W = texFlip == TextureFlip.FlipVertical ? -1 : 1;

        if (textureGl.IsFramebufferTexture)
            this.BatchedTextureCoordinates[this.BatchedQuadCount].W *= -1;

        this.BatchedQuadCount++;
    }

    public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        this._backend.CheckThread();
        if (!this.IsBegun)
            throw new Exception("Begin() has not been called!");

        //Ignore calls with invalid textures
        if (texture is not TextureGL textureGl)
            return;

        if (scale.X == 0 || scale.Y == 0 || colorOverride.A == 0) return;

        // Checks if we have filled the batch or run out of textures
        if (this.BatchedQuadCount == BATCH_COUNT || this.UsedTextures == this._backend.QueryMaxTextureUnits()) {
            this.Flush();
        }

        //Set Size to the Source Rectangle
        Vector2 size = new Vector2(sourceRect.Width, sourceRect.Height);

        //Apply Scale
        size *= scale;

        sourceRect.Y = texture.Height - sourceRect.Y - sourceRect.Height;

        this.BatchedColors[this.BatchedQuadCount]               = colorOverride;
        this.BatchedPositions[this.BatchedQuadCount]            = position;
        this.BatchedSizes[this.BatchedQuadCount]                = size;
        this.BatchedRotationOrigins[this.BatchedQuadCount]      = rotOrigin;
        this.BatchedRotations[this.BatchedQuadCount]            = rotation;
        this.BatchedTextureIds[this.BatchedQuadCount]           = this.GetTextureId(textureGl);
        this.BatchedTextureCoordinates[this.BatchedQuadCount].X = (float)sourceRect.X                       / texture.Width;
        this.BatchedTextureCoordinates[this.BatchedQuadCount].Y = (float)sourceRect.Y                       / texture.Height;
        this.BatchedTextureCoordinates[this.BatchedQuadCount].Z = (float)sourceRect.Width  / texture.Width  * (texFlip == TextureFlip.FlipHorizontal ? -1 : 1);
        this.BatchedTextureCoordinates[this.BatchedQuadCount].W = (float)sourceRect.Height / texture.Height * (texFlip == TextureFlip.FlipVertical ? -1 : 1);

        this.BatchedQuadCount++;
    }

    internal struct BatchedVertex {
        public Vector2 VertexPosition;
        public Vector2 VertexTextureCoordinate;
        public float   VertexQuadIndex;
    }

    private const int BATCH_COUNT = 128;
        
    // private int _quadColorsUniformPosition;             //u_QuadColors
    // private int _quadPositionsUniformPosition;          //u_QuadPositions
    // private int _quadSizesUniformPosition;              //u_QuadSizes
    // private int _quadRotationOriginsUniformPosition;    //u_QuadRotationOrigins
    // private int _quadRotationsUniformPosition;          //u_QuadRotations
    // private int _quadTextureIdsUniformPosition;         //u_QuadTextureIds
    // private int _quadTextureCoordinatesUniformPosition; //u_QuadTextureCoordinates

    private int BatchedQuadCount = 0;
    private int UsedTextures = 0;

    private BatchedVertex[] BatchedVertices           = new BatchedVertex[BATCH_COUNT * 4];
    private ushort[]        BatchedIndicies           = new ushort[BATCH_COUNT        * 6];
    private Color[]         BatchedColors             = new Color[BATCH_COUNT];
    private Vector2[]       BatchedPositions          = new Vector2[BATCH_COUNT];
    private Vector2[]       BatchedSizes              = new Vector2[BATCH_COUNT];
    private Vector2[]       BatchedRotationOrigins    = new Vector2[BATCH_COUNT];
    private float[]         BatchedRotations          = new float[BATCH_COUNT];
    private float[]         BatchedTextureIds         = new float[BATCH_COUNT];
    private Vector4[]       BatchedTextureCoordinates = new Vector4[BATCH_COUNT];

    private readonly uint[]                 TextureArray = new uint[32];
    private readonly VixieFontStashRenderer _textRenderer;

    private unsafe void Flush() {
        this._backend.CheckThread();
        if (this.BatchedQuadCount == 0) return;

        //Bind all the textures
        for (var i = 0; i < this.UsedTextures; i++) {
            uint tex = this.TextureArray[i];

            this._gl.ActiveTexture(TextureUnit.Texture0 + i);
            this._gl.BindTexture(TextureTarget.Texture2D, tex);
        }
        this._backend.CheckError("bind texes");

        this._program.SetUniform4("u_QuadColors", this.BatchedColors, this.BatchedQuadCount);
        this._program.SetUniform2("u_QuadPositions", this.BatchedPositions, this.BatchedQuadCount);
        this._program.SetUniform2("u_QuadSizes", this.BatchedSizes, this.BatchedQuadCount);
        this._program.SetUniform2("u_QuadRotationOrigins", this.BatchedRotationOrigins, this.BatchedQuadCount);
        this._program.SetUniform1("u_QuadRotations", this.BatchedRotations, this.BatchedQuadCount);
        this._program.SetUniform1("u_QuadTextureIds", this.BatchedTextureIds, this.BatchedQuadCount);
        this._program.SetUniform4("u_QuadTextureCoordinates", this.BatchedTextureCoordinates, this.BatchedQuadCount);
        
        this._gl.DrawElements<ushort>(PrimitiveType.Triangles, (uint)(this.BatchedQuadCount * 6), DrawElementsType.UnsignedShort, this.BatchedIndicies);

        this._backend.CheckError("draw elements");

        this.BatchedQuadCount = 0;
        this.UsedTextures = 0;
    }

    public void End() {
        this._backend.CheckThread();
        this.IsBegun = false;
        this.Flush();

        this._program.Unbind();
        this._vertexBuffer.Unbind();
        this._vao.Unbind();
    }

    #region overloads

    public void Draw(Texture texture, Vector2 position, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
        this.Draw(texture, position, Vector2.One, rotation, Color.White, flip, rotOrigin);
    }

    public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
        this.Draw(texture, position, scale, rotation, Color.White, flip, rotOrigin);
    }

    public void Draw(Texture texture, Vector2 position, Vector2 scale, Color colorOverride, float rotation = 0, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        this.Draw(texture, position, scale, rotation, colorOverride, texFlip, rotOrigin);
    }

    #endregion

    #region text

    /// <summary>
    ///     Batches Text to the Screen
    /// </summary>
    /// <param name="font">Font to Use</param>
    /// <param name="text">Text to Write</param>
    /// <param name="position">Where to Draw</param>
    /// <param name="color">What color to draw</param>
    /// <param name="rotation">Rotation of the text</param>
    /// <param name="origin">The rotation origin of the text</param>
    /// <param name="scale">Scale of the text, leave null to draw at standard scale</param>
    public void DrawString(DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation = 0f, Vector2? scale = null, Vector2 origin = default) {
        //Default Scale
        if (scale == null || scale == Vector2.Zero)
            scale = Vector2.One;

        //Draw
        font.DrawText(this._textRenderer, text, position, System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B), scale.Value, rotation, origin);
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
    public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color color, float rotation = 0f, Vector2? scale = null, Vector2 origin = default) {
        //Default Scale
        if (scale == null || scale == Vector2.Zero)
            scale = Vector2.One;

        //Draw
        font.DrawText(this._textRenderer, text, position, color, scale.Value, rotation, origin);
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
    public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color[] colors, float rotation = 0f, Vector2? scale = null, Vector2 origin = default) {
        //Default Scale
        if (scale == null || scale == Vector2.Zero)
            scale = Vector2.One;

        //Draw
        font.DrawText(this._textRenderer, text, position, colors, scale.Value, rotation, origin);
    }
    #endregion
}