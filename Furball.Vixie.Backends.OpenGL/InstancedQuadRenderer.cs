using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using FontStashSharp;
using Furball.Vixie.Backends.OpenGL.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.FontStashSharp;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using Silk.NET.OpenGL;
using Color=Furball.Vixie.Backends.Shared.Color;
using Texture=Furball.Vixie.Backends.Shared.Texture;

namespace Furball.Vixie.Backends.OpenGL; 

public class InstancedQuadRenderer : IQuadRenderer {
    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex {
        public Vector2 Position;
        public Vector2 TexturePosition;
    }

    private static Vertex[] _vertices = {
        new() {
            Position        = new Vector2(0, 0),
            TexturePosition = new Vector2(0, 1)
        },
        new() {
            Position        = new Vector2(1, 0),
            TexturePosition = new Vector2(1, 1)
        },
        new() {
            Position        = new Vector2(1, 1),
            TexturePosition = new Vector2(1, 0)
        },
        new() {
            Position        = new Vector2(0, 1),
            TexturePosition = new Vector2(0, 0)
        }
    };
    private static ushort[] _indicies = {
        //Tri 1
        0, 1, 2,
        //Tri 2
        2, 3, 0
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct InstanceData {
        public Vector2 Position;
        public Vector2 Size;
        public Color   Color;
        public Vector2 TextureRectPosition;
        public Vector2 TextureRectSize;
        public Vector2 RotationOrigin;
        public float   Rotation;
        public int     TextureId;
    }

    private BufferObjectGL      _vbo;
    private BufferObjectGL      _instanceVbo;
    private VertexArrayObjectGL _vao;

    private VixieFontStashRenderer _textRenderer;

    private ShaderGL _shaderGl41;

    private OpenGLBackend _backend;
    // ReSharper disable once InconsistentNaming
    private GL gl;

    public unsafe InstancedQuadRenderer(OpenGLBackend backend) {
        this._backend = backend;
        this._backend.CheckThread();

        this.gl = this._backend.GetModernGL();

        this._boundTextures = new uint[this._backend.QueryMaxTextureUnits()];
        for (var i = 0; i < this._boundTextures.Length; i++) {
            this._boundTextures[i] = uint.MaxValue;
        }
            
        string vertSource = ResourceHelpers.GetStringResource("Shaders/InstancedQuadRenderer/VertexShader.glsl");
        string fragSource = InstancedQuadShaderGenerator.GetFragment(backend);

        if (backend.CreationBackend == Backend.OpenGLES) {
            const string glVersion = "#version 140";
            const string glesVersion = "#version 300 es";
            
            vertSource = vertSource.Replace(glVersion, glesVersion);
            fragSource = fragSource.Replace(glVersion, glesVersion);
        }

        this._shaderGl41 = new ShaderGL(backend);

        this._shaderGl41.AttachShader(ShaderType.VertexShader,   vertSource);
        this._shaderGl41.AttachShader(ShaderType.FragmentShader, fragSource);
        this._shaderGl41.Link();

        this._shaderGl41.Bind();

        this._backend.CheckError("bind textures");

        for (int i = 0; i < backend.QueryMaxTextureUnits(); i++) {
            this._shaderGl41.BindUniformToTexUnit($"tex_{i}", i);
        }

        this._vao = new VertexArrayObjectGL(backend);
        this._vao.Bind();

        this._vbo = new BufferObjectGL(backend, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        this._vbo.Bind();
        this._vbo.SetData<Vertex>(_vertices);

        VertexBufferLayoutGL layout = new();
        layout.AddElement<float>(2);
        layout.AddElement<float>(2);

        this._vao.AddBuffer(this._vbo, layout);
            
        this._instanceVbo = new BufferObjectGL(backend, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
        this._instanceVbo.Bind();
        this._instanceVbo.SetData(null, (nuint)(sizeof(InstanceData) * NUM_INSTANCES));

        layout = new VertexBufferLayoutGL();
        //Position
        layout.AddElement<float>(2, false, 1);
        //Size
        layout.AddElement<float>(2, false, 1);
        //Color
        layout.AddElement<float>(4, false, 1);
        //Texture Position
        layout.AddElement<float>(2, false, 1);
        //Texture size
        layout.AddElement<float>(2, false, 1);
        //Rotation origin
        layout.AddElement<float>(2, false, 1);
        //Rotation
        layout.AddElement<float>(1, false, 1);
        //Texture ID
        layout.AddElement<int>(1, false, 1);

        this._vao.AddBuffer(this._instanceVbo, layout, 2);

        this._backend.CheckError("more vtx attrib stuffs");

        this._instanceVbo.Unbind();
        this._vao.Unbind();

        this._textRenderer = new VixieFontStashRenderer(this._backend, this);
    }

    public void Dispose() {
        this._backend.CheckThread();
        this._shaderGl41.Dispose();
        this._vao.Dispose();
        this._vbo.Dispose();
        this._instanceVbo.Dispose();
    }

    public bool IsBegun {
        get;
        set;
    }

    public void Begin() {
        this._backend.CheckThread();
        this._shaderGl41.Bind();

        this._shaderGl41.SetUniform("vx_WindowProjectionMatrix", this._backend.ProjectionMatrix);

        this._instances    = 0;
        this._usedTextures = 0;

        this._vao.Bind();
        this._backend.CheckError("bind vao");

        // this._InstanceVBO.SetData<InstanceData>(this._instanceData);
        this._instanceVbo.Bind();

        this.IsBegun = true;
    }

    public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        this._backend.CheckThread();
        if (!this.IsBegun)
            throw new Exception("Begin() has not been called!");

        //Ignore calls with invalid textures
        if (texture == null || texture is not TextureGL textureGl41)
            return;

        if (this._instances >= NUM_INSTANCES || this._usedTextures == this._backend.QueryMaxTextureUnits()) {
            this.Flush();
        }
            
        this._instanceData[this._instances].Position              = position;
        this._instanceData[this._instances].Size                  = texture.Size * scale;
        this._instanceData[this._instances].Color                 = colorOverride;
        this._instanceData[this._instances].Rotation              = rotation;
        this._instanceData[this._instances].RotationOrigin        = rotOrigin;
        this._instanceData[this._instances].TextureId             = this.GetTextureId(textureGl41);
        this._instanceData[this._instances].TextureRectPosition.X = 0;
        this._instanceData[this._instances].TextureRectPosition.Y = 0;
        this._instanceData[this._instances].TextureRectSize.X     = texFlip == TextureFlip.FlipHorizontal ? -1 : 1;
        this._instanceData[this._instances].TextureRectSize.Y     = texFlip == TextureFlip.FlipVertical ? -1 : 1;

        if(textureGl41.IsFramebufferTexture)
            this._instanceData[this._instances].TextureRectSize.Y *= -1;

        this._instances++;
    }

    public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        this._backend.CheckThread();
        if (!this.IsBegun)
            throw new Exception("Begin() has not been called!");

        //Ignore calls with invalid textures
        if (texture == null || texture is not TextureGL texGl)
            return;

        if (this._instances >= NUM_INSTANCES || this._usedTextures == this._backend.QueryMaxTextureUnits()) {
            this.Flush();
        }

        //Set Size to the Source Rectangle
        Vector2 size = new Vector2(sourceRect.Width, sourceRect.Height);

        //Apply Scale
        size *= scale;
            
        sourceRect.Y = texture.Height - sourceRect.Y - sourceRect.Height;

        this._instanceData[this._instances].Position              = position;
        this._instanceData[this._instances].Size                  = size;
        this._instanceData[this._instances].Color                 = colorOverride;
        this._instanceData[this._instances].Rotation              = rotation;
        this._instanceData[this._instances].RotationOrigin        = rotOrigin;
        this._instanceData[this._instances].TextureId             = this.GetTextureId(texGl);
        this._instanceData[this._instances].TextureRectPosition.X = (float)sourceRect.X      / texture.Width;
        this._instanceData[this._instances].TextureRectPosition.Y = (float)sourceRect.Y      / texture.Height;
        this._instanceData[this._instances].TextureRectSize.X     = (float)sourceRect.Width  / texture.Width * (texFlip == TextureFlip.FlipHorizontal ? -1 : 1);
        this._instanceData[this._instances].TextureRectSize.Y     = (float)sourceRect.Height / texture.Height * (texFlip == TextureFlip.FlipVertical ? -1 : 1);

        this._instances++;
    }

    public void Draw(Texture texture, Vector2 position, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
        this.Draw(texture, position, Vector2.One, rotation, Color.White, flip, rotOrigin);
    }

    public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
        this.Draw(texture, position, scale, rotation, Color.White, flip, rotOrigin);
    }

    public void Draw(Texture texture, Vector2 position, Vector2 scale, Color colorOverride, float rotation = 0, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        this.Draw(texture, position, scale, rotation, colorOverride, texFlip, rotOrigin);
    }

    private readonly uint[] _boundTextures;
    private          int    _usedTextures = 0;


    private int GetTextureId(TextureGL tex) {
        this._backend.CheckThread();
        if(this._usedTextures != 0)
            for (int i = 0; i < this._usedTextures; i++) {
                uint tex2 = this._boundTextures[i];

                if (tex2          == uint.MaxValue) break;
                if (tex.TextureId == tex2) return i;
            }

        this._boundTextures[this._usedTextures] = tex.TextureId;
        this._usedTextures++;

        return this._usedTextures - 1;
    }

    public const int NUM_INSTANCES = 1024;

    private          uint           _instances    = 0;
    private readonly InstanceData[] _instanceData = new InstanceData[NUM_INSTANCES];

    private unsafe void Flush() {
        this._backend.CheckThread();
        if (this._instances == 0) return;

        this._backend.BindTextures(this._boundTextures, (uint) this._usedTextures);
        this._backend.CheckError("bind textures");
        for (var i = 0; i < this._boundTextures.Length; i++) {
            this._boundTextures[i] = uint.MaxValue;
        }
            
        fixed (void* ptr = this._instanceData)
            this._instanceVbo.SetSubData(ptr, (nuint)(this._instances * sizeof(InstanceData)));

        this.gl.DrawElementsInstanced<ushort>(PrimitiveType.TriangleStrip, 6, DrawElementsType.UnsignedShort, _indicies, this._instances);
        this._backend.CheckError("quad renderer draw");

        this._instances    = 0;
        this._usedTextures = 0;
    }

    public void End() {
        this._backend.CheckThread();
        this.Flush();
        this.IsBegun = false;
    }

    #region text

    /// <summary>
    /// Batches Text to the Screen
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
        if(scale == null || scale == Vector2.Zero)
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
        if(scale == null || scale == Vector2.Zero)
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
        if(scale == null || scale == Vector2.Zero)
            scale = Vector2.One;

        //Draw
        font.DrawText(this._textRenderer, text, position, colors, scale.Value, rotation, origin);
    }
    #endregion
}