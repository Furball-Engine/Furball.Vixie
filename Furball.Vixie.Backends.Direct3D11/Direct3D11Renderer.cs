using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using Silk.NET.Maths;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Furball.Vixie.Backends.Direct3D11; 

public class Direct3D11Renderer : IRenderer {
    private readonly Direct3D11Backend   _backend;
    private readonly ID3D11DeviceContext _deviceContext;
    private readonly ID3D11Device        _device;

    private readonly ID3D11VertexShader _vertexShader;
    private readonly ID3D11PixelShader  _pixelShader;
    
    private readonly ID3D11InputLayout _inputLayout;

    private readonly ID3D11Buffer _projectionMatrixBuffer;

    public unsafe Direct3D11Renderer(Direct3D11Backend backend) {
        this._backend       = backend;
        this._deviceContext = backend.GetDeviceContext();
        this._device        = backend.GetDevice();
        
        byte[] vertexShaderData = ResourceHelpers.GetByteResource("VertexShader.obj");
        byte[] pixelShaderData  = ResourceHelpers.GetByteResource("PixelShader.obj");
        
        //Safety checks for shader data
        Guard.EnsureNonNull(vertexShaderData);
        Guard.EnsureNonNull(pixelShaderData);
        Guard.Assert(vertexShaderData.Length != 0);
        Guard.Assert(pixelShaderData.Length != 0);
        
        //Create shaders
        this._vertexShader = this._device.CreateVertexShader(vertexShaderData);
        this._pixelShader  = this._device.CreatePixelShader(pixelShaderData);

        InputElementDescription[] inputLayoutDescription = {
            new("POSITION", 0, Format.R32G32_Float,
                (int)Marshal.OffsetOf<Vertex>(nameof (Vertex.Position)), 0,
                InputClassification.PerVertexData, 0),
            new("TEXCOORD", 0, Format.R32G32_Float,
                (int)Marshal.OffsetOf<Vertex>(nameof (Vertex.TextureCoordinate)), 0,
                InputClassification.PerVertexData, 0),
            new("COLOR", 0, Format.R32G32B32A32_Float,
                (int)Marshal.OffsetOf<Vertex>(nameof (Vertex.Color)), 0,
                InputClassification.PerVertexData, 0),
            new("TEXID2", 0, Format.R32_UInt,
                (int)Marshal.OffsetOf<Vertex>(nameof (Vertex.TexId)), 0,
                InputClassification.PerVertexData, 0),
            //Note: the reason we add `sizeof(int)` is because in the actual vertex definition it is a long, but HLSL
            //does not have a `long` type, so we just split it into 2 32bit integers
            new("TEXID", 0, Format.R32_UInt,
                (int)Marshal.OffsetOf<Vertex>(nameof (Vertex.TexId)) + sizeof(int), 0,
                InputClassification.PerVertexData, 0)
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
    
    public override void Begin() {
        throw new System.NotImplementedException();
    }
    
    public override void End() {
        throw new System.NotImplementedException();
    }
    
    public override MappedData Reserve(ushort vertexCount, uint indexCount) {
        throw new System.NotImplementedException();
    }
    
    public override long GetTextureId(VixieTexture tex) {
        throw new System.NotImplementedException();
    }
    
    public override unsafe void Draw() {
        this.UpdateProjectionMatrixBuffer();
        
        this._deviceContext.VSSetShader(this._vertexShader);
        this._deviceContext.VSSetConstantBuffer(0, this._projectionMatrixBuffer);
        
        this._deviceContext.PSSetShader(this._pixelShader);

        this._deviceContext.IASetInputLayout(this._inputLayout);
        this._deviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

        for (int i = 0; i < this._renderBuffers.Count; i++) {
            this._deviceContext.IASetVertexBuffer(0, buf.VertexBuffer, sizeof(Vertex));
            this._deviceContext.IASetIndexBuffer(buf.IndexBuffer, Format.R16_UInt, 0);

            //TODO: bind textures from render buffer
            
            this._deviceContext.DrawIndexed(buf.IndexCount, 0, 0);
        }
        
        this._deviceContext.VSSetShader(null);
        this._deviceContext.VSSetConstantBuffer(0, null);

        this._deviceContext.PSSetShader(null);
        
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