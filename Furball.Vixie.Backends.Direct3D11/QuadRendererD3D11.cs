using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using FontStashSharp;
using Furball.Vixie.Backends.Direct3D11.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.FontStashSharp;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Color=Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie.Backends.Direct3D11 {
    public unsafe class QuadRendererD3D11 : IQuadRenderer {
        public bool IsBegun { get; set; }

        private Direct3D11Backend   _backend;
        private ID3D11DeviceContext _deviceContext;
        private ID3D11Device        _device;

        private ID3D11Buffer _vertexBuffer;
        private ID3D11Buffer _indexBuffer;
        private ID3D11Buffer _constantBuffer;

        private ID3D11InputLayout  _inputLayout;
        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader  _pixelShader;
        private ID3D11SamplerState _samplerState;

        [StructLayout(LayoutKind.Sequential)]
        struct VertexData {
            public Vector2 Position;
            public Vector2 TexCoord;
            public Vector2 Scale;
            public float   Rotation;
            public Vector4 Color;
            public Vector2 RotationOrigin;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ConstantBufferData {
            public Matrix4x4 ProjectionMatrix;
        }

        private VertexData[] _localVertexBuffer;
        private VertexData*  _vertexBufferPointer;
        private int          _currentVertex;

        private ConstantBufferData _constantBufferData;

        private VixieFontStashRenderer _textRenderer;

        public unsafe QuadRendererD3D11(Direct3D11Backend backend) {
            this._backend       = backend;
            this._deviceContext = backend.GetDeviceContext();
            this._device        = backend.GetDevice();

            string shaderSourceCode = ResourceHelpers.GetStringResource("Shaders/QuadRenderer/Shaders.hlsl");

            Compiler.Compile(shaderSourceCode, Array.Empty<ShaderMacro>(), null, "VS_Main", "VertexShader.hlsl", "vs_5_0", ShaderFlags.EnableStrictness, EffectFlags.None, out Blob vertexShaderBlob, out Blob vertexShaderErrorBlob);
            Compiler.Compile(shaderSourceCode, Array.Empty<ShaderMacro>(), null, "PS_Main", "PixelShader.hlsl", "ps_5_0", ShaderFlags.EnableStrictness, EffectFlags.None, out Blob pixelShaderBlob, out Blob pixelShaderErrorBlob);

            if (vertexShaderBlob == null || pixelShaderBlob == null)
                throw new Exception("Failed to Compile Shaders.");

            InputElementDescription[] elementDescription = new [] {
                new InputElementDescription("POSITION",  0, Format.R32G32_Float,       (int) Marshal.OffsetOf<VertexData>("Position"),       0),
                new InputElementDescription("TEXCOORD",  0, Format.R32G32_Float,       (int) Marshal.OffsetOf<VertexData>("TexCoord"),       0),
                new InputElementDescription("SCALE",     0, Format.R32G32_Float,       (int) Marshal.OffsetOf<VertexData>("Scale"),          0),
                new InputElementDescription("ROTATION",  0, Format.R32_Float,          (int) Marshal.OffsetOf<VertexData>("Rotation"),       0),
                new InputElementDescription("COLOR",     0, Format.R32G32B32A32_Float, (int) Marshal.OffsetOf<VertexData>("Color"),          0),
                new InputElementDescription("ROTORIGIN", 0, Format.R32G32_Float,       (int) Marshal.OffsetOf<VertexData>("RotationOrigin"), 0),
            };

            ID3D11InputLayout layout = this._device.CreateInputLayout(elementDescription, vertexShaderBlob);
            this._inputLayout = layout;

            int vertexBufferSize = sizeof(VertexData) * 4;

            BufferDescription vertexBufferDescription = new BufferDescription {
                BindFlags = BindFlags.VertexBuffer,
                ByteWidth = vertexBufferSize,
                Usage     = ResourceUsage.Default,
            };

            ID3D11Buffer vertexBuffer = this._device.CreateBuffer(vertexBufferDescription);
            this._vertexBuffer = vertexBuffer;

            BufferDescription indexBufferDescription = new BufferDescription {
                BindFlags = BindFlags.IndexBuffer,
                ByteWidth = sizeof(uint) * 6,
                Usage     = ResourceUsage.Default
            };

            ID3D11Buffer indexBuffer = this._device.CreateBuffer(indexBufferDescription);
            this._indexBuffer = indexBuffer;

            uint[] indicies = new uint[] {
                0, 1, 2,
                2, 3, 0
            };

            this._deviceContext.UpdateSubresource(indicies, indexBuffer);

            BufferDescription constantBufferDescription = new BufferDescription {
                BindFlags = BindFlags.ConstantBuffer,
                ByteWidth = sizeof(ConstantBufferData),
                Usage     = ResourceUsage.Default
            };

            ID3D11Buffer constantBuffer = this._device.CreateBuffer(constantBufferDescription);
            this._constantBuffer = constantBuffer;

            this._constantBufferData = new ConstantBufferData {
                ProjectionMatrix = backend.GetProjectionMatrix()
            };

            this._deviceContext.UpdateSubresource(this._constantBufferData, constantBuffer);

            this._vertexShader = this._device.CreateVertexShader(vertexShaderBlob);
            this._pixelShader  = this._device.CreatePixelShader(pixelShaderBlob);

            SamplerDescription samplerDescription = new SamplerDescription {
                Filter             = Filter.MinMagMipLinear,
                AddressU           = TextureAddressMode.Wrap,
                AddressV           = TextureAddressMode.Wrap,
                AddressW           = TextureAddressMode.Wrap,
                ComparisonFunction = ComparisonFunction.Never
            };

            this._samplerState = this._device.CreateSamplerState(samplerDescription);

            this._textRenderer = new VixieFontStashRenderer(this._backend, this);
        }

        public void Begin() {
            this.IsBegun            = true;
            this._localVertexBuffer = new VertexData[4];

            fixed (VertexData* ptr = this._localVertexBuffer)
                this._vertexBufferPointer = ptr;

            this._constantBufferData.ProjectionMatrix = this._backend.GetProjectionMatrix();

            this._deviceContext.UpdateSubresource(this._constantBufferData, this._constantBuffer);
        }

        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, float rotation, Color colorOverride, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            fixed (VertexData* ptr = this._localVertexBuffer)
                this._vertexBufferPointer = ptr;

            if (textureGl is not TextureD3D11 texture)
                return;

            Vector2 size = textureGl.Size * scale;

            Vector2 topLeftUv = Vector2.Zero;
            Vector2 bottomRightUv = Vector2.Zero;

            switch (texFlip) {
                case TextureFlip.None:
                    topLeftUv     = new Vector2(0, 0);
                    bottomRightUv = new Vector2(1, 1);
                    break;
                case TextureFlip.FlipVertical:
                    topLeftUv     = new Vector2(0, 1);
                    bottomRightUv = new Vector2(1, 0);
                    break;
                case TextureFlip.FlipHorizontal:
                    topLeftUv     = new Vector2(1, 0);
                    bottomRightUv = new Vector2(0, 1);
                    break;
            }

            this._vertexBufferPointer->Position       = position;
            this._vertexBufferPointer->Color          = new Vector4(colorOverride.Rf, colorOverride.Gf, colorOverride.Bf, colorOverride.Af);
            this._vertexBufferPointer->TexCoord       = topLeftUv;
            this._vertexBufferPointer->Rotation       = rotation;
            this._vertexBufferPointer->Scale          = scale;
            this._vertexBufferPointer->RotationOrigin = position + rotOrigin;
            this._vertexBufferPointer++;

            this._vertexBufferPointer->Position       = new Vector2(position.X, position.Y + size.Y);
            this._vertexBufferPointer->TexCoord       = new Vector2(topLeftUv.X, bottomRightUv.Y);
            this._vertexBufferPointer->Color          = new Vector4(colorOverride.Rf, colorOverride.Gf, colorOverride.Bf, colorOverride.Af);
            this._vertexBufferPointer->Rotation       = rotation;
            this._vertexBufferPointer->Scale          = scale;
            this._vertexBufferPointer->RotationOrigin = position + rotOrigin;
            this._vertexBufferPointer++;

            this._vertexBufferPointer->Position       = position + size;
            this._vertexBufferPointer->TexCoord       = bottomRightUv;
            this._vertexBufferPointer->Color          = new Vector4(colorOverride.Rf, colorOverride.Gf, colorOverride.Bf, colorOverride.Af);
            this._vertexBufferPointer->Rotation       = rotation;
            this._vertexBufferPointer->Scale          = scale;
            this._vertexBufferPointer->RotationOrigin = position + rotOrigin;
            this._vertexBufferPointer++;

            this._vertexBufferPointer->Position       = new Vector2(position.X + size.X, position.Y);
            this._vertexBufferPointer->TexCoord       = new Vector2(bottomRightUv.X, topLeftUv.Y);
            this._vertexBufferPointer->Color          = new Vector4(colorOverride.Rf, colorOverride.Gf, colorOverride.Bf, colorOverride.Af);
            this._vertexBufferPointer->Rotation       = rotation;
            this._vertexBufferPointer->Scale          = scale;
            this._vertexBufferPointer->RotationOrigin = position + rotOrigin;
            this._vertexBufferPointer++;

            this._deviceContext.UpdateSubresource(this._localVertexBuffer, this._vertexBuffer);

            this._deviceContext.IASetInputLayout(this._inputLayout);
            this._deviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

            this._deviceContext.IASetVertexBuffer(0, this._vertexBuffer, sizeof(VertexData));
            this._deviceContext.IASetIndexBuffer(this._indexBuffer, Format.R32_UInt, 0);

            this._deviceContext.VSSetShader(this._vertexShader);
            this._deviceContext.VSSetConstantBuffer(0, this._constantBuffer);


            this._deviceContext.PSSetShader(this._pixelShader);
            this._deviceContext.PSSetSampler(0, this._samplerState);

            texture.BindToPixelShader(0);

            this._deviceContext.DrawIndexed(6, 0, 0);
        }
        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            fixed (VertexData* ptr = this._localVertexBuffer)
                this._vertexBufferPointer = ptr;

            if (textureGl is not TextureD3D11 texture)
                return;

            Vector2 size = new Vector2(sourceRect.Width, sourceRect.Height) * scale;

            Vector2 topLeftUv = Vector2.Zero;
            Vector2 bottomRightUv = Vector2.Zero;

            switch (texFlip) {
                case TextureFlip.None:
                    topLeftUv     = new Vector2(sourceRect.X                      * (1.0f / textureGl.Width), 1 - (sourceRect.Y + sourceRect.Height) * (1.0f / textureGl.Height));
                    bottomRightUv = new Vector2((sourceRect.X + sourceRect.Width) * (1.0f / textureGl.Width), 1 - sourceRect.Y                       * (1.0f / textureGl.Height));
                    break;
                case TextureFlip.FlipVertical:
                    topLeftUv     = new Vector2(sourceRect.X                      * (1.0f / textureGl.Width), 1 - sourceRect.Y                       * (1.0f / textureGl.Height));
                    bottomRightUv = new Vector2((sourceRect.X + sourceRect.Width) * (1.0f / textureGl.Width), 1 - (sourceRect.Y + sourceRect.Height) * (1.0f / textureGl.Height));
                    break;
                case TextureFlip.FlipHorizontal:
                    topLeftUv     = new Vector2((sourceRect.X + sourceRect.Width) * (1.0f / textureGl.Width), 1 - (sourceRect.Y + sourceRect.Height) * (1.0f / textureGl.Height));
                    bottomRightUv = new Vector2(sourceRect.X                      * (1.0f / textureGl.Width), 1 - sourceRect.Y                       * (1.0f / textureGl.Height));
                    break;
            }

            this._vertexBufferPointer->Position       = position - rotOrigin;
            this._vertexBufferPointer->Color          = new Vector4(colorOverride.Rf, colorOverride.Gf, colorOverride.Bf, colorOverride.Af);
            this._vertexBufferPointer->TexCoord       = topLeftUv;
            this._vertexBufferPointer->Rotation       = rotation;
            this._vertexBufferPointer->Scale          = scale;
            this._vertexBufferPointer->RotationOrigin = position + rotOrigin;
            this._vertexBufferPointer++;

            this._vertexBufferPointer->Position       = new Vector2(position.X, position.Y + size.Y) - rotOrigin;
            this._vertexBufferPointer->TexCoord       = new Vector2(topLeftUv.X, bottomRightUv.Y);
            this._vertexBufferPointer->Color          = new Vector4(colorOverride.Rf, colorOverride.Gf, colorOverride.Bf, colorOverride.Af);
            this._vertexBufferPointer->Rotation       = rotation;
            this._vertexBufferPointer->Scale          = scale;
            this._vertexBufferPointer->RotationOrigin = position + rotOrigin;
            this._vertexBufferPointer++;

            this._vertexBufferPointer->Position       = (position + size) - rotOrigin;
            this._vertexBufferPointer->TexCoord       = bottomRightUv;
            this._vertexBufferPointer->Color          = new Vector4(colorOverride.Rf, colorOverride.Gf, colorOverride.Bf, colorOverride.Af);
            this._vertexBufferPointer->Rotation       = rotation;
            this._vertexBufferPointer->Scale          = scale;
            this._vertexBufferPointer->RotationOrigin = position + rotOrigin;
            this._vertexBufferPointer++;

            this._vertexBufferPointer->Position       = new Vector2(position.X + size.X, position.Y) - rotOrigin;
            this._vertexBufferPointer->TexCoord       = new Vector2(bottomRightUv.X, topLeftUv.Y);
            this._vertexBufferPointer->Color          = new Vector4(colorOverride.Rf, colorOverride.Gf, colorOverride.Bf, colorOverride.Af);
            this._vertexBufferPointer->Rotation       = rotation;
            this._vertexBufferPointer->Scale          = scale;
            this._vertexBufferPointer->RotationOrigin = position + rotOrigin;
            this._vertexBufferPointer++;

            this._deviceContext.UpdateSubresource(this._localVertexBuffer, this._vertexBuffer);

            this._deviceContext.IASetInputLayout(this._inputLayout);
            this._deviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

            this._deviceContext.IASetVertexBuffer(0, this._vertexBuffer, sizeof(VertexData));
            this._deviceContext.IASetIndexBuffer(this._indexBuffer, Format.R32_UInt, 0);

            this._deviceContext.VSSetShader(this._vertexShader);
            this._deviceContext.VSSetConstantBuffer(0, this._constantBuffer);


            this._deviceContext.PSSetShader(this._pixelShader);
            this._deviceContext.PSSetSampler(0, this._samplerState);

            texture.BindToPixelShader(0);

            this._deviceContext.DrawIndexed(6, 0, 0);
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
            //Default Scale
            if(scale == null || scale == Vector2.Zero)
                scale = Vector2.One;

            //Draw
            font.DrawText(this._textRenderer, text, position, System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B), scale.Value, rotation);
        }

        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color color, float rotation = 0, Vector2? scale = null) {
            if(scale == null || scale == Vector2.Zero)
                scale = Vector2.One;

            font.DrawText(this._textRenderer, text, position, color, scale.Value, rotation);
        }

        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color[] colors, float rotation = 0, Vector2? scale = null) {
            //Default Scale
            if(scale == null || scale == Vector2.Zero)
                scale = Vector2.One;

            //Draw
            font.DrawText(this._textRenderer, text, position, colors, scale.Value, rotation);
        }

        public void End() {

        }

        public void Dispose() {
            this._constantBuffer.Dispose();
            this._indexBuffer.Dispose();
            this._inputLayout.Dispose();
            this._pixelShader.Dispose();
            this._samplerState.Dispose();
            this._vertexBuffer.Dispose();
            this._vertexShader.Dispose();
        }
    }
}
