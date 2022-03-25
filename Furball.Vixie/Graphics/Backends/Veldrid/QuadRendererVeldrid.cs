using System;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using FontStashSharp;
using Furball.Vixie.Graphics.Backends.OpenGL41.Abstractions;
using Furball.Vixie.Graphics.Backends.Veldrid.Abstractions;
using Furball.Vixie.Graphics.Renderers;
using Veldrid;
using Veldrid.SPIRV;
using Rectangle=System.Drawing.Rectangle;

namespace Furball.Vixie.Graphics.Backends.Veldrid {
    public class QuadRendererVeldrid : IQuadRenderer {

        private readonly VeldridBackend _backend;
        
        public bool IsBegun {
            get;
            set;
        }

        private Pipeline _pipeline;
        
        [StructLayout(LayoutKind.Sequential)]
        private struct InstanceData {
            public Vector2 InstancePosition;
            public Vector2 InstanceSize;
            public Color   InstanceColor;
            public Vector2 InstanceTextureRectPosition;
            public Vector2 InstanceTextureRectSize;
            public Vector2 InstanceRotationOrigin;
            public float   InstanceRotation;
            public int     InstanceTextureId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex {
            public Vector2 VertexPosition;
            public Vector2 TextureCoordinate;
        }

        private readonly DeviceBuffer _projectionBuffer;
        private readonly ResourceSet  _projectionBufferResourceSet;
        
        public unsafe QuadRendererVeldrid(VeldridBackend backend) {
            this._backend = backend;

            string vertexSource = Helpers.ResourceHelpers.GetStringResource("ShaderCode/Veldrid/VertexShader.glsl", true);
            string fragmentSource = Helpers.ResourceHelpers.GetStringResource("ShaderCode/Veldrid/FragmentShader.glsl", true);

            ShaderDescription vertexShaderDescription   = new ShaderDescription(ShaderStages.Vertex,   Encoding.UTF8.GetBytes(vertexSource),   "main");
            ShaderDescription fragmentShaderDescription = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentSource), "main");

            Shader[] shaders = this._backend.ResourceFactory.CreateFromSpirv(vertexShaderDescription, fragmentShaderDescription);

            VertexLayoutDescription vtxLayout = new VertexLayoutDescription(new[] {
                new VertexElementDescription("VertexPosition", VertexElementFormat.Float2, VertexElementSemantic.Position), 
                new VertexElementDescription("TextureCoordinate", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate)
            }) {
                InstanceStepRate = 0
            };

            VertexLayoutDescription instanceVtxLayout = new VertexLayoutDescription(new[] {
                new VertexElementDescription("InstancePosition",            VertexElementFormat.Float2, VertexElementSemantic.Position), 
                new VertexElementDescription("InstanceSize",                VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("InstanceColor",               VertexElementFormat.Float4, VertexElementSemantic.Color),
                new VertexElementDescription("InstanceTextureRectPosition", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("InstanceTextureRectSize",     VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("InstanceRotationOrigin",      VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("InstanceRotation",            VertexElementFormat.Float1, VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("InstanceTextureId",            VertexElementFormat.Int1, VertexElementSemantic.TextureCoordinate),
            }) {
                InstanceStepRate = 1
            };

            GraphicsPipelineDescription pipelineDescription = new() {
                ShaderSet = new ShaderSetDescription {
                    Shaders = shaders,
                    VertexLayouts = new[] {
                        vtxLayout, instanceVtxLayout
                    }
                },
                Outputs           = backend.GraphicsDevice.SwapchainFramebuffer.OutputDescription,
                BlendState        = BlendStateDescription.SingleAlphaBlend,
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                RasterizerState   = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, true)
            };

            this._pipeline = backend.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);

            BufferDescription projBufDesc = new BufferDescription((uint)sizeof(Matrix4x4), BufferUsage.UniformBuffer);
            this._projectionBuffer = this._backend.ResourceFactory.CreateBuffer(projBufDesc);

            ResourceSetDescription projBufResourceSetDesc = new() {
                BoundResources = new[] {
                    this._projectionBuffer
                },
                Layout = this._backend.ResourceFactory.CreateResourceLayout(new(new[] {
                    new ResourceLayoutElementDescription("ProjectionMatrix", ResourceKind.UniformBuffer, ShaderStages.Fragment)
                }))
            };
            this._projectionBufferResourceSet = this._backend.ResourceFactory.CreateResourceSet(projBufResourceSetDesc);

            this._boundTextures = new Texture[backend.QueryMaxTextureUnits()];
        }
        
        public void Begin() {
            this.IsBegun = true;

            //Update the UBO with the projection matrix
            this._backend.BackendCommandList.UpdateBuffer(this._projectionBuffer, 0, this._backend.ProjectionMatrix);
            this._backend.BackendCommandList.SetGraphicsResourceSet(0, this._projectionBufferResourceSet);
        }

        public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            if (!this.IsBegun)
                throw new Exception("Begin() has not been called!");

            //Ignore calls with invalid textures
            if (texture == null || texture is not TextureVeldrid)
                return;

            if (this._instances >= NUM_INSTANCES || this._usedTextures == this._backend.QueryMaxTextureUnits()) {
                this.Flush();
            }
            
            this._instanceData[this._instances].InstancePosition              = position;
            this._instanceData[this._instances].InstanceSize                  = texture.Size * scale;
            this._instanceData[this._instances].InstanceColor                 = colorOverride;
            this._instanceData[this._instances].InstanceRotation              = rotation;
            this._instanceData[this._instances].InstanceRotationOrigin        = rotOrigin;
            this._instanceData[this._instances].InstanceTextureId             = this.GetTextureId(texture);
            this._instanceData[this._instances].InstanceTextureRectPosition.X = 0;
            this._instanceData[this._instances].InstanceTextureRectPosition.Y = 0;
            this._instanceData[this._instances].InstanceTextureRectSize.X     = 1;
            this._instanceData[this._instances].InstanceTextureRectSize.Y     = 1;

            this._instances++;
        }
        
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

        private          int            _instances    = 0;
        private readonly InstanceData[] _instanceData = new InstanceData[NUM_INSTANCES];
        private readonly Texture[]      _boundTextures;
        private          int            _usedTextures = 0;

        public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            if (!this.IsBegun)
                throw new Exception("Begin() has not been called!");

            //Ignore calls with invalid textures
            if (texture == null || texture is not TextureVeldrid)
                return;

            if (this._instances >= NUM_INSTANCES || this._usedTextures == this._backend.QueryMaxTextureUnits()) {
                this.Flush();
            }

            //Set Size to the Source Rectangle
            Vector2 size = new Vector2(sourceRect.Width, sourceRect.Height);

            //Apply Scale
            size *= scale;
            
            sourceRect.Y = texture.Height - sourceRect.Y - sourceRect.Height;

            this._instanceData[this._instances].InstancePosition              = position;
            this._instanceData[this._instances].InstanceSize                  = size;
            this._instanceData[this._instances].InstanceColor                 = colorOverride;
            this._instanceData[this._instances].InstanceRotation              = rotation;
            this._instanceData[this._instances].InstanceRotationOrigin        = rotOrigin;
            this._instanceData[this._instances].InstanceTextureId             = this.GetTextureId(texture);
            this._instanceData[this._instances].InstanceTextureRectPosition.X = (float)sourceRect.X      / texture.Width;
            this._instanceData[this._instances].InstanceTextureRectPosition.Y = (float)sourceRect.Y      / texture.Height;
            this._instanceData[this._instances].InstanceTextureRectSize.X     = (float)sourceRect.Width  / texture.Width;
            this._instanceData[this._instances].InstanceTextureRectSize.Y     = (float)sourceRect.Height / texture.Height;

            this._instances++;
        }
        
        private const int NUM_INSTANCES = 1024;

        private void Flush() {
            if (this._instances == 0) return;
        }

        public void End() {
            this.Flush();
            this.IsBegun = false;
        }

        private          bool        _isDisposed = false;
        public void Dispose() {
            if (this._isDisposed) return;
        }

        ~QuadRendererVeldrid() {
            DisposeQueue.Enqueue(this);
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

        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation = 0, Vector2? scale = null) {
            throw new System.NotImplementedException();
        }
        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color color, float rotation = 0, Vector2? scale = null) {
            throw new System.NotImplementedException();
        }
        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color[] colors, float rotation = 0, Vector2? scale = null) {
            throw new System.NotImplementedException();
        }
    }
}
