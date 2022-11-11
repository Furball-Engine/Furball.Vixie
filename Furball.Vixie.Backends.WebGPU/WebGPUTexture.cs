using System;
using Furball.Vixie.Backends.Shared;
using Silk.NET.Maths;
using Silk.NET.WebGPU;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle = System.Drawing.Rectangle;

namespace Furball.Vixie.Backends.WebGPU;

public unsafe class WebGPUTexture : VixieTexture {
    private readonly WebGPUBackend          _backend;
    private readonly Silk.NET.WebGPU.WebGPU _webGpu;

    private readonly TextureParameters _parameters;

    public readonly Texture*     Texture;
    public readonly TextureView* TextureView;

    public BindGroup* BindGroup;

    public WebGPUTexture(WebGPUBackend backend, int width, int height, TextureParameters parameters) {
        this._backend    = backend;
        this._parameters = parameters;
        this._webGpu     = backend.WebGPU;

        this.Size = new Vector2D<int>(width, height);

        TextureFormat format = TextureFormat.Rgba8UnormSrgb;

        this.Texture = this._webGpu.DeviceCreateTexture(backend.Device, new TextureDescriptor {
            Dimension       = TextureDimension.TextureDimension2D,
            Format          = format,
            Size            = new Extent3D((uint)width, (uint)height, 1),
            Usage           = TextureUsage.CopyDst | TextureUsage.CopySrc | TextureUsage.TextureBinding,
            MipLevelCount   = parameters.RequestMipmaps ? (uint)this.MipMapCount(width, height) : 1,
            SampleCount     = 1,
            ViewFormats     = &format,
            ViewFormatCount = 1
        });

        this.TextureView = this._webGpu.TextureCreateView(this.Texture, new TextureViewDescriptor {
            ArrayLayerCount = 1,
            MipLevelCount   = parameters.RequestMipmaps ? (uint)this.MipMapCount(width, height) : 1,
            Format          = format,
            Dimension       = TextureViewDimension.TextureViewDimension2D,
            BaseArrayLayer  = 0,
            BaseMipLevel    = 0,
            Aspect          = TextureAspect.None
        });

        this.CreateBindGroup();
    }
    
    private void CreateBindGroup() {
        if (this.BindGroup != null) {
            this._backend.Disposal.Dispose(this.BindGroup);
            
            this.BindGroup = null;
        }
        
        BindGroupEntry* bindGroupEntries = stackalloc BindGroupEntry[2];
        bindGroupEntries[0] = new BindGroupEntry
        {
            Binding     = 0,
            TextureView = this.TextureView
        };
        bindGroupEntries[1] = new BindGroupEntry
        {
            Binding = 1,
            Sampler = this.FilterType == TextureFilterType.Pixelated 
                ? this._backend.NearestSampler 
                : this._backend.LinearSampler
        };

        this.BindGroup = this._webGpu.DeviceCreateBindGroup(this._backend.Device, new BindGroupDescriptor
        {
            Entries    = bindGroupEntries,
            EntryCount = 2,
            Layout     = this._backend.TextureSamplerBindGroupLayout
        });
    }

    private TextureFilterType _filterType;
    public override TextureFilterType FilterType {
        get => this._filterType;
        set {
            this._filterType = value;
            
            this.CreateBindGroup();
        }
    }

    public override bool Mipmaps => this._webGpu.TextureGetMipLevelCount(this.Texture) > 1;

    public override VixieTexture SetData <T>(ReadOnlySpan<T> data) {
        if (data.Length * sizeof(T) < this.Size.X * this.Size.Y * sizeof(Rgba32))
            throw new ArgumentException($"{nameof (data)} is too small!", nameof (data));

        this.SetData(data, new Rectangle(0, 0, this.Width, this.Height));

        return this;
    }

    public override VixieTexture SetData <T>(ReadOnlySpan<T> data, Rectangle rect) {
        fixed (T* ptr = data)
            this._webGpu.QueueWriteTexture(
            this._backend.Queue,
                new ImageCopyTexture {
                    Texture  = this.Texture,
                    Aspect   = TextureAspect.None,
                    Origin   = new Origin3D((uint)rect.X, (uint)rect.Y),
                    MipLevel = 0
                },
                ptr,
                (nuint)(sizeof(T) * data.Length),
                new TextureDataLayout {
                    BytesPerRow  = (uint)(rect.Width * sizeof(Rgba32)),
                    RowsPerImage = (uint)rect.Height
                },
                new Extent3D {
                    DepthOrArrayLayers = 1,
                    Width              = (uint)rect.Width,
                    Height             = (uint)rect.Height
                }
            );

        return this;
    }

    public override Rgba32[] GetData() {
        //TODO

        // ImageCopyTexture texture = new ImageCopyTexture();
        // ImageCopyBuffer  buffer  = new ImageCopyBuffer();

        // this._webGpu.CommandEncoderCopyTextureToBuffer(encoder, texture, buffer, new Extent3D());
        return Array.Empty<Rgba32>();
    }

    private bool       _isDisposed;
    public override void Dispose() {
        base.Dispose();

        if (this._isDisposed)
            return;

        this._backend.Disposal.Dispose(this.Texture);
        this._backend.Disposal.Dispose(this.TextureView);
        this._backend.Disposal.Dispose(this.BindGroup);

        this._isDisposed = true;
    }

    ~WebGPUTexture() {
        this.Dispose();
    }
}