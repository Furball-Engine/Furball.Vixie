using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using FontStashSharp;
using Furball.Vixie.FontStashSharp;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGLES;

namespace Furball.Vixie.Graphics.Renderers.OpenGL {
    public class QuadRenderer : ITextureRenderer, ITextRenderer {
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
        
        private BufferObject      _vbo;
        private BufferObject      _instanceVbo;
        private VertexArrayObject _vao;

        private VixieFontStashRenderer _textRenderer;

        private Shader _shader;

        // ReSharper disable once InconsistentNaming
        private GL gl;
        
        public unsafe QuadRenderer() {
            OpenGLHelper.CheckThread();

            gl = Global.Gl;

            string vertSource = ResourceHelpers.GetStringResource("ShaderCode/InstancedRenderer/VertexShader.glsl");
            string fragSource = ResourceHelpers.GetStringResource("ShaderCode/InstancedRenderer/FragmentShader.glsl");

            _shader = new Shader();

            _shader.AttachShader(ShaderType.VertexShader,   vertSource);
            _shader.AttachShader(ShaderType.FragmentShader, fragSource);
            _shader.Link();

            _shader.Bind();
            
            OpenGLHelper.CheckError();

            for (int i = 0; i < Global.Device.MaxTextureImageUnits; i++) {
                _shader.BindUniformToTexUnit($"tex_{i}", i);
            }

            this._vao = new VertexArrayObject();
            this._vao.Bind();

            this._vbo = new BufferObject(BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
            this._vbo.Bind();
            this._vbo.SetData<Vertex>(_vertices);
            
            //Vertex Position
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)0);
            //Texture position
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)sizeof(Vector2));
            OpenGLHelper.CheckError();
            
            gl.EnableVertexAttribArray(0);
            gl.EnableVertexAttribArray(1);
            OpenGLHelper.CheckError();

            this._instanceVbo = new BufferObject(BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
            this._instanceVbo.Bind();

            this._instanceVbo.SetData(null, (nuint)(sizeof(InstanceData) * NUM_INSTANCES));

            int ptrPos = 0;
            //Position
            gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            gl.VertexAttribDivisor(2, 1);
            ptrPos += sizeof(Vector2);
            //Size
            gl.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            gl.VertexAttribDivisor(3, 1);
            ptrPos += sizeof(Vector2);
            //Color
            gl.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            gl.VertexAttribDivisor(4, 1);
            ptrPos += sizeof(Color);
            //Texture position
            gl.VertexAttribPointer(5, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            gl.VertexAttribDivisor(5, 1);
            ptrPos += sizeof(Vector2);
            //Texture size
            gl.VertexAttribPointer(6, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            gl.VertexAttribDivisor(6, 1);
            ptrPos += sizeof(Vector2);
            //Rotation origin
            gl.VertexAttribPointer(7, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            gl.VertexAttribDivisor(7, 1);
            ptrPos += sizeof(Vector2);
            //Rotation
            gl.VertexAttribPointer(8, 1, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            gl.VertexAttribDivisor(8, 1);
            ptrPos += sizeof(float);
            //Texture id
            gl.VertexAttribIPointer(9, 1, VertexAttribIType.Int, (uint)sizeof(InstanceData), (void*)ptrPos);
            gl.VertexAttribDivisor(9, 1);
            ptrPos += sizeof(int);

            gl.EnableVertexAttribArray(2);
            gl.EnableVertexAttribArray(3);
            gl.EnableVertexAttribArray(4);
            gl.EnableVertexAttribArray(5);
            gl.EnableVertexAttribArray(6);
            gl.EnableVertexAttribArray(7);
            gl.EnableVertexAttribArray(8);
            gl.EnableVertexAttribArray(9);

            OpenGLHelper.CheckError();
            
            this._instanceVbo.Unbind();
            this._vao.Unbind();

            this._textRenderer = new VixieFontStashRenderer(this);
        }

        public void Dispose() {
            this._shader.Dispose();
            this._vao.Dispose();
            this._vbo.Dispose();
            this._instanceVbo.Dispose();
        }
        
        public bool IsBegun {
            get;
            set;
        }
        
        public void Begin() {
            this._shader.Bind();

            _shader.SetUniform("vx_ModifierX",              Global.GameInstance.WindowManager.PositionMultiplier.X)
                   .SetUniform("vx_ModifierY",              Global.GameInstance.WindowManager.PositionMultiplier.Y)
                   .SetUniform("vx_WindowProjectionMatrix", Global.GameInstance.WindowManager.ProjectionMatrix);
            
            this._instances = 0;
            this._usedTextures = 0;

            this.IsBegun = true;
        }

        public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            if (!IsBegun)
                throw new Exception("Begin() has not been called!");

            //Ignore calls with invalid textures
            if (texture == null)
                return;
            
            if (_instances >= NUM_INSTANCES || _usedTextures == Global.Device.MaxTextureImageUnits) {
                Flush();
            }

            this._instanceData[this._instances].Position              = position;
            this._instanceData[this._instances].Size                  = texture.Size * scale;
            this._instanceData[this._instances].Color                 = colorOverride;
            this._instanceData[this._instances].Rotation              = rotation;
            this._instanceData[this._instances].RotationOrigin        = rotOrigin;
            this._instanceData[this._instances].TextureId             = GetTextureId(texture);
            this._instanceData[this._instances].TextureRectPosition.X = (float)0 / texture.Width;
            this._instanceData[this._instances].TextureRectPosition.Y = (float)0 / texture.Height;
            this._instanceData[this._instances].TextureRectSize.X     = texture.Size.X / texture.Width;
            this._instanceData[this._instances].TextureRectSize.Y     = texture.Size.Y / texture.Height;

            this._instances++;
        }

        public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            if (!IsBegun)
                throw new Exception("Begin() has not been called!");

            //Ignore calls with invalid textures
            if (texture == null)
                return;

            if (_instances >= NUM_INSTANCES || _usedTextures == Global.Device.MaxTextureImageUnits) {
                Flush();
            }

            //Set Size to the Source Rectangle
            Vector2 size = new Vector2(sourceRect.Width, sourceRect.Height);

            //Apply Scale
            size *= scale;

            this._instanceData[this._instances].Position              = position;
            this._instanceData[this._instances].Size                  = size;
            this._instanceData[this._instances].Color                 = colorOverride;
            this._instanceData[this._instances].Rotation              = rotation;
            this._instanceData[this._instances].RotationOrigin        = rotOrigin;
            this._instanceData[this._instances].TextureId             = GetTextureId(texture);
            this._instanceData[this._instances].TextureRectPosition.X = (float)sourceRect.X      / texture.Width;
            this._instanceData[this._instances].TextureRectPosition.Y = (float)sourceRect.Y      / texture.Height;
            this._instanceData[this._instances].TextureRectSize.X     = (float)sourceRect.Width  / texture.Width;
            this._instanceData[this._instances].TextureRectSize.Y     = (float)sourceRect.Height / texture.Height;

            this._instances++;
        }

        public void Draw(Texture texture, Vector2 position, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
            Draw(texture, position, Vector2.One, rotation, Color.White, flip, rotOrigin);
        }

        public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
            Draw(texture, position, scale, rotation, Color.White, flip, rotOrigin);
        }

        public void Draw(Texture texture, Vector2 position, Vector2 scale, Color colorOverride, float rotation = 0, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            Draw(texture, position, scale, rotation, colorOverride, texFlip, rotOrigin);
        }

        private readonly Texture[] _boundTextures = new Texture[Global.Device.MaxTextureImageUnits];
        private          int       _usedTextures  = 0;
        
        
        private int GetTextureId(Texture tex) {
            if(_usedTextures != 0)
                for (int i = 0; i < _usedTextures; i++) {
                    Texture tex2 = _boundTextures[i];

                    if (tex2 == null) break;
                    if (tex  == tex2) return i;
                }

            _boundTextures[_usedTextures] = tex;
            _usedTextures++;
		
            return _usedTextures - 1;
        }
        
        public const int NUM_INSTANCES = 1024;

        private          uint           _instances    = 0;
        private readonly InstanceData[] _instanceData = new InstanceData[NUM_INSTANCES];
        
        private unsafe void Flush() {
            if (_instances == 0) return;
            
            for (int i = 0; i < _usedTextures; i++) {
                Texture tex = _boundTextures[i];
			
                tex.Bind(TextureUnit.Texture0 + i);
            }
            
            this._vao.Bind();
            OpenGLHelper.CheckError();

            // this._InstanceVBO.SetData<InstanceData>(this._instanceData);
            this._instanceVbo.Bind();
            fixed (void* ptr = this._instanceData)
                this._instanceVbo.SetSubData(ptr, (nuint)(this._instances * sizeof(InstanceData)));
            
            gl.DrawElementsInstanced<ushort>(PrimitiveType.TriangleStrip, 6, DrawElementsType.UnsignedShort, _indicies, this._instances);

            this._instances    = 0;
            this._usedTextures = 0;
        }
        
        public void End() {
            this.Flush();
            this.IsBegun = false;
        }

        #region text

        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation = 0, Vector2? scale = null) {
            DrawString(font, text, position, color, rotation, scale, default);
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
