using System;
using System.IO;
using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Rectangle=System.Drawing.Rectangle;

namespace Furball.Vixie.Backends.Direct3D11.Abstractions; 

public class TextureD3D11 : Texture {
    private Direct3D11Backend   _backend;
    private ID3D11Device        _device;
    private ID3D11DeviceContext _deviceContext;

    private  ID3D11Texture2D          _texture;
    internal ID3D11ShaderResourceView TextureView;

    internal int UsedId = -1;

    public override Vector2 Size { get; protected set; }

    public TextureD3D11(Direct3D11Backend backend, ID3D11Texture2D texture, ID3D11ShaderResourceView shaderResourceView, Vector2 size) {
        backend.CheckThread();
        this._backend       = backend;
        this._deviceContext = backend.GetDeviceContext();
        this._device        = backend.GetDevice();

        this.Size = size;

        this._texture    = texture;
        this.TextureView = shaderResourceView;
    }

    public unsafe TextureD3D11(Direct3D11Backend backend) {
        backend.CheckThread();
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

        fixed (void* ptr = data) {
            SubresourceData subresourceData = new SubresourceData(ptr, 4);

            ID3D11Texture2D texture = this._device.CreateTexture2D(textureDescription, new []{ subresourceData} );
            ID3D11ShaderResourceView textureView = this._device.CreateShaderResourceView(texture);

            this._texture    = texture;
            this.TextureView = textureView;
        }

        this.Size = Vector2.One;
    }

    public unsafe TextureD3D11(Direct3D11Backend backend, byte[] imageData, bool qoi = false) {
        backend.CheckThread();
        this._backend       = backend;
        this._device        = backend.GetDevice();
        this._deviceContext = backend.GetDeviceContext();

        Image<Rgba32> image;

        if(qoi) {
            (Rgba32[] pixels, QoiLoader.QoiHeader header) data = QoiLoader.Load(imageData);

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

        ID3D11Texture2D texture = this._device.CreateTexture2D(textureDescription);

        image.ProcessPixelRows(accessor => {
            for (int i = 0; i < accessor.Height; i++) {
                fixed (void* ptr = &accessor.GetRowSpan(i).GetPinnableReference()) {
                    this._deviceContext.UpdateSubresource(texture, 0, new Box(0, i, 0, accessor.Width, i + 1, 1), (IntPtr)ptr, 4 * accessor.Width, (4 * accessor.Width));
                }
            }
        });

        ID3D11ShaderResourceView textureView = this._device.CreateShaderResourceView(texture);

        this._texture    = texture;
        this.TextureView = textureView;

        this.Size = new Vector2(image.Width, image.Height);

        image.Dispose();
    }

    public unsafe TextureD3D11(Direct3D11Backend backend, Stream stream) {
        backend.CheckThread();
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

        ID3D11Texture2D texture = this._device.CreateTexture2D(textureDescription);

        image.ProcessPixelRows(accessor => {
            for (int i = 0; i < accessor.Height; i++) {
                fixed (void* ptr = &accessor.GetRowSpan(i).GetPinnableReference()) {
                    this._deviceContext.UpdateSubresource(texture, 0, new Box(0, i, 0, accessor.Width, i + 1, 1), (IntPtr)ptr, 4 * accessor.Width, (4 * accessor.Width));
                }
            }
        });

        ID3D11ShaderResourceView textureView = this._device.CreateShaderResourceView(texture);

        this._texture    = texture;
        this.TextureView = textureView;

        this.Size = new Vector2(image.Width, image.Height);

        image.Dispose();
    }

    public TextureD3D11(Direct3D11Backend backend, uint width, uint height) {
        backend.CheckThread();
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

        ID3D11Texture2D          texture     = this._device.CreateTexture2D(textureDescription);
        ID3D11ShaderResourceView textureView = this._device.CreateShaderResourceView(texture);

        this._texture    = texture;
        this.TextureView = textureView;

        this.Size = new Vector2(width, height);
    }

    public unsafe TextureD3D11(Direct3D11Backend backend, string filepath) {
        backend.CheckThread();
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

        ID3D11Texture2D texture = this._device.CreateTexture2D(textureDescription);

        image.ProcessPixelRows(accessor => {
            for (int i = 0; i < accessor.Height; i++) {
                fixed (void* ptr = &accessor.GetRowSpan(i).GetPinnableReference()) {
                    this._deviceContext.UpdateSubresource(texture, 0, new Box(0, i, 0, accessor.Width, i + 1, 1), (IntPtr)ptr, 4 * accessor.Width, (4 * accessor.Width));
                }
            }
        });

        ID3D11ShaderResourceView textureView = this._device.CreateShaderResourceView(texture);

        this._texture    = texture;
        this.TextureView = textureView;

        this.Size = new Vector2(image.Width, image.Height);

        image.Dispose();
    }

    ~TextureD3D11() {
        DisposeQueue.Enqueue(this);
    }

    public override Texture SetData<pDataType>(int level, pDataType[] data) {
        this._backend.CheckThread();
        this._deviceContext.UpdateSubresource(data, this._texture);

        return this;
    }

    public override unsafe Texture SetData<pDataType>(int level, Rectangle rect, pDataType[] data) {
        this._backend.CheckThread();
        fixed (void* dataPtr = data) {
            this._deviceContext.UpdateSubresource(this._texture, level, new Box(rect.X, rect.Y, 0, rect.X + rect.Width, rect.Y + rect.Height, 1), (IntPtr)dataPtr, 4 * rect.Width, (4 * rect.Width) * rect.Height);
        }

        this._deviceContext.PSSetShaderResource(0, this.TextureView);

        return this;
    }

    public Texture BindToPixelShader(int slot) {
        this._backend.CheckThread();
        this._deviceContext.PSSetShaderResource(slot, this.TextureView);

        return this;
    }

    private bool _isDisposed = false;

    public override void Dispose() {
        this._backend.CheckThread();
            
        if (this._isDisposed)
            return;

        this._isDisposed = true;

        try {
            this._texture?.Dispose();
            this.TextureView?.Dispose();
        } catch(NullReferenceException) { /* Apperantly thing?.Dispose can still throw a NullRefException? */ }
    }
}