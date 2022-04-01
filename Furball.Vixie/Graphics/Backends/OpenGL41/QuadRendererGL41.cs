using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using FontStashSharp;
using Furball.Vixie.FontStashSharp;
using Furball.Vixie.Graphics.Backends.OpenGL_;
using Furball.Vixie.Graphics.Backends.OpenGL41.Abstractions;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Graphics.Backends.OpenGL41 {
    public class QuadRendererGL41 : IQuadRenderer {
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
        private VertexArrayObjectGL41 _vao;

        private VixieFontStashRenderer _textRenderer;

        private ShaderGL41 _shaderGl41;

        private OpenGL41Backend _backend;
        // ReSharper disable once InconsistentNaming
        private GL gl;

        public unsafe QuadRendererGL41(OpenGL41Backend backend) {
            this._backend = backend;
            this._backend.CheckThread();

            this.gl       = this._backend.GetGlApi();

            this._boundTextures = new TextureGL41[this._backend.QueryMaxTextureUnits()];

            string vertSource = ResourceHelpers.GetStringResource("ShaderCode/OpenGL41/InstancedRenderer/VertexShader.glsl");
            string fragSource = QuadShaderGeneratorGL41.GetFragment(backend);

            this._shaderGl41 = new ShaderGL41(backend);

            this._shaderGl41.AttachShader(ShaderType.VertexShader,   vertSource);
            this._shaderGl41.AttachShader(ShaderType.FragmentShader, fragSource);
            this._shaderGl41.Link();

            this._shaderGl41.Bind();

            this._backend.CheckError();

            for (int i = 0; i < backend.QueryMaxTextureUnits(); i++) {
                this._shaderGl41.BindUniformToTexUnit($"tex_{i}", i);
            }

            this._vao = new VertexArrayObjectGL41(backend);
            this._vao.Bind();

            this._vbo = new BufferObjectGL(backend, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
            this._vbo.Bind();
            this._vbo.SetData<Vertex>(_vertices);

            //Vertex Position
            this.gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)0);
            //Texture position
            this.gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)sizeof(Vector2));
            this._backend.CheckError();

            this.gl.EnableVertexAttribArray(0);
            this.gl.EnableVertexAttribArray(1);
            this._backend.CheckError();

            this._instanceVbo = new BufferObjectGL(backend, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
            this._instanceVbo.Bind();

            this._instanceVbo.SetData(null, (nuint)(sizeof(InstanceData) * NUM_INSTANCES));

            int ptrPos = 0;
            //Position
            this.gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            this.gl.VertexAttribDivisor(2, 1);
            ptrPos += sizeof(Vector2);
            //Size
            this.gl.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            this.gl.VertexAttribDivisor(3, 1);
            ptrPos += sizeof(Vector2);
            //Color
            this.gl.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            this.gl.VertexAttribDivisor(4, 1);
            ptrPos += sizeof(Color);
            //Texture position
            this.gl.VertexAttribPointer(5, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            this.gl.VertexAttribDivisor(5, 1);
            ptrPos += sizeof(Vector2);
            //Texture size
            this.gl.VertexAttribPointer(6, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            this.gl.VertexAttribDivisor(6, 1);
            ptrPos += sizeof(Vector2);
            //Rotation origin
            this.gl.VertexAttribPointer(7, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            this.gl.VertexAttribDivisor(7, 1);
            ptrPos += sizeof(Vector2);
            //Rotation
            this.gl.VertexAttribPointer(8, 1, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            this.gl.VertexAttribDivisor(8, 1);
            ptrPos += sizeof(float);
            //Texture id
            this.gl.VertexAttribIPointer(9, 1, VertexAttribIType.Int, (uint)sizeof(InstanceData), (void*)ptrPos);
            this.gl.VertexAttribDivisor(9, 1);
            ptrPos += sizeof(int);

            this.gl.EnableVertexAttribArray(2);
            this.gl.EnableVertexAttribArray(3);
            this.gl.EnableVertexAttribArray(4);
            this.gl.EnableVertexAttribArray(5);
            this.gl.EnableVertexAttribArray(6);
            this.gl.EnableVertexAttribArray(7);
            this.gl.EnableVertexAttribArray(8);
            this.gl.EnableVertexAttribArray(9);

            this._backend.CheckError();

            this._instanceVbo.Unbind();
            this._vao.Unbind();

            this._textRenderer = new VixieFontStashRenderer(this._backend, this);
        }

        public void Dispose() {
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
            this._shaderGl41.Bind();

            this._shaderGl41.SetUniform("vx_ModifierX",       Global.GameInstance.WindowManager.PositionMultiplier.X)
                   .SetUniform("vx_ModifierY",              Global.GameInstance.WindowManager.PositionMultiplier.Y)
                   .SetUniform("vx_WindowProjectionMatrix", this._backend.ProjectionMatrix);

            this._instances = 0;
            this._usedTextures = 0;

            this.IsBegun = true;
        }

        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, float rotation, Color colorOverride, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            if (!this.IsBegun)
                throw new Exception("Begin() has not been called!");

            //Ignore calls with invalid textures
            if (textureGl == null)
                return;

            if (this._instances >= NUM_INSTANCES || this._usedTextures == this._backend.QueryMaxTextureUnits()) {
                this.Flush();
            }
            
            this._instanceData[this._instances].Position              = position;
            this._instanceData[this._instances].Size                  = textureGl.Size * scale;
            this._instanceData[this._instances].Color                 = colorOverride;
            this._instanceData[this._instances].Rotation              = rotation;
            this._instanceData[this._instances].RotationOrigin        = rotOrigin;
            this._instanceData[this._instances].TextureId             = this.GetTextureId(textureGl);
            this._instanceData[this._instances].TextureRectPosition.X = 0;
            this._instanceData[this._instances].TextureRectPosition.Y = 0;
            this._instanceData[this._instances].TextureRectSize.X     = texFlip == TextureFlip.FlipHorizontal ? -1 : 1;
            this._instanceData[this._instances].TextureRectSize.Y     = texFlip == TextureFlip.FlipVertical ? -1 : 1;

            this._instances++;
        }

        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            if (!this.IsBegun)
                throw new Exception("Begin() has not been called!");

            //Ignore calls with invalid textures
            if (textureGl == null || textureGl is not TextureGL41)
                return;

            if (this._instances >= NUM_INSTANCES || this._usedTextures == this._backend.QueryMaxTextureUnits()) {
                this.Flush();
            }

            //Set Size to the Source Rectangle
            Vector2 size = new Vector2(sourceRect.Width, sourceRect.Height);

            //Apply Scale
            size *= scale;
            
            sourceRect.Y = textureGl.Height - sourceRect.Y - sourceRect.Height;

            this._instanceData[this._instances].Position              = position;
            this._instanceData[this._instances].Size                  = size;
            this._instanceData[this._instances].Color                 = colorOverride;
            this._instanceData[this._instances].Rotation              = rotation;
            this._instanceData[this._instances].RotationOrigin        = rotOrigin;
            this._instanceData[this._instances].TextureId             = this.GetTextureId(textureGl);
            this._instanceData[this._instances].TextureRectPosition.X = (float)sourceRect.X      / textureGl.Width;
            this._instanceData[this._instances].TextureRectPosition.Y = (float)sourceRect.Y      / textureGl.Height;
            this._instanceData[this._instances].TextureRectSize.X     = (float)sourceRect.Width  / textureGl.Width * (texFlip == TextureFlip.FlipHorizontal ? -1 : 1);
            this._instanceData[this._instances].TextureRectSize.Y     = (float)sourceRect.Height / textureGl.Height * (texFlip == TextureFlip.FlipVertical ? -1 : 1);

            this._instances++;
        }

        public void Draw(Texture textureGl, Vector2 position, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
            this.Draw(textureGl, position, Vector2.One, rotation, Color.White, flip, rotOrigin);
        }

        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
            this.Draw(textureGl, position, scale, rotation, Color.White, flip, rotOrigin);
        }

        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, Color colorOverride, float rotation = 0, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            this.Draw(textureGl, position, scale, rotation, colorOverride, texFlip, rotOrigin);
        }

        private readonly Texture[] _boundTextures;
        private          int       _usedTextures  = 0;


        private int GetTextureId(Texture tex) {
            if(this._usedTextures != 0)
                for (int i = 0; i < this._usedTextures; i++) {
                    Texture tex2 = this._boundTextures[i];

                    if (tex2 == null) break;
                    if (tex  == tex2) return i;
                }

            this._boundTextures[this._usedTextures] = tex;
            this._usedTextures++;

            return this._usedTextures - 1;
        }

        public const int NUM_INSTANCES = 1024;

        private          uint           _instances    = 0;
        private readonly InstanceData[] _instanceData = new InstanceData[NUM_INSTANCES];

        private unsafe void Flush() {
            if (this._instances == 0) return;

            for (int i = 0; i < this._usedTextures; i++) {
                TextureGL41 tex = this._boundTextures[i] as TextureGL41;

                tex.Bind(TextureUnit.Texture0 + i);
            }

            this._vao.Bind();
            this._backend.CheckError();

            // this._InstanceVBO.SetData<InstanceData>(this._instanceData);
            this._instanceVbo.Bind();
            fixed (void* ptr = this._instanceData)
                this._instanceVbo.SetSubData(ptr, (nuint)(this._instances * sizeof(InstanceData)));

            this.gl.DrawElementsInstanced<ushort>(PrimitiveType.TriangleStrip, 6, DrawElementsType.UnsignedShort, _indicies, this._instances);

            this._instances    = 0;
            this._usedTextures = 0;
        }

        public void End() {
            this.Flush();
            this.IsBegun = false;
        }

        #region text

        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation = 0, Vector2? scale = null) {
            this.DrawString(font, text, position, color, rotation, scale, default);
        }
        
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
        #endregion
    }
}
