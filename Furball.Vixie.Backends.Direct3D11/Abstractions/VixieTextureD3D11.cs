using System;
using System.IO;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers;
using Silk.NET.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Rectangle=System.Drawing.Rectangle;
#pragma warning disable CS8618

namespace Furball.Vixie.Backends.Direct3D11.Abstractions;

internal sealed class VixieTextureD3D11 : VixieTexture {
    private Direct3D11Backend _backend;

    private  ID3D11Texture2D          _texture;
    internal ID3D11ShaderResourceView TextureView;
    private  Texture2DDescription     _textureDescription;

    public VixieTextureD3D11(
        Direct3D11Backend backend, ID3D11Texture2D texture, ID3D11ShaderResourceView shaderResourceView,
        Vector2D<int> size, Texture2DDescription desc
    ) {
        backend.CheckThread();
        this._backend = backend;

        this.Size = size;

        this._texture    = texture;
        this.TextureView = shaderResourceView;

        this._textureDescription = desc;

        this.GenerateMips();
    }

    public unsafe VixieTextureD3D11(Direct3D11Backend backend) {
        backend.CheckThread();
        this._backend = backend;

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

        ID3D11Texture2D texture = this._backend.Device.CreateTexture2D(
        textureDescription,
        new[] {
            subresourceData
        }
        );
        ID3D11ShaderResourceView textureView = this._backend.Device.CreateShaderResourceView(texture);


        this._texture    = texture;
        this.TextureView = textureView;

        this.TextureView.DebugName = "white pixel";

        this._textureDescription = textureDescription;

        this.GenerateMips();

        this.Size = Vector2D<int>.One;
    }

    public VixieTextureD3D11(Direct3D11Backend backend, byte[] imageData, TextureParameters parameters) {
        backend.CheckThread();
        this._backend = backend;

        Image<Rgba32> image;

        bool qoi = imageData.Length > 3 && imageData[0] == 'q' && imageData[1] == 'o' && imageData[2] == 'i' &&
                   imageData[3] == 'f';

        if (qoi) {
            (Rgba32[] pixels, QoiLoader.QoiHeader header) data = QoiLoader.Load(imageData);

            image = Image.LoadPixelData(data.pixels, (int)data.header.Width, (int)data.header.Height);
        } else {
            image = Image.Load<Rgba32>(imageData);
        }

        this.CreateTextureAndView(image.Width, image.Height, parameters);

        this.SetData(image);

        this.GenerateMips();

        this.Size = new Vector2D<int>(image.Width, image.Height);

        image.Dispose();

        this.FilterType = parameters.FilterType;
    }

    private unsafe void SetData(Image<Rgba32> image) {
        image.ProcessPixelRows(
        accessor => {
            for (int i = 0; i < accessor.Height; i++)
                fixed (void* ptr = accessor.GetRowSpan(i)) {
                    this._backend.DeviceContext.UpdateSubresource(
                    this._texture,
                    0,
                    new Box(0, i, 0, accessor.Width, i + 1, 1),
                    (IntPtr)ptr,
                    sizeof(Rgba32) * accessor.Width,
                    sizeof(Rgba32) * accessor.Width
                    );
                }
        }
        );
    }

    public VixieTextureD3D11(Direct3D11Backend backend, Stream stream, TextureParameters parameters) {
        backend.CheckThread();
        this._backend = backend;

        Image<Rgba32> image = Image.Load<Rgba32>(stream);

        this.CreateTextureAndView(image.Width, image.Height, parameters);

        this.SetData(image);

        this.GenerateMips();

        this.Size = new Vector2D<int>(image.Width, image.Height);

        image.Dispose();

        this.FilterType = parameters.FilterType;
    }

    public VixieTextureD3D11(Direct3D11Backend backend, uint width, uint height, TextureParameters parameters) {
        backend.CheckThread();
        this._backend = backend;

        this.CreateTextureAndView((int)width, (int)height, parameters);

        this.GenerateMips();

        this.Size = new Vector2D<int>((int)width, (int)height);

        this.FilterType = parameters.FilterType;
    }

    private void CreateTextureAndView(int width, int height, TextureParameters parameters) {
        Texture2DDescription textureDescription = new Texture2DDescription {
            Width     = width,
            Height    = height,
            MipLevels = parameters.RequestMipmaps ? this.MipMapCount(width, height) : 1,
            ArraySize = 1,
            Format    = Format.R8G8B8A8_UNorm,
            BindFlags = parameters.RequestMipmaps
                            ? BindFlags.ShaderResource | BindFlags.RenderTarget
                            : BindFlags.ShaderResource,
            Usage     = ResourceUsage.Default,
            MiscFlags = parameters.RequestMipmaps ? ResourceOptionFlags.GenerateMips : ResourceOptionFlags.None,
            SampleDescription = new SampleDescription {
                Count   = 1,
                Quality = 0
            }
        };

        this._textureDescription = textureDescription;

        ID3D11Texture2D          texture     = this._backend.Device.CreateTexture2D(textureDescription);
        ID3D11ShaderResourceView textureView = this._backend.Device.CreateShaderResourceView(texture);

        this._texture    = texture;
        this.TextureView = textureView;
    }

    ~VixieTextureD3D11() {
        DisposeQueue.Enqueue(this);
    }

    public override bool Mipmaps => this._textureDescription.MipLevels != 1;

    public override unsafe VixieTexture SetData<pDataType>(ReadOnlySpan<pDataType> data) {
        this._backend.CheckThread();
        fixed (void* ptr = data) {
            this._backend.DeviceContext.UpdateSubresource(
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

    public override unsafe VixieTexture SetData<pDataType>(ReadOnlySpan<pDataType> data, Rectangle rect) {
        this._backend.CheckThread();
        fixed (void* dataPtr = data) {
            this._backend.DeviceContext.UpdateSubresource(
            this._texture,
            0,
            new Box(rect.X, rect.Y, 0, rect.X + rect.Width, rect.Y + rect.Height, 1),
            (IntPtr)dataPtr,
            sizeof(Rgba32) * rect.Width,
            sizeof(Rgba32) * rect.Width * rect.Height
            );
        }

        this._backend.DeviceContext.PSSetShaderResource(0, this.TextureView);

        this.GenerateMips();

        return this;
    }

    public override unsafe Rgba32[] GetData() {
        Texture2DDescription desc = this._textureDescription;
        desc.Usage          = ResourceUsage.Staging;
        desc.CPUAccessFlags = CpuAccessFlags.Read;
        desc.Format         = Format.R8G8B8A8_UNorm_SRgb;
        desc.MipLevels      = 1;

        //Create staging texture
        ID3D11Texture2D texture = this._backend.Device.CreateTexture2D(desc);

        //Copy texture to staging texture
        this._backend.DeviceContext.CopyResource(texture, this._texture);

        //Map data
        MappedSubresource mapped = this._backend.DeviceContext.Map(texture, 0);

        //Copy into array
        Span<Rgba32> rawData = mapped.AsSpan<Rgba32>(texture, 0, 0);

        //Create new array to store the pixels contiguously
        Rgba32[] data = new Rgba32[desc.Width * desc.Height];

        //Copy the data into a contiguous array
        for (int i = 0; i < desc.Height; i++)
            rawData.Slice(i * (mapped.RowPitch / sizeof(Rgba32)), desc.Width).CopyTo(data.AsSpan(i * desc.Width));

        //Unmap & dispose
        this._backend.DeviceContext.Unmap(texture, 0, 0);
        texture.Dispose();

        return data;
    }

    private void GenerateMips() {
        this._backend.DeviceContext.GenerateMips(this.TextureView);
    }

    private TextureFilterType _filterType = TextureFilterType.Smooth;

    public override TextureFilterType FilterType {
        get => this._filterType;
        set {
            this._filterType = value;

            //TODO: actually implement this
        }
    }

    public VixieTexture BindToPixelShader(int slot) {
        this._backend.CheckThread();
        this._backend.DeviceContext.PSSetShaderResource(slot, this.TextureView);

        return this;
    }

    private bool _isDisposed = false;

    public override void Dispose() {
        this._backend.CheckThread();

        if (this._isDisposed)
            return;

        this._isDisposed = true;

        try {
            this._texture.Dispose();
            this.TextureView.Dispose();
        }
        catch (NullReferenceException) {/* Apperantly thing?.Dispose can still throw a NullRefException? */
        }
    }
}