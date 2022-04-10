using System;
using System.IO;
using System.Numerics;
using Furball.Vixie.Backends.Shared;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Device=SharpDX.Direct3D11.Device;
using Rectangle=System.Drawing.Rectangle;

namespace Furball.Vixie.Backends.Direct3D11.Abstractions {
    public class TextureD3D11 : Texture {
        private Direct3D11Backend _backend;
        private Device            _device;
        private DeviceContext     _deviceContext;

        private Texture2D          _texture;
        private ShaderResourceView _textureView;

        public override Vector2 Size { get; protected set; }

        public TextureD3D11(Direct3D11Backend backend, Texture2D texture, ShaderResourceView shaderResourceView, Vector2 size) {
            this._backend       = backend;
            this._deviceContext = backend.GetDeviceContext();
            this._device        = backend.GetDevice();

            this.Size = size;

            this._texture     = texture;
            this._textureView = shaderResourceView;
        }

        public unsafe TextureD3D11(Direct3D11Backend backend) {
            this._backend       = backend;
            this._device        = backend.GetDevice();
            this._deviceContext = backend.GetDeviceContext();

            Texture2DDescription textureDescription = new Texture2DDescription {
                Width     = 1,
                Height    = 1,
                MipLevels = 0,
                ArraySize = 1,
                Format    = Format.R8G8B8A8_UNorm_SRgb,
                SampleDescription = new SampleDescription {
                    Count = 1
                },
                Usage     = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource
            };

            byte[] data = new byte[] {
                255, 255, 255, 255
            };

           fixed (byte* ptr = data) {
               Texture2D texture = new Texture2D(backend.GetDevice(), textureDescription, new DataRectangle((IntPtr) ptr, 4));
               ShaderResourceView textureView = new ShaderResourceView(backend.GetDevice(), texture);
               this._texture     = texture;
               this._textureView = textureView;
           }

           this.Size = Vector2.One;
        }

        public unsafe TextureD3D11(Direct3D11Backend backend, byte[] imageData, bool qoi = false) {
            this._backend       = backend;
            this._device        = backend.GetDevice();
            this._deviceContext = backend.GetDeviceContext();

            Image<Rgba32> image;

            if(qoi) {
                (Rgba32[] pixels, QoiLoader.QoiHeader header) data  = QoiLoader.Load(imageData);

                image = Image.LoadPixelData(data.pixels, (int)data.header.Width, (int)data.header.Height);
            } else {
                image = Image.Load<Rgba32>(imageData);
            }

            Texture2DDescription textureDescription = new Texture2DDescription {
                Width     = image.Width,
                Height    = image.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format    = Format.R8G8B8A8_UNorm,
                BindFlags = BindFlags.ShaderResource,
                Usage     = ResourceUsage.Default,
                SampleDescription = new SampleDescription {
                    Count = 1, Quality = 0
                },
            };

            image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels);

            Texture2D texture = new Texture2D(backend.GetDevice(), textureDescription, new DataRectangle((IntPtr) pixels.Pin().Pointer, 4 * image.Width));
            ShaderResourceView textureView = new ShaderResourceView(backend.GetDevice(), texture);

            this._texture     = texture;
            this._textureView = textureView;

            this.Size = new Vector2(image.Width, image.Height);
        }

        public unsafe TextureD3D11(Direct3D11Backend backend, Stream stream) {
            this._backend       = backend;
            this._device        = backend.GetDevice();
            this._deviceContext = backend.GetDeviceContext();

            Image<Rgba32> image = Image.Load<Rgba32>(stream);

            Texture2DDescription textureDescription = new Texture2DDescription {
                Width     = image.Width,
                Height    = image.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format    = Format.R8G8B8A8_UNorm,
                BindFlags = BindFlags.ShaderResource,
                Usage     = ResourceUsage.Default,
                SampleDescription = new SampleDescription {
                    Count = 1, Quality = 0
                },
            };

            image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels);

            Texture2D texture = new Texture2D(backend.GetDevice(), textureDescription, new DataRectangle((IntPtr) pixels.Pin().Pointer, 4 * image.Width));
            ShaderResourceView textureView = new ShaderResourceView(backend.GetDevice(), texture);

            this._texture     = texture;
            this._textureView = textureView;

            this.Size = new Vector2(image.Width, image.Height);
        }

        public TextureD3D11(Direct3D11Backend backend, uint width, uint height) {
            this._backend       = backend;
            this._device        = backend.GetDevice();
            this._deviceContext = backend.GetDeviceContext();

            Texture2DDescription textureDescription = new Texture2DDescription {
                Width     = (int) width,
                Height    = (int) height,
                MipLevels = 1,
                ArraySize = 1,
                Format    = Format.R8G8B8A8_UNorm,
                BindFlags = BindFlags.ShaderResource,
                Usage     = ResourceUsage.Default,
                SampleDescription = new SampleDescription {
                    Count = 1, Quality = 0
                },
            };

            Texture2D texture = new Texture2D(backend.GetDevice(), textureDescription);
            ShaderResourceView textureView = new ShaderResourceView(backend.GetDevice(), texture);

            this._texture     = texture;
            this._textureView = textureView;

            this.Size = new Vector2(width, height);
        }

        public unsafe TextureD3D11(Direct3D11Backend backend, string filepath) {
            this._backend       = backend;
            this._device        = backend.GetDevice();
            this._deviceContext = backend.GetDeviceContext();

            Image<Rgba32> image = (Image<Rgba32>)Image.Load(filepath);

            Texture2DDescription textureDescription = new Texture2DDescription {
                Width     = image.Width,
                Height    = image.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format    = Format.R8G8B8A8_UNorm,
                BindFlags = BindFlags.ShaderResource,
                Usage     = ResourceUsage.Default,
                SampleDescription = new SampleDescription {
                    Count = 1, Quality = 0
                },
            };

            image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels);

            Texture2D texture = new Texture2D(backend.GetDevice(), textureDescription, new DataRectangle((IntPtr) pixels.Pin().Pointer, 4 * image.Width));
            ShaderResourceView textureView = new ShaderResourceView(backend.GetDevice(), texture);

            this._texture     = texture;
            this._textureView = textureView;

            this.Size = new Vector2(image.Width, image.Height);
        }

        public override Texture SetData<pDataType>(int level, pDataType[] data) {
            this._deviceContext.UpdateSubresource(data, this._texture);

            return this;
        }

        public override unsafe Texture SetData<pDataType>(int level, Rectangle rect, pDataType[] data) {
            fixed (void* dataPtr = data) {
                this._deviceContext.UpdateSubresource(this._texture, level, new ResourceRegion(rect.X, rect.Y, 0, rect.X + rect.Width, rect.Y + rect.Height, 1), (IntPtr)dataPtr, 4 * rect.Width, (4 * rect.Width) * rect.Height);
            }

            this._deviceContext.PixelShader.SetShaderResource(0, this._textureView);

            return this;
        }

        public Texture BindToPixelShader(int slot) {
            this._deviceContext.PixelShader.SetShaderResource(slot, this._textureView);

            return this;
        }
    }
}
