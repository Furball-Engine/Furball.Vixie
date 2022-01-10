using System;
using System.Drawing;
using System.Numerics;
using FontStashSharp;
using Furball.Vixie.FontStashSharp;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Graphics.Renderers.OpenGL {
    /// <summary>
    /// Renderer which draws in an immediatefashion.
    /// </summary>
    public class ImmediateRenderer : IDisposable, ITextureRenderer, ITextRenderer {
        /// <summary>
        /// OpenGL API, used to shorten code
        /// </summary>
        private GL gl;

        /// <summary>
        /// Vertex Array Object which holds the Vertex Buffer and layout information
        /// </summary>
        private VertexArrayObject _vertexArray;
        /// <summary>
        /// Vertex Buffer which holds the temporary Verticies
        /// </summary>
        private BufferObject      _vertexBuffer;
        /// <summary>
        /// Index Buffer which holds enough verticies to draw 1 quad
        /// </summary>
        private BufferObject      _indexBuffer;
        /// <summary>
        /// Shader used to draw the immediateelements
        /// </summary>
        private Shader _shader;
        /// <summary>
        /// Indicates whether or not the Renderer is running
        /// </summary>
        public bool IsBegun { get; set; }

        /// <summary>
        /// FontStashSharp Renderer
        /// </summary>
        private VixieFontStashRenderer _textRenderer;

        /// <summary>
        /// Renderer which draws in an Immediate fashion.
        /// </summary>
        public ImmediateRenderer() {
            this.gl = Global.Gl;

            //Create Vertex Buffer
            this._vertexBuffer = new BufferObject(64, BufferTargetARB.ArrayBuffer);

            //Define Verticies to draw a single quad
            uint[] indicies = new uint[] {
                0, 1, 2,
                2, 3, 0
            };

            //Create Index Buffer and stick the indicies there
            this._indexBuffer = BufferObject.CreateNew<uint>(indicies, BufferTargetARB.ElementArrayBuffer);

            //Load Shader Sources
            string vertSource = ResourceHelpers.GetStringResource("ShaderCode/ImmediateRenderer/VertexShader.glsl");
            string fragSource = ResourceHelpers.GetStringResource("ShaderCode/ImmediateRenderer/PixelShader.glsl");

            //Create Attach Vertex and Fragment Shader, Compile and Link
            this._shader =
                new Shader()
                    .AttachShader(ShaderType.VertexShader, vertSource)
                    .AttachShader(ShaderType.FragmentShader, fragSource)
                    .Link();

            //Define Vertex Buffer layout
            VertexBufferLayout layout =
                new VertexBufferLayout()
                    .AddElement<float>(2)  //Position
                    .AddElement<float>(2)  //Texture Coordinates
                    .AddElement<float>(4); //Color Override

            //Create Vertex Array
            this._vertexArray = new VertexArrayObject();

            //Put the Vertex Buffer and the layout information in it
            this._vertexArray
                .Bind()
                .AddBuffer(this._vertexBuffer, layout);

            //Use the Default immediateRenderer Shader
            this.ChangeShader(this._shader);

            this._textRenderer = new VixieFontStashRenderer(this);
        }

        /// <summary>
        /// Simple Drawing Method for drawing with a vertex, index buffer and shader
        /// </summary>
        /// <param name="vertexBuffer">Vertex Buffer which holds the Verticies</param>
        /// <param name="indexBuffer">Index Buffer which holds the indicies</param>
        /// <param name="shader">Shader to draw with</param>
        public unsafe void Draw(BufferObject vertexBuffer, BufferObject indexBuffer, Shader shader) {
            vertexBuffer.Bind();
            indexBuffer.Bind();
            shader.Bind()
                  .SetUniform("u_ModifierX", UniformType.GlFloat, Global.GameInstance.WindowManager.PositionMultiplier.X)
                  .SetUniform("u_ModifierY", UniformType.GlFloat, Global.GameInstance.WindowManager.PositionMultiplier.Y)
                  //vx_WindowProjectionMatrix is a uniform provided by Vixie which can optionally be used to scale things into the window
                  .SetUniform("vx_WindowProjectionMatrix", UniformType.GlMat4F, Global.GameInstance.WindowManager.ProjectionMatrix);

            this.gl.DrawElements(PrimitiveType.Triangles, indexBuffer.DataCount, DrawElementsType.UnsignedInt, null);
        }
        /// <summary>
        /// Initializes Shaders and everything else, do this before drawing
        /// </summary>
        public void Begin() {
            this._vertexBuffer.Bind();
            this._indexBuffer.Bind();
            this._vertexArray.Bind();
            this._indexBuffer.Bind();
            this._currentShader.Bind();

            this.IsBegun = true;
        }
        /// <summary>
        /// Unlocks all the Shaders and Buffers
        /// </summary>
        public void End() {
            this._vertexBuffer.Unlock();
            this._indexBuffer.Unlock();
            this._vertexArray.Unlock();
            this._indexBuffer.Unlock();
            this._currentShader.Unlock();

            this.IsBegun = false;
        }
        /// <summary>
        /// Stores the currently in use Shader
        /// </summary>
        private Shader _currentShader;
        /// <summary>
        /// Used to change shader whenever necessary
        /// </summary>
        /// <param name="shader">New Shader</param>
        public void ChangeShader(Shader shader) {
            this._currentShader?.Unlock();

            this._currentShader = shader;
            //Bind and give it the Window Projection Matrix
            this._currentShader
                .LockingBind()
                .SetUniform("vx_WindowProjectionMatrix", UniformType.GlMat4F, Global.GameInstance.WindowManager.ProjectionMatrix);
        }
        /// <summary>
        /// Temporary Buffer for the Verticies
        /// </summary>
        private float[] _verticies;
        /// <summary>
        /// Here to not redefine the Variable, possibly speeding stuff up
        /// </summary>
        private Matrix4x4 _rotationMatrix;
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
            //Disallow calling Draw without calling Begin first
            if (!IsBegun)
                throw new Exception("Cannot call Draw before Calling Begin in BatchRenderer!");
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

            Vector2 topLeft = Vector2.Zero;
            Vector2 botRight = Vector2.Zero;

            //Apply Texture Flipping
            switch (texFlip) {
                default:
                case TextureFlip.None:
                    topLeft  = new Vector2(sourceRect.Value.X * (1.0f / texture.Size.X), (sourceRect.Value.Y + sourceRect.Value.Height) * (1.0f / texture.Size.Y));
                    botRight = new Vector2((sourceRect.Value.X + sourceRect.Value.Width)  * (1.0f / texture.Size.X), sourceRect.Value.Y * (1.0f / texture.Size.Y));
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

            //Initialize Rotation Matrix
            _rotationMatrix = Matrix4x4.CreateRotationZ(rotation, new Vector3(position.X, position.Y, 0));

            this._verticies = new float[] {
                /* Vertex Coordinates */  position.X,                position.Y + size.Value.Y,  /* Texture Coordinates */  topLeft.X,  botRight.Y,  /* Color */  colorOverride.Value.R, colorOverride.Value.G, colorOverride.Value.B, colorOverride.Value.A, //Bottom Left corner
                /* Vertex Coordinates */  position.X + size.Value.X, position.Y + size.Value.Y,  /* Texture Coordinates */  botRight.X, botRight.Y,  /* Color */  colorOverride.Value.R, colorOverride.Value.G, colorOverride.Value.B, colorOverride.Value.A, //Bottom Right corner
                /* Vertex Coordinates */  position.X + size.Value.X, position.Y,                 /* Texture Coordinates */  botRight.X, topLeft.Y,   /* Color */  colorOverride.Value.R, colorOverride.Value.G, colorOverride.Value.B, colorOverride.Value.A, //Top Right Corner
                /* Vertex Coordinates */  position.X,                position.Y,                 /* Texture Coordinates */  topLeft.X,  topLeft.Y,   /* Color */  colorOverride.Value.R, colorOverride.Value.G, colorOverride.Value.B, colorOverride.Value.A, //Top Left Corner
            };

            //Upload Data
            this._vertexBuffer.SetData<float>(this._verticies);
            this._currentShader.SetUniform("u_RotationMatrix", UniformType.GlMat4F, _rotationMatrix).SetUniform("vx_WindowProjectionMatrix", UniformType.GlMat4F, Global.GameInstance.WindowManager.ProjectionMatrix);

            //Bind Texture
            texture.Bind();

            //Send Draw Call
            this.gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        }

        /// <summary>
        /// Draws Text to the Screen
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
        /// Draws Text to the Screen
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
        /// Draws Colorful text to the Screen
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

        public void Dispose() {
            try {
                //Unlock Shaders and other things
                if (this._currentShader.Locked)
                    this._currentShader.Unlock();
                if (this._shader.Locked)
                    this._shader.Unlock();
                if (this._vertexBuffer.Locked)
                    this._vertexBuffer.Unlock();
                if (this._vertexArray.Locked)
                    this._vertexArray.Unlock();
                if (this._indexBuffer.Locked)
                    this._indexBuffer.Unlock();

                this._vertexArray.Dispose();
                this._currentShader.Dispose();
                this._vertexBuffer.Dispose();
                this._shader.Dispose();
                this._indexBuffer.Dispose();
            }
            catch {

            }
        }
    }
}
