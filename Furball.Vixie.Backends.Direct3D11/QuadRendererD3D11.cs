using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using FontStashSharp;
using Furball.Vixie.Backends.Direct3D11.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.FontStashSharp;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
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
        private ID3D11ShaderResourceView[] _nullShaderViews;
        private TextureD3D11[]             _boundTextures;
        private int                        _usedTextures;

        private VixieFontStashRenderer _textRenderer;

        public QuadRendererD3D11(Direct3D11Backend backend) {
            this._backend       = backend;
            this._deviceContext = backend.GetDeviceContext();
            this._device        = backend.GetDevice();

            byte[] vertexShaderData = ResourceHelpers.GetByteResource("Shaders/Compiled/QuadRenderer/VertexShader.dxc");
            byte[] pixelShaderData = ResourceHelpers.GetByteResource("Shaders/Compiled/QuadRenderer/PixelShader.dxc");

            ID3D11VertexShader vertexShader = this._device.CreateVertexShader(vertexShaderData);
            ID3D11PixelShader pixelShader = this._device.CreatePixelShader(pixelShaderData);

            this._vertexShader = vertexShader;
            this._pixelShader  = pixelShader;

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

            ID3D11InputLayout inputLayout = this._device.CreateInputLayout(inputLayoutDescription, vertexShaderData);
            this._inputLayout           = inputLayout;
            //this._inputLayout.DebugName = "QuadRendererD3D11 Input Layout";

            BufferDescription constantBufferDescription = new BufferDescription {
                BindFlags = BindFlags.ConstantBuffer,
                ByteWidth = sizeof(ConstantBufferData),
                CPUAccessFlags = CpuAccessFlags.Write,
                Usage = ResourceUsage.Dynamic
            };

            ID3D11Buffer constantBuffer = this._device.CreateBuffer(constantBufferDescription);
            this._constantBuffer           = constantBuffer;
            //this._constantBuffer.DebugName = "QuadRendererD3D11 Constant Buffer";

            BufferDescription vertexBufferDescription = new BufferDescription {
                BindFlags = BindFlags.VertexBuffer,
                ByteWidth = sizeof(VertexData) * 4,
                CPUAccessFlags = CpuAccessFlags.Write,
                Usage = ResourceUsage.Dynamic
            };

            ID3D11Buffer vertexBuffer = this._device.CreateBuffer(vertexBufferDescription);
            this._vertexBuffer           = vertexBuffer;
            //this._vertexBuffer.DebugName = "QuadRendererD3D11 Vertex Buffer";

            BufferDescription instanceBufferDesription = new BufferDescription {
                BindFlags      = BindFlags.VertexBuffer,
                ByteWidth      = sizeof(InstanceData) * INSTANCE_AMOUNT,
                CPUAccessFlags = CpuAccessFlags.Write,
                Usage          = ResourceUsage.Dynamic
            };

            ID3D11Buffer instanceBuffer = this._device.CreateBuffer(instanceBufferDesription);
            this._instanceBuffer         = instanceBuffer;
            //this._vertexBuffer.DebugName = "QuadRendererD3D11 Instance Buffer";

            BufferDescription indexBufferDescription = new BufferDescription {
                BindFlags = BindFlags.IndexBuffer,
                ByteWidth = sizeof(ushort) * 6,
                CPUAccessFlags = CpuAccessFlags.Write,
                Usage = ResourceUsage.Dynamic
            };

            ID3D11Buffer indexBuffer = this._device.CreateBuffer(indexBufferDescription);
            this._indexBuffer            = indexBuffer;
            //this._vertexBuffer.DebugName = "QuadRendererD3D11 Index Buffer";

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
            this._nullShaderViews  = new ID3D11ShaderResourceView[128];
            this._boundTextures    = new TextureD3D11[backend.QueryMaxTextureUnits()];

            for (int i = 0; i != backend.QueryMaxTextureUnits(); i++) {
                TextureD3D11 texture = backend.GetPrivateWhitePixelTexture();
                this._boundShaderViews[i] = texture.TextureView;
                this._boundTextures[i]    = texture;
            }

            this._usedTextures  = 0;

            SamplerDescription samplerDescription = new SamplerDescription {
                Filter             = Filter.MinMagMipLinear,
                AddressU           = TextureAddressMode.Wrap,
                AddressV           = TextureAddressMode.Wrap,
                AddressW           = TextureAddressMode.Wrap,
                ComparisonFunction = ComparisonFunction.Never
            };

            this._samplerState           = this._device.CreateSamplerState(samplerDescription);
            //this._samplerState.DebugName = "QuadRendererD3D11 Sampler State";

            this._textRenderer = new VixieFontStashRenderer(this._backend, this);
        }

        ~QuadRendererD3D11() {
            DisposeQueue.Enqueue(this);
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
            this._deviceContext.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
        }

        public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            if (!IsBegun)
                throw new Exception("Begin() has not been called in QuadRendererD3D11!");

            if (texture == null || texture is not TextureD3D11 textureD3D11)
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
            this._instanceData[this._instances].InstanceTextureId             = this.GetTextureId(textureD3D11);
            this._instanceData[this._instances].InstanceTextureRectPosition.X = 0;
            this._instanceData[this._instances].InstanceTextureRectPosition.Y = 0;
            this._instanceData[this._instances].InstanceTextureRectSize.X     = texFlip == TextureFlip.FlipHorizontal ? -1 : 1;
            this._instanceData[this._instances].InstanceTextureRectSize.Y     = texFlip == TextureFlip.FlipVertical ? -1 : 1;

            this._instances++;
        }
        public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            if (texture == null || texture is not TextureD3D11 textureD3D11)
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
            this._instanceData[this._instances].InstanceTextureId             = this.GetTextureId(textureD3D11);
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

        public void Draw(Texture texture, Vector2 position, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
            this.Draw(texture, position, Vector2.One, rotation, Color.White, flip, rotOrigin);
        }

        public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
            this.Draw(texture, position, scale, rotation, Color.White, flip, rotOrigin);
        }

        public void Draw(Texture texture, Vector2 position, Vector2 scale, Color colorOverride, float rotation = 0, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            this.Draw(texture, position, scale, rotation, colorOverride, texFlip, rotOrigin);
        }

        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation = 0, Vector2? scale = null, Vector2 origin = default) {
            //Default Scale
            if(scale == null || scale == Vector2.Zero)
                scale = Vector2.One;

            //Draw
            font.DrawText(this._textRenderer, text, position, System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B), scale.Value, rotation, origin);
        }

        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color color, float rotation = 0, Vector2? scale = null, Vector2 origin = default) {
            if(scale == null || scale == Vector2.Zero)
                scale = Vector2.One;

            font.DrawText(this._textRenderer, text, position, color, scale.Value, rotation, origin);
        }

        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color[] colors, float rotation = 0, Vector2? scale = null, Vector2 origin = default) {
            //Default Scale
            if(scale == null || scale == Vector2.Zero)
                scale = Vector2.One;

            //Draw
            font.DrawText(this._textRenderer, text, position, colors, scale.Value, rotation, origin);
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

            this._deviceContext.VSSetShader(null);
            this._deviceContext.VSSetConstantBuffer(0, null);

            this._deviceContext.PSSetShader(null);
            this._deviceContext.PSSetSampler(0, null);

            this._deviceContext.PSSetShaderResources(0, 128, this._nullShaderViews);

            this._deviceContext.IASetInputLayout(null);
            this._deviceContext.IASetVertexBuffer(VERTEX_BUFFER_SLOT,   null, 0);
            this._deviceContext.IASetVertexBuffer(INSTANCE_BUFFER_SLOT, null, 0);
            this._deviceContext.IASetIndexBuffer(null, Format.R16_UInt, 0);
        }

        private bool _isDisposed = false;

        public void Dispose() {
            if (this._isDisposed)
                return;

            this._isDisposed = true;

            for (int i = 0; i != this._backend.QueryMaxTextureUnits(); i++) {
                this._boundTextures[i]    = null;
                this._boundShaderViews[i] = null;
            }

            try {
                _vertexBuffer?.Dispose();
                _instanceBuffer?.Dispose();
                _indexBuffer?.Dispose();
                _constantBuffer?.Dispose();
                _inputLayout?.Dispose();
                _vertexShader?.Dispose();
                _pixelShader?.Dispose();
                _samplerState?.Dispose();
            } catch(NullReferenceException) { /* Apperantly thing?.Dispose can still throw a NullRefException? */ }
        }
    }
}
