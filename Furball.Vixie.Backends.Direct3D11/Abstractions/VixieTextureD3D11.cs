using System;
using System.IO;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle = System.Drawing.Rectangle;
#pragma warning disable CS8618

namespace Furball.Vixie.Backends.Direct3D11.Abstractions;

internal sealed class VixieTextureD3D11 : VixieTexture {
    private Direct3D11Backend _backend;

    private  ComPtr<ID3D11Texture2D>          _texture;
    internal ComPtr<ID3D11ShaderResourceView> TextureView;
    private  Texture2DDesc                    _textureDesc;

    public VixieTextureD3D11(
        Direct3D11Backend backend, ComPtr<ID3D11Texture2D> texture, ComPtr<ID3D11ShaderResourceView> shaderResourceView,
        Vector2D<int>     size,    Texture2DDesc           desc
    ) {
        backend.CheckThread();
        this._backend = backend;

        this.Size = size;

        this._texture    = texture;
        this.TextureView = shaderResourceView;

        this._textureDesc = desc;

        this.GenerateMips();
    }

    public unsafe VixieTextureD3D11(Direct3D11Backend backend) {
        backend.CheckThread();
        this._backend = backend;

        Texture2DDesc textureDesc = new() {
            Width     = 1,
            Height    = 1,
            MipLevels = 0,
            ArraySize = 1,
            Format    = Format.FormatR8G8B8A8Unorm,
            SampleDesc = new SampleDesc {
                Count = 1
            },
            Usage     = Usage.Default,
            BindFlags = (uint)BindFlag.ShaderResource
        };

        byte* data = stackalloc byte[] {
            255, 255, 255, 255
        };

        SubresourceData subresourceData = new SubresourceData(data, 4);

        this._backend.Device.CreateTexture2D(
            textureDesc,
            in subresourceData,
            ref this._texture
        );
        this._backend.Device.CreateShaderResourceView(this._texture, null, ref this.TextureView);

        this._textureDesc = textureDesc;

        this.Size = Vector2D<int>.One;
    }

    public VixieTextureD3D11(Direct3D11Backend backend, byte[] imageData, TextureParameters parameters) {
        backend.CheckThread();
        this._backend = backend;

        Image<Rgba32> image;

        bool qoi = imageData.Length > 3 && imageData[0] == 'q' && imageData[1] == 'o' && imageData[2] == 'i' &&
                   imageData[3]     == 'f';

        if (qoi) {
            (Rgba32[] pixels, QoiLoader.QoiHeader header) data = QoiLoader.Load(imageData);

            image = Image.LoadPixelData(data.pixels, (int)data.header.Width, (int)data.header.Height);
        }
        else {
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
                            0u,
                            new Box(0, (uint)i, 0, (uint)accessor.Width, (uint)i + 1, 1),
                            ptr,
                            (uint)(sizeof(Rgba32) * accessor.Width),
                            (uint)(sizeof(Rgba32) * accessor.Width)
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

    private unsafe void CreateTextureAndView(int width, int height, TextureParameters parameters) {
        Texture2DDesc textureDesc = new() {
            Width     = (uint)width,
            Height    = (uint)height,
            MipLevels = (uint)(parameters.RequestMipmaps ? this.MipMapCount(width, height) : 1),
            ArraySize = 1,
            Format    = Format.FormatR8G8B8A8Unorm,
            BindFlags = (uint)(parameters.RequestMipmaps
                ? BindFlag.ShaderResource | BindFlag.RenderTarget
                : BindFlag.ShaderResource),
            Usage     = Usage.Default,
            MiscFlags = (uint)(parameters.RequestMipmaps ? ResourceMiscFlag.GenerateMips : ResourceMiscFlag.None),
            SampleDesc = new SampleDesc {
                Count   = 1,
                Quality = 0
            }
        };

        this._textureDesc = textureDesc;

        this._backend.Device.CreateTexture2D(textureDesc, null, ref this._texture);
        this._backend.Device.CreateShaderResourceView(this._texture, null, ref this.TextureView);
    }

    ~VixieTextureD3D11() {
        DisposeQueue.Enqueue(this);
    }

    public override bool Mipmaps => this._textureDesc.MipLevels != 1;

    public override unsafe VixieTexture SetData <pDataType>(ReadOnlySpan<pDataType> data) {
        this._backend.CheckThread();
        fixed (void* ptr = data) {
            this._backend.DeviceContext.UpdateSubresource(
                this._texture,
                0u,
                new Box(0, 0, 0, (uint)this.Width, (uint)this.Height, 1),
                ptr,
                (uint)(sizeof(Rgba32) * this.Width),
                (uint)(sizeof(Rgba32) * this.Width)
            );
        }

        this.GenerateMips();

        return this;
    }

    public override unsafe VixieTexture SetData <pDataType>(ReadOnlySpan<pDataType> data, Rectangle rect) {
        this._backend.CheckThread();
        fixed (void* dataPtr = data) {
            this._backend.DeviceContext.UpdateSubresource(
                this._texture,
                0,
                new Box((uint)rect.X, (uint)rect.Y, 0, (uint)(rect.X + rect.Width), (uint)(rect.Y + rect.Height), 1),
                dataPtr,
                (uint)(sizeof(Rgba32) * rect.Width),
                (uint)(sizeof(Rgba32) * rect.Width * rect.Height)
            );
        }

        this._backend.DeviceContext.PSSetShaderResources(0, 1, this.TextureView);

        this.GenerateMips();

        return this;
    }

    public override unsafe Rgba32[] GetData() {
        Texture2DDesc desc = this._textureDesc;
        desc.Usage          = Usage.Staging;
        desc.CPUAccessFlags = (uint)CpuAccessFlag.Read;
        desc.Format         = Format.FormatR8G8B8A8Unorm;
        desc.MipLevels      = 1;
        desc.BindFlags      = 0;

        //Create staging texture
        ComPtr<ID3D11Texture2D> stagingTex = null;
        int ret = this._backend.Device.CreateTexture2D(desc, null, ref stagingTex);

        this._backend.PrintInfoLog();

        //Copy texture to staging texture
        this._backend.DeviceContext.CopyResource(stagingTex, this._texture);

        //Map data
        MappedSubresource mapped = new MappedSubresource();
        this._backend.DeviceContext.Map(stagingTex, 0, Map.Read, 0, ref mapped);

        //Copy into array
        // Span<Rgba32> rawData = mapped.AsSpan<Rgba32>(stagingTex, 0, 0);
        Span<Rgba32> rawData = new Span<Rgba32>(mapped.PData, (int)(desc.Width * desc.Height));
        
        //Create new array to store the pixels contiguously
        Rgba32[] data = new Rgba32[desc.Width * desc.Height];

        //Copy the data into a contiguous array
        for (int i = 0; i < desc.Height; i++)
            rawData.Slice((int)(i * (mapped.RowPitch / sizeof(Rgba32))), (int)desc.Width).CopyTo(data.AsSpan((int)(i * desc.Width)));

        //Unmap & dispose
        this._backend.DeviceContext.Unmap(stagingTex, 0);
        stagingTex.Dispose();

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
        catch (NullReferenceException) { /* Apperantly thing?.Dispose can still throw a NullRefException? */
        }
    }
}