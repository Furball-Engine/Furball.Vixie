using System;
using System.IO;
using System.Numerics;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle=System.Drawing.Rectangle;

namespace Furball.Vixie.Graphics.Backends.Direct3D11.Abstractions {
    public class TextureD3D11 : Texture {
        private Direct3D11Backend _backend;

        private Texture2D          _texture;
        private ShaderResourceView _textureView;

        public override Vector2 Size { get; protected set; }

        public unsafe TextureD3D11(Direct3D11Backend backend) {
            this._backend = backend;

            Texture2DDescription textureDescription = new Texture2DDescription {
                Width     = 1,
                Height    = 1,
                MipLevels = 1,
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
            this._backend = backend;

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
            this._backend = backend;

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
            this._backend = backend;

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
            this._backend = backend;

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
            return this;
        }

        public override Texture SetData<pDataType>(int level, Rectangle rect, pDataType[] data) {
            return this;
        }
    }
}
