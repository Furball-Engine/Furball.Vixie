using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using FontStashSharp;
using Furball.Vixie.Backends.Direct3D11.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.FontStashSharp;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers.Helpers;
using Kettu;
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

        private ID3D11Buffer       _vertexBuffer;
        private ID3D11Buffer       _instanceBuffer;
        private ID3D11Buffer       _indexBuffer;
        private ID3D11Buffer       _constantBuffer;
        private ID3D11InputLayout  _inputLayout;
        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader  _pixelShader;
        private ID3D11SamplerState _samplerState;

        [StructLayout(LayoutKind.Sequential)]
        struct VertexData {
            public Vector2 Position;
            public Vector2 TexCoord;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct InstanceData {
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
        struct ConstantBufferData {
            public Matrix4x4 ProjectionMatrix;
        }

        private const int VERTEX_BUFFER_SLOT   = 0;
        private const int INSTANCE_BUFFER_SLOT = 1;

        private const int INSTANCE_AMOUNT = 16384;

        private int                        _instances;
        private InstanceData[]             _instanceData;
        private ID3D11ShaderResourceView[] _boundShaderViews;
        private TextureD3D11[]             _boundTextures;
        private int                        _usedTextures;

        private VixieFontStashRenderer _textRenderer;

        public QuadRendererD3D11(Direct3D11Backend backend) {
            this._backend       = backend;
            this._deviceContext = backend.GetDeviceContext();
            this._device        = backend.GetDevice();

            string shaderSourceCode = ResourceHelpers.GetStringResource("Shaders/QuadRenderer/Shaders.hlsl");

            Compiler.Compile(shaderSourceCode, Array.Empty<ShaderMacro>(), null, "VS_Main", "VertexShader.hlsl", "vs_5_0", ShaderFlags.EnableStrictness, EffectFlags.None, out Blob vertexShaderBlob, out Blob vertexShaderErrorBlob);
            Compiler.Compile(shaderSourceCode, Array.Empty<ShaderMacro>(), null, "PS_Main", "PixelShader.hlsl", "ps_5_0", ShaderFlags.EnableStrictness, EffectFlags.None, out Blob pixelShaderBlob, out Blob pixelShaderErrorBlob);

            if (vertexShaderBlob == null) {
                if (vertexShaderErrorBlob != null) {
                    throw new Exception("Failed to compile QuadRendererD3D11 Vertex Shader, Compilation Log:\n" + Encoding.UTF8.GetString(vertexShaderErrorBlob.AsBytes()));
                }
                throw new Exception("Failed to compile QuadRendererD3D11 Vertex Shader, Compilation Log missing...");
            }

            if (pixelShaderBlob == null) {
                if (pixelShaderErrorBlob != null) {
                    throw new Exception("Failed to compile QuadRendererD3D11 Pixel Shader, Compilation Log:\n" + Encoding.UTF8.GetString(pixelShaderErrorBlob.AsBytes()));
                }
                throw new Exception("Failed to compile QuadRendererD3D11 Pixel Shader, Compilation Log missing...");
            }

            ID3D11VertexShader vertexShader = this._device.CreateVertexShader(vertexShaderBlob);
            ID3D11PixelShader pixelShader = this._device.CreatePixelShader(pixelShaderBlob);

            InputElementDescription[] inputLayoutDescription = new InputElementDescription[] {
                new InputElementDescription("POSITION",                 0, Format.R32G32_Float,       (int) Marshal.OffsetOf<VertexData>("Position"),                      VERTEX_BUFFER_SLOT,   InputClassification.PerVertexData,   0),
                new InputElementDescription("TEXCOORD",                 0, Format.R32G32_Float,       (int) Marshal.OffsetOf<VertexData>("TexCoord"),                      VERTEX_BUFFER_SLOT,   InputClassification.PerVertexData,   0),
                new InputElementDescription("INSTANCE_POSITION",        0, Format.R32G32_Float,       (int) Marshal.OffsetOf<InstanceData>("InstancePosition"),            INSTANCE_BUFFER_SLOT, InputClassification.PerInstanceData, 1),
                new InputElementDescription("INSTANCE_SIZE",            0, Format.R32G32_Float,       (int) Marshal.OffsetOf<InstanceData>("InstanceSize"),                INSTANCE_BUFFER_SLOT, InputClassification.PerInstanceData, 1),
                new InputElementDescription("INSTANCE_COLOR",           0, Format.R32G32B32A32_Float, (int) Marshal.OffsetOf<InstanceData>("InstanceColor"),               INSTANCE_BUFFER_SLOT, InputClassification.PerInstanceData, 1),
                new InputElementDescription("INSTANCE_TEXRECTPOSITION", 0, Format.R32G32_Float,       (int) Marshal.OffsetOf<InstanceData>("InstanceTextureRectPosition"), INSTANCE_BUFFER_SLOT, InputClassification.PerInstanceData, 1),
                new InputElementDescription("INSTANCE_TEXRECTSIZE",     0, Format.R32G32_Float,       (int) Marshal.OffsetOf<InstanceData>("InstanceTextureRectSize"),     INSTANCE_BUFFER_SLOT, InputClassification.PerInstanceData, 1),
                new InputElementDescription("INSTANCE_ROTORIGIN",       0, Format.R32G32_Float,       (int) Marshal.OffsetOf<InstanceData>("InstanceRotationOrigin"),      INSTANCE_BUFFER_SLOT, InputClassification.PerInstanceData, 1),
                new InputElementDescription("INSTANCE_ROTATION",        0, Format.R32_Float,          (int) Marshal.OffsetOf<InstanceData>("InstanceRotation"),            INSTANCE_BUFFER_SLOT, InputClassification.PerInstanceData, 1),
                new InputElementDescription("INSTANCE_TEXID",           0, Format.R32G32_SInt,        (int) Marshal.OffsetOf<InstanceData>("InstanceTextureId"),           INSTANCE_BUFFER_SLOT, InputClassification.PerInstanceData, 1),
            };

            ID3D11InputLayout inputLayout = this._device.CreateInputLayout(inputLayoutDescription, vertexShaderBlob);

            BufferDescription constantBufferDescription = new BufferDescription {
                BindFlags = BindFlags.ConstantBuffer,
                ByteWidth = sizeof(ConstantBufferData),
                CPUAccessFlags = CpuAccessFlags.Write,
                Usage = ResourceUsage.Dynamic
            };

            ID3D11Buffer constantBuffer = this._device.CreateBuffer(constantBufferDescription);

            BufferDescription vertexBufferDescription = new BufferDescription {
                BindFlags = BindFlags.VertexBuffer,
                ByteWidth = sizeof(VertexData) * 4,
                CPUAccessFlags = CpuAccessFlags.Write,
                Usage = ResourceUsage.Dynamic
            };

            ID3D11Buffer vertexBuffer = this._device.CreateBuffer(vertexBufferDescription);

            BufferDescription instanceBufferDesription = new BufferDescription {
                BindFlags      = BindFlags.VertexBuffer,
                ByteWidth      = sizeof(InstanceData) * INSTANCE_AMOUNT,
                CPUAccessFlags = CpuAccessFlags.Write,
                Usage          = ResourceUsage.Dynamic
            };

            ID3D11Buffer instanceBuffer = this._device.CreateBuffer(instanceBufferDesription);

            BufferDescription indexBufferDescription = new BufferDescription {
                BindFlags = BindFlags.IndexBuffer,
                ByteWidth = sizeof(ushort) * 6,
                CPUAccessFlags = CpuAccessFlags.Write,
                Usage = ResourceUsage.Dynamic
            };

            ID3D11Buffer indexBuffer = this._device.CreateBuffer(indexBufferDescription);

            VertexData[] verticies = new [] {
                new VertexData { Position = new Vector2(0, 1), TexCoord = new Vector2(0, 1) },
                new VertexData { Position = new Vector2(1, 1), TexCoord = new Vector2(1, 1) },
                new VertexData { Position = new Vector2(1, 0), TexCoord = new Vector2(1, 0) },
                new VertexData { Position = new Vector2(0, 0), TexCoord = new Vector2(0, 0) },
            };

            ushort[] indicies = new ushort[] {
                0, 1, 2,
                2, 3, 0
            };

            ConstantBufferData[] constantBufferData = new[] {
                new ConstantBufferData { ProjectionMatrix = backend.GetProjectionMatrix() }
            };

            /* Copy Verticies */ {
                MappedSubresource vertexBufferResource = this._deviceContext.Map(vertexBuffer, MapMode.WriteDiscard);

                fixed (void* vertexPointer = verticies) {
                    long copySize = 4 * sizeof(VertexData);
                    Buffer.MemoryCopy(vertexPointer, (void*)vertexBufferResource.DataPointer, copySize, copySize);
                }

                this._deviceContext.Unmap(vertexBuffer);
            }

            /* Copy Indicies */ {
                MappedSubresource indexBufferResource = this._deviceContext.Map(indexBuffer, MapMode.WriteDiscard);

                fixed (void* indexPointer = indicies) {
                    long copySize = 6 * sizeof(ushort);
                    Buffer.MemoryCopy(indexPointer, (void*)indexBufferResource.DataPointer, copySize, copySize);
                }

                this._deviceContext.Unmap(indexBuffer);
            }

            /* Copy Constant Buffer */ {
                MappedSubresource constantBufferResource = this._deviceContext.Map(constantBuffer, MapMode.WriteDiscard);

                fixed (void* constantBufferPointer = constantBufferData) {
                    long copySize = sizeof(ConstantBufferData);
                    Buffer.MemoryCopy(constantBufferPointer, (void*)constantBufferResource.DataPointer, copySize, copySize);
                }

                this._deviceContext.Unmap(constantBuffer);
            }

            this._instances        = 0;
            this._instanceData     = new InstanceData[INSTANCE_AMOUNT];
            this._boundShaderViews = new ID3D11ShaderResourceView[backend.QueryMaxTextureUnits()];
            this._boundTextures    = new TextureD3D11[backend.QueryMaxTextureUnits()];

            for (int i = 0; i != backend.QueryMaxTextureUnits(); i++) {
                TextureD3D11 texture = backend.GetPrivateWhitePixelTexture();
                this._boundShaderViews[i] = texture.TextureView;
                this._boundTextures[i]    = texture;
            }

            this._usedTextures  = 0;

            this._inputLayout    = inputLayout;
            this._vertexShader   = vertexShader;
            this._pixelShader    = pixelShader;
            this._vertexBuffer   = vertexBuffer;
            this._constantBuffer = constantBuffer;
            this._instanceBuffer = instanceBuffer;
            this._indexBuffer    = indexBuffer;

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
            this.IsBegun  = true;
            
            ConstantBufferData[] constantBufferData = new[] {
                new ConstantBufferData { ProjectionMatrix = this._backend.GetProjectionMatrix() }
            };

            /* Copy Constant Buffer */ {
                MappedSubresource constantBufferResource = this._deviceContext.Map(this._constantBuffer, MapMode.WriteDiscard);

                fixed (void* constantBufferPointer = constantBufferData) {
                    long copySize = sizeof(ConstantBufferData);
                    Buffer.MemoryCopy(constantBufferPointer, (void*)constantBufferResource.DataPointer, copySize, copySize);
                }

                this._deviceContext.Unmap(_constantBuffer);
            }

            this._deviceContext.VSSetShader(this._vertexShader);
            this._deviceContext.VSSetConstantBuffer(0, this._constantBuffer);

            this._deviceContext.PSSetShader(this._pixelShader);
            this._deviceContext.PSSetSampler(0, this._samplerState);

            this._deviceContext.PSSetShaderResources(0, 128, this._boundShaderViews);

            this._deviceContext.IASetInputLayout(this._inputLayout);
            this._deviceContext.IASetVertexBuffer(VERTEX_BUFFER_SLOT, this._vertexBuffer, sizeof(VertexData));
            this._deviceContext.IASetVertexBuffer(INSTANCE_BUFFER_SLOT, this._instanceBuffer, sizeof(InstanceData));
            this._deviceContext.IASetIndexBuffer(this._indexBuffer, Format.R16_UInt, 0);
        }

        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, float rotation, Color colorOverride, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            if (!IsBegun)
                throw new Exception("Begin() has not been called in QuadRendererD3D11!");

            if (textureGl == null || textureGl is not TextureD3D11 texture)
                return;

            if (this._instances >= INSTANCE_AMOUNT || this._usedTextures == this._backend.QueryMaxTextureUnits()) {
                this.End();
                this.Begin();
            }

            this._instanceData[this._instances].InstancePosition              = position;
            this._instanceData[this._instances].InstanceSize                  = texture.Size * scale;
            this._instanceData[this._instances].InstanceColor                 = colorOverride;
            this._instanceData[this._instances].InstanceRotation              = rotation;
            this._instanceData[this._instances].InstanceRotationOrigin        = rotOrigin;
            this._instanceData[this._instances].InstanceTextureId             = this.GetTextureId(texture);
            this._instanceData[this._instances].InstanceTextureRectPosition.X = 0;
            this._instanceData[this._instances].InstanceTextureRectPosition.Y = 0;
            this._instanceData[this._instances].InstanceTextureRectSize.X     = texFlip == TextureFlip.FlipHorizontal ? -1 : 1;
            this._instanceData[this._instances].InstanceTextureRectSize.Y     = texFlip == TextureFlip.FlipVertical ? -1 : 1;

            this._instances++;
        }
        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            if (textureGl == null || textureGl is not TextureD3D11 texture)
                return;

            if (!IsBegun)
                throw new Exception("Begin() has not been called in QuadRendererD3D11!");

            if (this._instances >= INSTANCE_AMOUNT || this._usedTextures == this._backend.QueryMaxTextureUnits()) {
                this.End();
                this.Begin();
            }

            //Set Size to the Source Rectangle
            Vector2 size = new Vector2(sourceRect.Width, sourceRect.Height);

            //Apply Scale
            size *= scale;

            this._instanceData[this._instances].InstancePosition              = position;
            this._instanceData[this._instances].InstanceSize                  = size;
            this._instanceData[this._instances].InstanceColor                 = colorOverride;
            this._instanceData[this._instances].InstanceRotation              = rotation;
            this._instanceData[this._instances].InstanceRotationOrigin        = rotOrigin;
            this._instanceData[this._instances].InstanceTextureId             = this.GetTextureId(texture);
            this._instanceData[this._instances].InstanceTextureRectPosition.X = (float)sourceRect.X                       / texture.Width;
            this._instanceData[this._instances].InstanceTextureRectPosition.Y = (float)sourceRect.Y                       / texture.Height;
            this._instanceData[this._instances].InstanceTextureRectSize.X     = (float)sourceRect.Width  / texture.Width  * (texFlip == TextureFlip.FlipHorizontal ? -1 : 1);
            this._instanceData[this._instances].InstanceTextureRectSize.Y     = (float)sourceRect.Height / texture.Height * (texFlip == TextureFlip.FlipVertical ? -1 : 1);

            this._instances++;
        }

        private int GetTextureId(TextureD3D11 tex) {
            if(tex.UsedId != -1) return tex.UsedId;

            if(this._usedTextures != 0)
                for (int i = 0; i < this._usedTextures; i++) {
                    ID3D11ShaderResourceView tex2 = this._boundShaderViews[i];

                    if (tex2 == null) break;
                    if (tex.TextureView == tex2) return i;
                }

            this._boundShaderViews[this._usedTextures] = tex.TextureView;
            this._boundTextures[this._usedTextures]    = tex;

            tex.UsedId = this._usedTextures;

            this._usedTextures++;

            return this._usedTextures - 1;
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
            font.DrawText(this._textRenderer, text, position, System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B), scale.Value, rotation, default);
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
            if (this._instances == 0)
                return;

            this._deviceContext.PSSetShaderResources(0, 128, this._boundShaderViews);

            for (int i = 0; i != this._backend.QueryMaxTextureUnits(); i++)
                this._boundTextures[i].UsedId = -1;

            MappedSubresource instanceBufferMap = this._deviceContext.Map(this._instanceBuffer, MapMode.WriteDiscard);

            fixed (void* instanceData = this._instanceData) {
                long bytesToCopy = sizeof(InstanceData) * this._instances;

                Buffer.MemoryCopy(instanceData, (void*)instanceBufferMap.DataPointer, bytesToCopy, bytesToCopy);
            }

            this._deviceContext.Unmap(this._instanceBuffer, 0);

            this._deviceContext.DrawIndexedInstanced(6, this._instances, 0, 0, 0);

            this._usedTextures = 0;
            this._instances    = 0;
        }

        public void Dispose() {
            _vertexBuffer.Release();
            _instanceBuffer.Release();
            _indexBuffer.Release();
            _constantBuffer.Release();
            _inputLayout.Release();
            _vertexShader.Release();
            _pixelShader.Release();
            _samplerState.Release();
        }
    }
}
