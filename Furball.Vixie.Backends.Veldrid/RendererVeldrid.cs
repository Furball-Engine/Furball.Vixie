using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Backends.Veldrid.Abstractions;
using Furball.Vixie.Helpers.Helpers;
using Veldrid;
using Veldrid.SPIRV;

namespace Furball.Vixie.Backends.Veldrid; 

public unsafe class RendererVeldrid : IRenderer {
    private readonly VeldridBackend _backend;
    
    private readonly VeldridBufferMapper _vtxMapper;
    private readonly VeldridBufferMapper _idxMapper;

    private readonly DeviceBuffer _projectionBuffer;
    private readonly ResourceSet  _projectionBufferResourceSet;
    private readonly Pipeline     _pipeline;
    
    private const int QUAD_COUNT = 256;

    private class BufferData {
        public DeviceBuffer Vtx;
        public DeviceBuffer Idx;

        public int                   UsedTextures;
        public VixieTextureVeldrid[] Textures;
        
        public uint IndexCount;
    }

    private List<BufferData> _buffers = new();
    
    public RendererVeldrid(VeldridBackend backend) {
        this._backend = backend;

        this._vtxMapper = new VeldridBufferMapper(backend, QUAD_COUNT * 4, BufferUsage.VertexBuffer);
        this._idxMapper = new VeldridBufferMapper(backend, QUAD_COUNT * 6, BufferUsage.IndexBuffer);

        string vertexSource   = ResourceHelpers.GetStringResource("Shaders/VertexShader.glsl");
        string fragmentSource = ResourceHelpers.GetStringResource("Shaders/FragmentShader.glsl");

        Shader[] shaders =
            backend.ResourceFactory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexSource), "main"),
                new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentSource), "main"));

        VertexLayoutDescription vtxLayout = new() {
            Elements = new[] {
                new VertexElementDescription("Position", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("TextureCoordinate", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("Color", VertexElementFormat.Float4, VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("TextureId2", VertexElementFormat.Int1, VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("TextureId", VertexElementFormat.Int1, VertexElementSemantic.TextureCoordinate),
            },
            Stride = (uint)sizeof(Vertex)
        };

        GraphicsPipelineDescription pipelineDesc = new() {
            ShaderSet = new ShaderSetDescription {
                Shaders = shaders,
                VertexLayouts = new[] {
                    vtxLayout
                }
            },
            Outputs           = backend.RenderFramebuffer.OutputDescription,
            BlendState        = BlendStateDescription.SingleAlphaBlend,
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            RasterizerState =
                new RasterizerStateDescription(FaceCullMode.Front, PolygonFillMode.Solid, FrontFace.Clockwise, false,
                                               true),
            DepthStencilState    = DepthStencilStateDescription.Disabled,
            ResourceBindingModel = ResourceBindingModel.Improved
        };
        #region create projection buffer

        BufferDescription projBufDesc = new((uint)sizeof(Matrix4x4), BufferUsage.UniformBuffer);
        this._projectionBuffer = this._backend.ResourceFactory.CreateBuffer(projBufDesc);
            
        ResourceSetDescription projectionBufferResourceLayout = new() {
            BoundResources = new[] {
                this._projectionBuffer
            },
            Layout = this._backend.ResourceFactory.CreateResourceLayout(new(new[] {
                new ResourceLayoutElementDescription("ProjectionMatrixUniform", ResourceKind.UniformBuffer, ShaderStages.Vertex)
            }))
        };
        this._projectionBufferResourceSet =
            this._backend.ResourceFactory.CreateResourceSet(projectionBufferResourceLayout);
        #endregion
        pipelineDesc.ResourceLayouts = new[] {
            projectionBufferResourceLayout.Layout,
            VixieTextureVeldrid.ResourceLayouts[0],
            VixieTextureVeldrid.ResourceLayouts[1],
            VixieTextureVeldrid.ResourceLayouts[2],
            VixieTextureVeldrid.ResourceLayouts[3]
        };

        this._pipeline = backend.ResourceFactory.CreateGraphicsPipeline(ref pipelineDesc);
    }
    
    public override void Begin() {
        this._buffers.Clear();
        
        this._vtxMapper.Map();
        this._idxMapper.Map();
    }
    
    public override void End() {
        this.DumpToBuffers();
        
        this._vtxMapper.Unmap();
        this._idxMapper.Unmap();
    }

    private int                    _usedTextures;
    private VixieTextureVeldrid?[] _textures = new VixieTextureVeldrid?[VeldridBackend.MAX_TEXTURE_UNITS];
    public override long GetTextureId(VixieTexture tex) {
        if (tex is not VixieTextureVeldrid texVeldrid)
            throw new InvalidOperationException();

        for (int i = 0; i < this._textures.Length; i++) {
            VixieTextureVeldrid? vixieTextureVeldrid = this._textures[i];
            if (vixieTextureVeldrid == tex)
                return i;
        }

        if (this._usedTextures == this._textures.Length - 1) {
            this.DumpToBuffers();
            return this.GetTextureId(tex);
        }

        this._textures[this._usedTextures] = texVeldrid;

        this._usedTextures++;

        return this._usedTextures - 1;
    }

    private void DumpToBuffers() {
        if (this._indexCount == 0)
            return;
        
        DeviceBuffer vtx = this._vtxMapper.Reset();
        DeviceBuffer idx = this._idxMapper.Reset();

        BufferData buf;
        this._buffers.Add(buf = new BufferData {
            Vtx      = vtx, Idx = idx, IndexCount = this._indexCount, UsedTextures = this._usedTextures,
            Textures = new VixieTextureVeldrid[this._textures.Length]
        });
        Array.Copy(this._textures, buf.Textures, this._usedTextures);
        
        for (int i = 0; i < this._textures.Length; i++) {
            this._textures[i] = null;
        }
        
        this._usedTextures = 0;
        this._indexOffset  = 0;
        this._indexCount   = 0;
    }
    
    private          ushort       _indexOffset;
    private          uint         _indexCount;
    public override MappedData Reserve(ushort vertexCount, uint indexCount) {
        Debug.Assert(vertexCount != 0, "vertexCount != 0");
        Debug.Assert(indexCount  != 0, "indexCount != 0");
        
        Debug.Assert(vertexCount * sizeof(Vertex) < (int)this._vtxMapper.SizeInBytes, "vertexCount * sizeof(Vertex) < this._vtxMapper.SizeInBytes");
        Debug.Assert(indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes, "indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes");

        void* vtx = this._vtxMapper.Reserve((nuint)(vertexCount * sizeof(Vertex)));
        void* idx = this._idxMapper.Reserve(indexCount * sizeof(ushort));

        if (vtx == null || idx == null) {
            this.DumpToBuffers();
            return this.Reserve(vertexCount, indexCount);
        }

        this._indexOffset += vertexCount;
        this._indexCount  += indexCount;

        return new MappedData((Vertex*)vtx, (ushort*)idx, vertexCount, indexCount, (uint)(this._indexOffset - vertexCount));
    }
    
    public override void Draw() {
        this._backend.CommandList.SetPipeline(this._pipeline);

        this._backend.CommandList.UpdateBuffer(this._projectionBuffer, 0, this._backend.ProjectionMatrix);
        this._backend.CommandList.SetGraphicsResourceSet(0, this._projectionBufferResourceSet);

        for (uint i = 0; i < VeldridBackend.MAX_TEXTURE_UNITS; i++) {
            this._backend.CommandList.SetGraphicsResourceSet(i + 1, this._backend.WhitePixelResourceSet);
        }
        
        for (int i = 0; i < this._buffers.Count; i++) {
            BufferData buf = this._buffers[i];
            this._backend.CommandList.InsertDebugMarker($"begin draw buf {i}");

            for (uint j = 0; j < buf.UsedTextures; j++) {
                this._backend.CommandList.SetGraphicsResourceSet(j + 1, buf.Textures[j].GetResourceSet(this._backend, (int)j));
            }
            
            this._backend.CommandList.SetVertexBuffer(0, buf.Vtx);
            this._backend.CommandList.SetIndexBuffer(buf.Idx, IndexFormat.UInt16);
            
            this._backend.CommandList.DrawIndexed(buf.IndexCount);
        }
    }
    protected override void DisposeInternal() {
        this._buffers.Clear();
    }
}