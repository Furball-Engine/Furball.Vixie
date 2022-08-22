using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Direct3D11.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.FontStashSharp;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using Silk.NET.Maths;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Furball.Vixie.Backends.Direct3D11; 

public class Direct3D11Renderer : Renderer {
    private readonly Direct3D11Backend   _backend;
    private readonly ID3D11DeviceContext _deviceContext;
    private readonly ID3D11Device        _device;

    private readonly ID3D11VertexShader _vertexShader;
    private readonly ID3D11PixelShader  _pixelShader;
    
    private readonly ID3D11InputLayout _inputLayout;

    private readonly ID3D11Buffer _projectionMatrixBuffer;

    private readonly Direct3D11BufferMapper _vtxMapper;
    private readonly Direct3D11BufferMapper _idxMapper;
    
    private ID3D11ShaderResourceView?[] _boundShaderViews;
    private ID3D11ShaderResourceView[]  _nullShaderViews;
    private VixieTextureD3D11?[]        _boundTextures;
    private int                         _usedTextures;
    
    private readonly ID3D11SamplerState _samplerState;

    private class RenderBuffer : IDisposable {
        public ID3D11Buffer? Vtx;
        public ID3D11Buffer? Idx;

        public int                         UsedTextures;
        public ID3D11ShaderResourceView[]? Textures; //TODO
        
        public uint IndexCount;

        private bool _isDisposed;
        public void Dispose() {
            if(this._isDisposed)
                return;

            this._isDisposed = true;

            this.Vtx?.Dispose();
            this.Idx?.Dispose();

            this.Textures = null;
        }

        ~RenderBuffer() {
            DisposeQueue.Enqueue(this);
        }
    }

    private List<RenderBuffer> _renderBuffers = new();

    private Queue<ID3D11Buffer> _vtxBufferQueue = new();
    private Queue<ID3D11Buffer> _idxBufferQueue = new();
    
    private const int QUAD_AMOUNT = 256;

    public unsafe Direct3D11Renderer(Direct3D11Backend backend) {
        this._backend       = backend;
        this._deviceContext = backend.GetDeviceContext();
        this._device        = backend.GetDevice();
        
        byte[] vertexShaderData = ResourceHelpers.GetByteResource("Shaders/VertexShader.obj");
        byte[] pixelShaderData  = ResourceHelpers.GetByteResource("Shaders/PixelShader.obj");
        
        //Safety checks for shader data
        Guard.EnsureNonNull(vertexShaderData, "vertexShaderData");
        Guard.EnsureNonNull(pixelShaderData, "pixelShaderData");
        Guard.Assert(vertexShaderData.Length != 0, "vertexShaderData.Length != 0");
        Guard.Assert(pixelShaderData.Length  != 0, "pixelShaderData.Length != 0");
        
        //Create shaders
        this._vertexShader = this._device.CreateVertexShader(vertexShaderData);
        this._pixelShader  = this._device.CreatePixelShader(pixelShaderData);

        InputElementDescription[] inputLayoutDescription = {
            new("POSITION", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0, InputClassification.PerVertexData, 0),
            new("TEXCOORD", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0, InputClassification.PerVertexData, 0),
            new("COLOR", 0, Format.R32G32B32A32_Float, InputElementDescription.AppendAligned, 0, InputClassification.PerVertexData, 0),
            new("TEXID", 0, Format.R32_UInt, InputElementDescription.AppendAligned, 0, InputClassification.PerVertexData, 0),
            //Note: the reason we add `sizeof(int)` is because in the actual vertex definition it is a long, but HLSL
            //does not have a `long` type, so we just split it into 2 32bit integers
            new("TEXID", 1, Format.R32_UInt, InputElementDescription.AppendAligned, 0, InputClassification.PerVertexData, 0)
        };

        //Create the input layout for the shader
        this._inputLayout = this._device.CreateInputLayout(inputLayoutDescription, vertexShaderData);

        BufferDescription projectionBufferDescription = new BufferDescription {
            BindFlags      = BindFlags.ConstantBuffer,
            ByteWidth      = sizeof(Matrix4x4),
            CPUAccessFlags = CpuAccessFlags.Write,
            Usage          = ResourceUsage.Dynamic
        };
        
        this._projectionMatrixBuffer = this._device.CreateBuffer(projectionBufferDescription);
        this.UpdateProjectionMatrixBuffer();

        this._vtxMapper = new Direct3D11BufferMapper(backend, (uint)(sizeof(Vertex) * 4 * QUAD_AMOUNT), BindFlags.VertexBuffer);
        this._idxMapper = new Direct3D11BufferMapper(backend, (uint)(sizeof(Vertex) * 6 * QUAD_AMOUNT), BindFlags.IndexBuffer);
        
        this._boundShaderViews = new ID3D11ShaderResourceView[backend.QueryMaxTextureUnits()];
        this._nullShaderViews  = new ID3D11ShaderResourceView[128];
        this._boundTextures    = new VixieTextureD3D11?[backend.QueryMaxTextureUnits()];
        
        for (int i = 0; i != backend.QueryMaxTextureUnits(); i++) {
            VixieTextureD3D11 vixieTexture = backend.GetPrivateWhitePixelTexture();
            
            this._boundShaderViews[i] = vixieTexture.TextureView;
            this._boundTextures[i]    = vixieTexture;
        }
        
        SamplerDescription samplerDescription = new() {
            Filter             = Filter.MinMagMipLinear,
            AddressU           = TextureAddressMode.Wrap,
            AddressV           = TextureAddressMode.Wrap,
            AddressW           = TextureAddressMode.Wrap,
            ComparisonFunction = ComparisonFunction.Never,
            MinLOD             = 0,
            MipLODBias         = 0,
            MaxLOD             = float.MaxValue
        };

        this._samplerState = this._device.CreateSamplerState(samplerDescription);

        this.FontRenderer = new VixieFontStashRenderer(backend, this);
    }

    private unsafe void UpdateProjectionMatrixBuffer() {
        //Map the data
        MappedSubresource map = this._deviceContext.Map(this._projectionMatrixBuffer, MapMode.WriteDiscard);

        //Get the projection matrix into a local var so we are able to use the & operator on it
        Matrix4x4 projMatrix = this._backend.GetProjectionMatrix();
        //Copy the projection matrix into the mapped buffer
        Buffer.MemoryCopy(&projMatrix, (void*)map.DataPointer, sizeof(Matrix4x4), sizeof(Matrix4x4));
        
        //Unmap the data
        this._deviceContext.Unmap(this._projectionMatrixBuffer);
    }
    
    private bool _isFirst = true;
    public override void Begin() {
        //Save all the buffers from the render queue
        this._renderBuffers.ForEach(x => {
            Guard.EnsureNonNull(x.Vtx, "x.Vtx");
            Guard.EnsureNonNull(x.Idx, "x.Idx");

            this._vtxBufferQueue.Enqueue(x.Vtx!);
            this._idxBufferQueue.Enqueue(x.Idx!);

            //We set these to null to ensure that they dont get disposed when `RenderBuffer.Dispose` is called by the
            //destructor
            x.Vtx = null;
            x.Idx = null;
        });
        //Clear the render buffer queue
        this._renderBuffers.Clear();
        
        if (this._vtxBufferQueue.Count > 0) {
            this._vtxMapper.ResetFromExistingBuffer(this._vtxBufferQueue.Dequeue());
            this._idxMapper.ResetFromExistingBuffer(this._idxBufferQueue.Dequeue());
        }
        else {
            Guard.Assert(this._isFirst);
            
            //These should be null, because this code path should only run on the *first* time these are mapped.
            //If it is not the first time these are mapped, then it *will* have a buffer to pull from, and will instead
            //use the `true` condition of the above if statement, while this should be covered by the `Guard.Assert()`
            //above, these `Guard` checks act as an extra barrier against shenanigans
            Guard.EnsureNull(this._vtxMapper.ResetFromFreshBuffer(), "this._vtxMapper.ResetFromFreshBuffer()");
            Guard.EnsureNull(this._idxMapper.ResetFromFreshBuffer(), "this._idxMapper.ResetFromFreshBuffer()");
            
            this._isFirst = false;
        }

        this._usedTextures = 0;
        this._indexCount   = 0;
        this._indexOffset  = 0;
    }
    
    public override void End() {
        this.DumpToBuffers();
        
        this._vtxMapper.Unmap();
        this._idxMapper.Unmap();
    }

    private void DumpToBuffers() {
        if (this._indexCount == 0)
            return;

        ID3D11Buffer? vtx;
        ID3D11Buffer? idx;
        if (this._vtxBufferQueue.Count > 0) {
            vtx = this._vtxMapper.ResetFromExistingBuffer(this._vtxBufferQueue.Dequeue());
            idx = this._idxMapper.ResetFromExistingBuffer(this._idxBufferQueue.Dequeue());
        }
        else {
            vtx = this._vtxMapper.ResetFromFreshBuffer();
            idx = this._idxMapper.ResetFromFreshBuffer();
        }
        
        Guard.Assert(vtx != null);
        Guard.Assert(idx != null);

        RenderBuffer buf;
        this._renderBuffers.Add(buf = new RenderBuffer {
            Vtx      = vtx!, Idx = idx!, IndexCount = this._indexCount, UsedTextures = this._usedTextures,
            Textures = new ID3D11ShaderResourceView[this._boundShaderViews.Length]
        });
        Array.Copy(this._boundShaderViews, buf.Textures, this._usedTextures);
        
        for (int i = 0; i < this._boundShaderViews.Length; i++) {
            if(this._boundTextures[i] != null && this._boundTextures[i] != this._backend.GetPrivateWhitePixelTexture())
                this._boundTextures[i]!.UsedId = -1;

            this._boundTextures[i]    = null;
            this._boundShaderViews[i] = null;
        }
        
        this._usedTextures = 0;
        this._indexOffset  = 0;
        this._indexCount   = 0;
    }
    
    private ushort _indexOffset;
    private uint   _indexCount;

    private int _reserveRecursionCount = 0;
    public override unsafe MappedData Reserve(ushort vertexCount, uint indexCount) {
        Guard.Assert(vertexCount != 0, "vertexCount != 0");
        Guard.Assert(indexCount  != 0, "indexCount != 0");
        
        Guard.Assert(vertexCount * sizeof(Vertex) < (int)this._vtxMapper.SizeInBytes, "vertexCount * sizeof(Vertex) < this._vtxMapper.SizeInBytes");
        Guard.Assert(indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes, "indexCount * sizeof(ushort) < (int)this._idxMapper.SizeInBytes");

        void* vtx = this._vtxMapper.Reserve((nuint)(vertexCount * sizeof(Vertex)));
        void* idx = this._idxMapper.Reserve(indexCount * sizeof(ushort));

        if (vtx == null || idx == null) {
            //We should *never* recurse multiple times in this function, if we do, that indicates that for some reason,
            //even after dumping to a buffer to draw, we still are unable to reserve memory.
            Guard.Assert(this._reserveRecursionCount == 0, "this._reserveRecursionCount == 0");
            
            this.DumpToBuffers();
            this._reserveRecursionCount++;
            return this.Reserve(vertexCount, indexCount);
        }

        this._indexOffset += vertexCount;
        this._indexCount  += indexCount;

        this._reserveRecursionCount = 0;
        return new MappedData((Vertex*)vtx, (ushort*)idx, vertexCount, indexCount, (uint)(this._indexOffset - vertexCount));
    }
    
    public override long GetTextureId(VixieTexture texOrig) {
        this._backend.CheckThread();

        Guard.EnsureNonNull(texOrig, "texOrig");
        
        VixieTextureD3D11 tex = (VixieTextureD3D11)texOrig;
        
        if(tex.UsedId != -1) return tex.UsedId;

        if(this._usedTextures != 0)
            for (int i = 0; i < this._usedTextures; i++) {
                ID3D11ShaderResourceView? tex2 = this._boundShaderViews[i];

                if (tex2            == null) break;
                if (tex.TextureView == tex2) return i;
            }

        if (this._usedTextures == this._boundShaderViews.Length - 1) {
            this.DumpToBuffers();
            return this.GetTextureId(tex);
        }

        this._boundShaderViews[this._usedTextures] = tex.TextureView;
        this._boundTextures[this._usedTextures]    = tex;

        tex.UsedId = this._usedTextures;

        this._usedTextures++;

        return this._usedTextures - 1;
    }
    
    public override unsafe void Draw() {
        this.UpdateProjectionMatrixBuffer();
        
        this._deviceContext.VSSetShader(this._vertexShader);
        this._deviceContext.VSSetConstantBuffer(0, this._projectionMatrixBuffer);
        
        this._deviceContext.PSSetShader(this._pixelShader);
        this._deviceContext.PSSetSampler(0, this._samplerState);
        
        this._deviceContext.IASetInputLayout(this._inputLayout);
        this._deviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

        for (int i = 0; i < this._renderBuffers.Count; i++) {
            RenderBuffer buf = this._renderBuffers[i];
            
            Guard.EnsureNonNull(buf.Vtx);
            Guard.EnsureNonNull(buf.Idx);
            
            this._deviceContext.IASetVertexBuffer(0, buf.Vtx!, sizeof(Vertex));
            this._deviceContext.IASetIndexBuffer(buf.Idx!, Format.R16_UInt, 0);
            
            this._deviceContext.PSSetShaderResources(0, buf.UsedTextures, buf.Textures!);

            this._deviceContext.DrawIndexed((int)buf.IndexCount, 0, 0);
        }
        
        this._deviceContext.VSSetShader(null);
        this._deviceContext.VSSetConstantBuffer(0, null);

        this._deviceContext.PSSetShader(null);
        this._deviceContext.PSSetShaderResources(0, 128, this._nullShaderViews);

        this._deviceContext.IASetInputLayout(null);
        this._deviceContext.IASetVertexBuffer(0, null, 0);
        this._deviceContext.IASetIndexBuffer(null, Format.R16_UInt, 0);
    }
    
    protected override void DisposeInternal() {
        this._inputLayout.Dispose();
        
        this._pixelShader.Dispose();
        this._vertexShader.Dispose();
        
        this._projectionMatrixBuffer.Dispose();
    }
}