using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using FontStashSharp;
using Furball.Vixie.FontStashSharp;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGLES;

namespace Furball.Vixie.Graphics.Renderers.OpenGL {
    public class QuadRenderer : ITextureRenderer {
        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex {
            public Vector2 Position;
            public Vector2 TexturePosition;
        }
        
        private static Vertex[] _Vertices = {
            new() {
                Position = new(0, 0),
                TexturePosition = new(0, 1)
            },
            new() {
                Position = new(1, 0),
                TexturePosition = new(1, 1)
            },
            new() {
                Position = new(1, 1),
                TexturePosition = new(1, 0)
            },
            new() {
                Position = new(0, 1),
                TexturePosition = new(0, 0)
            }
        };
        private static ushort[] _Indicies = {
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
        
        private BufferObject      _VBO;
        private BufferObject      _InstanceVBO;
        private VertexArrayObject _VAO;

        private VixieFontStashRenderer _textRenderer;

        private Shader _shader;
        
        public unsafe QuadRenderer() {
            OpenGLHelper.CheckThread();

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

            _VAO = new VertexArrayObject();
            _VAO.Bind();

            _VBO = new(BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
            this._VBO.Bind();
            this._VBO.SetData<Vertex>(_Vertices);
            
            //Vertex Position
            Global.Gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)0);
            //Texture position
            Global.Gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)sizeof(Vector2));
            OpenGLHelper.CheckError();
            
            Global.Gl.EnableVertexAttribArray(0);
            Global.Gl.EnableVertexAttribArray(1);
            OpenGLHelper.CheckError();

            _InstanceVBO = new(BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
            _InstanceVBO.Bind();

            this._InstanceVBO.SetData(null, (nuint)(sizeof(InstanceData) * NUM_INSTANCES));

            int ptrPos = 0;
            //Position
            Global.Gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            Global.Gl.VertexAttribDivisor(2, 1);
            ptrPos += sizeof(Vector2);
            //Size
            Global.Gl.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            Global.Gl.VertexAttribDivisor(3, 1);
            ptrPos += sizeof(Vector2);
            //Color
            Global.Gl.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            Global.Gl.VertexAttribDivisor(4, 1);
            ptrPos += sizeof(Color);
            //Texture position
            Global.Gl.VertexAttribPointer(5, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            Global.Gl.VertexAttribDivisor(5, 1);
            ptrPos += sizeof(Vector2);
            //Texture size
            Global.Gl.VertexAttribPointer(6, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            Global.Gl.VertexAttribDivisor(6, 1);
            ptrPos += sizeof(Vector2);
            //Rotation origin
            Global.Gl.VertexAttribPointer(7, 2, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            Global.Gl.VertexAttribDivisor(7, 1);
            ptrPos += sizeof(Vector2);
            //Rotation
            Global.Gl.VertexAttribPointer(8, 1, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)ptrPos);
            Global.Gl.VertexAttribDivisor(8, 1);
            ptrPos += sizeof(float);
            //Texture id
            Global.Gl.VertexAttribIPointer(9, 1, VertexAttribIType.Int, (uint)sizeof(InstanceData), (void*)ptrPos);
            Global.Gl.VertexAttribDivisor(9, 1);
            ptrPos += sizeof(int);

            Global.Gl.EnableVertexAttribArray(2);
            Global.Gl.EnableVertexAttribArray(3);
            Global.Gl.EnableVertexAttribArray(4);
            Global.Gl.EnableVertexAttribArray(5);
            Global.Gl.EnableVertexAttribArray(6);
            Global.Gl.EnableVertexAttribArray(7);
            Global.Gl.EnableVertexAttribArray(8);
            Global.Gl.EnableVertexAttribArray(9);

            OpenGLHelper.CheckError();
            
            _InstanceVBO.Unbind();
            
            _VAO.Unbind();

            this._textRenderer = new VixieFontStashRenderer(this);
        }

        public void ChangeShader(Shader shader) {
            throw new NotImplementedException();
        }
        
        public void Dispose() {
            this._shader.Dispose();
            this._VAO.Dispose();
            this._VBO.Dispose();
            this._InstanceVBO.Dispose();
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
        
        public void Draw(Texture texture, Vector2 position, Vector2? size = null, Vector2? scale = null, float rotation = 0, Color? colorOverride = null, Rectangle? sourceRect = null, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            if (!IsBegun) throw new Exception("Begin() has not been called!");

            //Ignore calls with invalid textures
            if (texture == null) return;
            
            if (_instances >= NUM_INSTANCES || _usedTextures == Global.Device.MaxTextureImageUnits) {
                Flush();
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

            this._instanceData[this._instances].Position              = position;
            this._instanceData[this._instances].Size                  = size.Value;
            this._instanceData[this._instances].Color                 = colorOverride.Value;
            this._instanceData[this._instances].Rotation              = rotation;
            this._instanceData[this._instances].RotationOrigin        = rotOrigin;
            this._instanceData[this._instances].TextureId             = GetTextureId(texture);
            this._instanceData[this._instances].TextureRectPosition.X = (float)sourceRect.Value.X      / texture.Width;
            this._instanceData[this._instances].TextureRectPosition.Y = (float)sourceRect.Value.Y      / texture.Height;
            this._instanceData[this._instances].TextureRectSize.X     = (float)sourceRect.Value.Width  / texture.Width;
            this._instanceData[this._instances].TextureRectSize.Y     = (float)sourceRect.Value.Height / texture.Height;

            this._instances++;
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
            
            _VAO.Bind();
            OpenGLHelper.CheckError();

            // this._InstanceVBO.SetData<InstanceData>(this._instanceData);
            this._InstanceVBO.Bind();
            fixed (void* ptr = this._instanceData)
                this._InstanceVBO.SetSubData(ptr, (nuint)(this._instances * sizeof(InstanceData)));
            
            Global.Gl.DrawElementsInstanced<ushort>(PrimitiveType.TriangleStrip, 6, DrawElementsType.UnsignedShort, _Indicies, this._instances);

            this._instances    = 0;
            this._usedTextures = 0;
        }
        
        public void End() {
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
