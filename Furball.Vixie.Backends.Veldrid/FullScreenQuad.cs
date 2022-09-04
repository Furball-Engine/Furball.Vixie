using System;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using Veldrid;
using Veldrid.SPIRV;

namespace Furball.Vixie.Backends.Veldrid; 

internal class FullScreenQuad {
    private readonly VeldridBackend _backend;

    private static ushort[] _sQuadIndices = new ushort[] {
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

        Shader[] shaders = this._backend.ResourceFactory.CreateFromSpirv(new ShaderDescription(ShaderStages.Vertex, ResourceHelpers.GetByteResource("Shaders/FullScreenQuad/VertexShader.glsl", typeof(VeldridBackend)), "main"), new ShaderDescription(ShaderStages.Fragment, ResourceHelpers.GetByteResource("Shaders/FullScreenQuad/FragmentShader.glsl", typeof(VeldridBackend)), "main"));

        GraphicsPipelineDescription pd = new(
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
        this._pipeline = factory.CreateGraphicsPipeline(ref pd);

        float[] verts = this.GetFullScreenQuadVerts();

        this._vb = factory.CreateBuffer(new BufferDescription(verts.SizeInBytes() * sizeof(float), BufferUsage.VertexBuffer));
        cl.UpdateBuffer(this._vb, 0, verts);

        this._ib = factory.CreateBuffer(new BufferDescription(_sQuadIndices.SizeInBytes(), BufferUsage.IndexBuffer));
        cl.UpdateBuffer(this._ib, 0, _sQuadIndices);
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
        this._backend.CommandList.SetPipeline(this._pipeline);
        this._backend.CommandList.SetGraphicsResourceSet(0, this._backend.MainFramebufferTextureSet);
        this._backend.CommandList.SetVertexBuffer(0, this._vb);
        this._backend.CommandList.SetIndexBuffer(this._ib, IndexFormat.UInt16);
        this._backend.CommandList.DrawIndexed(6, 1, 0, 0, 0);
    }
        
    private bool _isDisposed = false;
    public void Dispose() {
        if (this._isDisposed) return;
        this._isDisposed = true;
            
        DisposeQueue.Enqueue(this._pipeline);
        DisposeQueue.Enqueue(this._ib);
        DisposeQueue.Enqueue(this._vb);
        GC.SuppressFinalize(this);
    }
}