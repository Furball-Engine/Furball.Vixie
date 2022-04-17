using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Furball.Vixie.Backends.Direct3D11 {
    public class LineRendererD3D11 : ILineRenderer {
        public bool IsBegun { get; set; }

        private Direct3D11Backend   _backend;
        private ID3D11Device        _device;
        private ID3D11DeviceContext _deviceContext;

        [StructLayout(LayoutKind.Sequential)]
        private struct VertexData {
            public Vector2 Position;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct InstanceData {
            public Vector2 InstancePosition;
            public Vector2 InstanceSize;
            public Color   InstanceColor;
            public float   InstanceRotation;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ConstantBufferData {
            public Matrix4x4 ProjectionMatrix;
        }

        private const int VERTEX_BUFFER_SLOT = 0;
        private const int INSTANCE_BUFFER_SLOT = 1;

        private const int INSTANCE_AMOUNT = 8192;

        private ID3D11InputLayout  _inputLayout;
        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader  _pixelShader;
        private ID3D11Buffer       _vertexBuffer;
        private ID3D11Buffer       _indexBuffer;
        private ID3D11Buffer       _constantBuffer;
        private ID3D11Buffer       _instanceBuffer;

        private int            _instances;
        private InstanceData[] _instanceData;

        public unsafe LineRendererD3D11(Direct3D11Backend backend) {
            this._backend       = backend;
            this._device        = backend.GetDevice();
            this._deviceContext = backend.GetDeviceContext();

            string shaderSource = ResourceHelpers.GetStringResource("Shaders/LineRenderer/Shaders.hlsl");

            Compiler.Compile(shaderSource, Array.Empty<ShaderMacro>(), null, "VS_Main", "VertexShader.hlsl", "vs_5_0", ShaderFlags.EnableStrictness, EffectFlags.None, out Blob vertexShaderBlob, out Blob vertexShaderErrorBlob);
            Compiler.Compile(shaderSource, Array.Empty<ShaderMacro>(), null, "PS_Main", "PixelShader.hlsl", "ps_5_0", ShaderFlags.EnableStrictness, EffectFlags.None, out Blob pixelShaderBlob, out Blob pixelShaderErrorBlob);

            if (vertexShaderErrorBlob != null)
                throw new Exception("LineRendererD3D11 Vertex Shader failed to compile! Error Log:\n" + Encoding.UTF8.GetString(vertexShaderErrorBlob.AsBytes()));

            if (pixelShaderErrorBlob != null)
                throw new Exception("LineRendererD3D11 Pixel Shader failed to compile! Error Log:\n" + Encoding.UTF8.GetString(pixelShaderErrorBlob.AsBytes()));

            InputElementDescription[] inputLayoutDescription = new InputElementDescription[] {
                new InputElementDescription("POSITION",          0, Format.R32G32_Float,       (int) Marshal.OffsetOf<VertexData>  ("Position"),         VERTEX_BUFFER_SLOT,   InputClassification.PerVertexData,   0),
                new InputElementDescription("INSTANCE_POSITION", 0, Format.R32G32_Float,       (int) Marshal.OffsetOf<InstanceData>("InstancePosition"), INSTANCE_BUFFER_SLOT, InputClassification.PerInstanceData, 1),
                new InputElementDescription("INSTANCE_SIZE",     0, Format.R32G32_Float,       (int) Marshal.OffsetOf<InstanceData>("InstanceSize"),     INSTANCE_BUFFER_SLOT, InputClassification.PerInstanceData, 1),
                new InputElementDescription("INSTANCE_COLOR",    0, Format.R32G32B32A32_Float, (int) Marshal.OffsetOf<InstanceData>("InstanceColor"),    INSTANCE_BUFFER_SLOT, InputClassification.PerInstanceData, 1),
                new InputElementDescription("INSTANCE_ROTATION", 0, Format.R32_Float,          (int) Marshal.OffsetOf<InstanceData>("InstanceRotation"), INSTANCE_BUFFER_SLOT, InputClassification.PerInstanceData, 1),
            };

            ID3D11InputLayout inputLayout = this._device.CreateInputLayout(inputLayoutDescription, vertexShaderBlob);

            ID3D11VertexShader vertexShader = this._device.CreateVertexShader(vertexShaderBlob);
            ID3D11PixelShader pixelShader = this._device.CreatePixelShader(pixelShaderBlob);

            BufferDescription vertexBufferDescription = new BufferDescription {
                BindFlags           = BindFlags.VertexBuffer,
                ByteWidth           = sizeof(VertexData) * 4,
                CPUAccessFlags      = CpuAccessFlags.Write,
                MiscFlags           = ResourceOptionFlags.None,
                StructureByteStride = sizeof(VertexData),
                Usage               = ResourceUsage.Default
            };

            ID3D11Buffer vertexBuffer = this._device.CreateBuffer(vertexBufferDescription);

            BufferDescription instanceBufferDescritpion = new BufferDescription {
                BindFlags           = BindFlags.VertexBuffer,
                ByteWidth           = sizeof(InstanceData) * INSTANCE_AMOUNT,
                CPUAccessFlags      = CpuAccessFlags.Write,
                StructureByteStride = sizeof(InstanceData),
                Usage               = ResourceUsage.Dynamic
            };

            ID3D11Buffer instanceBuffer = this._device.CreateBuffer(instanceBufferDescritpion);

            BufferDescription indexBufferDescription = new BufferDescription {
                BindFlags = BindFlags.IndexBuffer,
                ByteWidth = sizeof(ushort) * 6,
                CPUAccessFlags = CpuAccessFlags.Write,
                Usage = ResourceUsage.Default
            };

            ID3D11Buffer indexBuffer = this._device.CreateBuffer(indexBufferDescription);

            BufferDescription constantBufferDescription = new BufferDescription {
                BindFlags = BindFlags.ConstantBuffer,
                ByteWidth = sizeof(ConstantBufferData),
                CPUAccessFlags = CpuAccessFlags.Write,
                Usage = ResourceUsage.Default
            };

            ID3D11Buffer constantBuffer = this._device.CreateBuffer(constantBufferDescription);

            this._inputLayout    = inputLayout;
            this._vertexShader   = vertexShader;
            this._pixelShader    = pixelShader;
            this._vertexBuffer   = vertexBuffer;
            this._indexBuffer    = indexBuffer;
            this._constantBuffer = constantBuffer;
            this._instanceBuffer = instanceBuffer;

            VertexData[] verticies = new [] {
                new VertexData { Position = new Vector2(0, 1) },
                new VertexData { Position = new Vector2(1, 1) },
                new VertexData { Position = new Vector2(1, 0) },
                new VertexData { Position = new Vector2(0, 0) },
            };

            ushort[] indicies = new ushort[] {
                0, 1, 2,
                2, 3, 0
            };

            ConstantBufferData constantBufferData = new ConstantBufferData {
                ProjectionMatrix = backend.GetProjectionMatrix()
            };

            this._deviceContext.UpdateSubresource(verticies, vertexBuffer);
            this._deviceContext.UpdateSubresource(indicies, indexBuffer);
            this._deviceContext.UpdateSubresource(constantBufferData, constantBuffer);

            this._instances    = 0;
            this._instanceData = new InstanceData[INSTANCE_AMOUNT];
        }

        public unsafe void Begin() {
            this.IsBegun = true;

            ConstantBufferData constantBufferData = new ConstantBufferData {
                ProjectionMatrix = _backend.GetProjectionMatrix()
            };

            this._deviceContext.UpdateSubresource(constantBufferData, this._constantBuffer);

            this._deviceContext.IASetInputLayout(this._inputLayout);
            this._deviceContext.IASetIndexBuffer(this._indexBuffer, Format.R16_UInt, 0);
            this._deviceContext.IASetVertexBuffer(VERTEX_BUFFER_SLOT, this._vertexBuffer, sizeof(VertexData));
            this._deviceContext.IASetVertexBuffer(INSTANCE_BUFFER_SLOT, this._instanceBuffer, sizeof(InstanceData));

            this._deviceContext.VSSetShader(this._vertexShader);
            this._deviceContext.PSSetShader(this._pixelShader);
        }

        public void Draw(Vector2 begin, Vector2 end, float thickness, Color color) {
            if (!this.IsBegun)
                throw new Exception("Begin() has not been called in LineRenderer!");

            if (this._instances >= INSTANCE_AMOUNT) {
                this.End();
                this.Begin();
            }

            this._instanceData[this._instances].InstancePosition = begin;
            this._instanceData[this._instances].InstanceSize.X   = (end - begin).Length();
            this._instanceData[this._instances].InstanceSize.Y   = thickness;
            this._instanceData[this._instances].InstanceColor    = color;
            this._instanceData[this._instances].InstanceRotation = (float)Math.Atan2(end.Y - begin.Y, end.X - begin.X);

            this._instances++;
        }

        public unsafe void End() {
            this.IsBegun = false;

            if (this._instances == 0)
                return;

            MappedSubresource instanceBufferMap = this._deviceContext.Map(this._instanceBuffer, MapMode.WriteDiscard);

            fixed (void* instanceData = this._instanceData) {
                long bytesToCopy = sizeof(InstanceData) * this._instances;

                Buffer.MemoryCopy(instanceData, (void*)instanceBufferMap.DataPointer, bytesToCopy, bytesToCopy);
            }

            this._deviceContext.Unmap(this._instanceBuffer, 0);

            this._deviceContext.DrawIndexedInstanced(6, this._instances, 0, 0, 0);
        }

        public void Dispose() {
            _inputLayout.Release();
            _vertexShader.Release();
            _pixelShader.Release();
            _vertexBuffer.Release();
            _indexBuffer.Release();
            _constantBuffer.Release();
            _instanceBuffer.Release();
        }
    }
}
