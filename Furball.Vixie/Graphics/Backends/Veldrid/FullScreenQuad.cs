using System.Reflection.Metadata;
using System.Text;
using Furball.Vixie.Helpers;
using Veldrid;
using Veldrid.SPIRV;

namespace Furball.Vixie.Graphics.Backends.Veldrid {

    public class FullScreenQuad {
        private readonly VeldridBackend _backend;

        private static ushort[] s_quadIndices = new ushort[] {
            0, 1, 2, 0, 2, 3
        };
        
        private Pipeline     _pipeline;
        private DeviceBuffer _ib;
        private DeviceBuffer _vb;
        public  bool         UseTintedTexture { get; set; }

        public FullScreenQuad(VeldridBackend backend) {
            this._backend = backend;

            var factory = backend.ResourceFactory;
            var gd      = backend.GraphicsDevice;
            var cl      = backend.CommandList;
            
            ResourceLayout resourceLayout = backend.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment), new ResourceLayoutElementDescription("SourceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            Shader[] shaders = this._backend.ResourceFactory.CreateFromSpirv(new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(FullScreenQuadShaders.VertexShader), "main"), new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(FullScreenQuadShaders.FragmentShader), "main"));

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                new BlendStateDescription(RgbaFloat.Black, BlendAttachmentDescription.OverrideBlend),
                DepthStencilStateDescription.Disabled,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    new[] {
                        new VertexLayoutDescription(new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2), new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                    },
                    shaders),
                new ResourceLayout[] {
                    resourceLayout
                },
                gd.SwapchainFramebuffer.OutputDescription);
            _pipeline = factory.CreateGraphicsPipeline(ref pd);

            float[] verts = this.GetFullScreenQuadVerts();

            _vb = factory.CreateBuffer(new BufferDescription(verts.SizeInBytes() * sizeof(float), BufferUsage.VertexBuffer));
            cl.UpdateBuffer(_vb, 0, verts);

            _ib = factory.CreateBuffer(new BufferDescription(s_quadIndices.SizeInBytes(), BufferUsage.IndexBuffer));
            cl.UpdateBuffer(_ib, 0, s_quadIndices);
        }
        
        public float[] GetFullScreenQuadVerts()
        {
            if(this._backend.GraphicsDevice.IsUvOriginTopLeft)
                return new float[]
                {
                    -1,  1, 0, 0,
                     1,  1, 1, 0,
                     1, -1, 1, 1,
                    -1, -1, 0, 1
                };
            return new float[]
            {
                -1,  1, 0, 1,
                 1,  1, 1, 1,
                 1, -1, 1, 0,
                -1, -1, 0, 0
            };
        }

        ~FullScreenQuad() {
            this.Dispose();
        }
        
        public void Render()
        {
            this._backend.CommandList.SetPipeline(_pipeline);
            this._backend.CommandList.SetGraphicsResourceSet(0, _backend.MainFramebufferTextureSet);
            this._backend.CommandList.SetVertexBuffer(0, _vb);
            this._backend.CommandList.SetIndexBuffer(_ib, IndexFormat.UInt16);
            this._backend.CommandList.DrawIndexed(6, 1, 0, 0, 0);
        }
        
        private bool _isDisposed = false;
        public void Dispose() {
            if (this._isDisposed) return;
            this._isDisposed = true;
            
            this._pipeline.Dispose();
            this._ib.Dispose();
            this._vb.Dispose();
        }
    }
}
    