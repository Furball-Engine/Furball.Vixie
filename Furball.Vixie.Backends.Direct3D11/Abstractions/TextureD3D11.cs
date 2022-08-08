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
#pragma warning disable CS8618

namespace Furball.Vixie.Backends.Direct3D11.Abstractions; 

internal sealed class TextureD3D11 : Texture {
    private Direct3D11Backend   _backend;
    private ID3D11Device        _device;
    private ID3D11DeviceContext _deviceContext;

    private  ID3D11Texture2D          _texture;
    internal ID3D11ShaderResourceView TextureView;

    internal int UsedId = -1;

    public TextureD3D11(Direct3D11Backend backend, ID3D11Texture2D texture, ID3D11ShaderResourceView shaderResourceView, Vector2 size) {
        backend.CheckThread();
        this._backend       = backend;
        this._deviceContext = backend.GetDeviceContext();
        this._device        = backend.GetDevice();

        this._size = size;

        this._texture    = texture;
        this.TextureView = shaderResourceView;

        this.GenerateMips();
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

        byte* data = stackalloc byte[] {
            255, 255, 255, 255
        };

        SubresourceData subresourceData = new(data, 4);

        ID3D11Texture2D texture = this._device.CreateTexture2D(
        textureDescription,
        new[] {
            subresourceData
        }
        );
        ID3D11ShaderResourceView textureView = this._device.CreateShaderResourceView(texture);

        this._texture    = texture;
        this.TextureView = textureView;

        this.GenerateMips();

        this._size = Vector2.One;
    }

    public TextureD3D11(Direct3D11Backend backend, byte[] imageData, TextureParameters parameters) {
        backend.CheckThread();
        this._backend       = backend;
        this._device        = backend.GetDevice();
        this._deviceContext = backend.GetDeviceContext();

        Image<Rgba32> image;

        bool qoi = imageData.Length > 3 && imageData[0] == 'q' && imageData[1] == 'o' && imageData[2] == 'i' &&
                   imageData[3]     == 'f';
        
        if(qoi) {
            (Rgba32[] pixels, QoiLoader.QoiHeader header) data = QoiLoader.Load(imageData);

            image = Image.LoadPixelData(data.pixels, (int)data.header.Width, (int)data.header.Height);
        } else {
            image = Image.Load<Rgba32>(imageData);
        }

        this.CreateTextureAndView(image.Width, image.Height, parameters);

        this.SetData(image);

        this.GenerateMips();

        this._size = new Vector2(image.Width, image.Height);

        image.Dispose();

        this.FilterType = parameters.FilterType;
    }

    private unsafe void SetData(Image<Rgba32> image) {
        image.ProcessPixelRows(
        accessor => {
            for (int i = 0; i < accessor.Height; i++)
                fixed (void* ptr = &accessor.GetRowSpan(i).GetPinnableReference()) {
                    this._deviceContext.UpdateSubresource(
                    this._texture,
                    0,
                    new Box(0, i, 0, accessor.Width, i + 1, 1),
                    (IntPtr)ptr,
                    4 * accessor.Width,
                    4 * accessor.Width
                    );
                }
        }
        );
    }

    public TextureD3D11(Direct3D11Backend backend, Stream stream, TextureParameters parameters) {
        backend.CheckThread();
        this._backend       = backend;
        this._device        = backend.GetDevice();
        this._deviceContext = backend.GetDeviceContext();

        Image<Rgba32> image = Image.Load<Rgba32>(stream);

        this.CreateTextureAndView(image.Width, image.Height, parameters);

        this.SetData(image);

        this.GenerateMips();

        this._size = new Vector2(image.Width, image.Height);

        image.Dispose();

        this.FilterType = parameters.FilterType;
    }

    public TextureD3D11(Direct3D11Backend backend, uint width, uint height, TextureParameters parameters) {
        backend.CheckThread();
        this._backend       = backend;
        this._device        = backend.GetDevice();
        this._deviceContext = backend.GetDeviceContext();

        this.CreateTextureAndView((int)width, (int)height, parameters);

        this.GenerateMips();

        this._size = new Vector2(width, height);

        this.FilterType = parameters.FilterType;
    }

    private void CreateTextureAndView(int width, int height, TextureParameters parameters) {
        Texture2DDescription textureDescription = new Texture2DDescription {
            Width     = width,
            Height    = height,
            MipLevels = parameters.RequestMipmaps ? this.MipMapCount(width, height) : 1,
            ArraySize = 1,
            Format    = Format.R8G8B8A8_UNorm,
            BindFlags = parameters.RequestMipmaps ? BindFlags.ShaderResource | BindFlags.RenderTarget
                            : BindFlags.ShaderResource,
            Usage     = ResourceUsage.Default,
            MiscFlags = parameters.RequestMipmaps ? ResourceOptionFlags.GenerateMips : ResourceOptionFlags.None,
            SampleDescription = new SampleDescription {
                Count = 1, Quality = 0
            },
        };

        ID3D11Texture2D          texture     = this._device.CreateTexture2D(textureDescription);
        ID3D11ShaderResourceView textureView = this._device.CreateShaderResourceView(texture);

        this._texture    = texture;
        this.TextureView = textureView;
    }

    ~TextureD3D11() {
        DisposeQueue.Enqueue(this);
    }

    public override unsafe Texture SetData <pDataType>(int level, pDataType[] data) {
        this._backend.CheckThread();
        fixed (void* ptr = data) {
            this._deviceContext.UpdateSubresource(
            this._texture,
            0,
            new Box(0, 0, 0, this.Width, this.Height, 1),
            (IntPtr)ptr,
            sizeof(Rgba32) * this.Width,
            sizeof(Rgba32) * this.Width
            );
        }

        this.GenerateMips();

        return this;
    }

    public override unsafe Texture SetData<pDataType>(int level, Rectangle rect, pDataType[] data) {
        this._backend.CheckThread();
        fixed (void* dataPtr = data) {
            this._deviceContext.UpdateSubresource(this._texture, level, new Box(rect.X, rect.Y, 0, rect.X + rect.Width, rect.Y + rect.Height, 1), (IntPtr)dataPtr, 4 * rect.Width, (4 * rect.Width) * rect.Height);
        }

        this._deviceContext.PSSetShaderResource(0, this.TextureView);

        this.GenerateMips();

        return this;
    }

    private void GenerateMips() {
        this._deviceContext.GenerateMips(this.TextureView);
    }

    private TextureFilterType _filterType = TextureFilterType.Smooth;
    public override TextureFilterType FilterType {
        get => this._filterType;
        set {
            this._filterType = value;
            
            //TODO: actually implement this
        }
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