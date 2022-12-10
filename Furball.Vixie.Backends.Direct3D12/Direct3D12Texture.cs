using Furball.Vixie.Backends.Shared;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle=System.Drawing.Rectangle;

namespace Furball.Vixie.Backends.Direct3D12;

public unsafe class Direct3D12Texture : VixieTexture {
    private readonly Direct3D12Backend _backend;

    private readonly ComPtr<ID3D12Resource> _texture;

    public uint Shader4ComponentMapping(uint src0, uint src1, uint src2, uint src3) {
        return src0 & 0x7            |
               (src1 & 0x7) << 3     |
               (src2 & 0x7) << 3 * 2 |
               (src3 & 0x7) << 3 * 3 |
               1            << 3 * 4;
    }


    public Direct3D12Texture(Direct3D12Backend backend, Image<Rgba32> img, TextureParameters parameters) {
        this._backend = backend;

        this.Mipmaps = parameters.RequestMipmaps;

        ResourceDesc textureDesc = new ResourceDesc {
            MipLevels        = (ushort)(parameters.RequestMipmaps ? this.MipMapCount(img.Width, img.Height) : 1),
            Format           = Format.FormatR8G8B8A8Unorm,
            Width            = (ulong)img.Width,
            Height           = (uint)img.Height,
            Flags            = ResourceFlags.None,
            DepthOrArraySize = 1,
            Dimension        = ResourceDimension.Texture2D,
            SampleDesc = new SampleDesc {
                Count   = 1,
                Quality = 0
            }
        };

        HeapProperties textureHeapProperties = new HeapProperties {
            Type                 = HeapType.Default,
            CPUPageProperty      = CpuPageProperty.None,
            CreationNodeMask     = 0,
            VisibleNodeMask      = 0,
            MemoryPoolPreference = MemoryPool.None
        };
        this._texture = this._backend.Device.CreateCommittedResource<ID3D12Resource>(&textureHeapProperties, HeapFlags.None, &textureDesc, ResourceStates.CopyDest | ResourceStates.CopySource, null);

        ulong uploadBufferSize = (ulong)(sizeof(Rgba32) * img.Width * img.Height);

        ResourceDesc uploadBufferDesc = new ResourceDesc {
            Dimension        = ResourceDimension.Buffer,
            Width            = uploadBufferSize,
            Format           = Format.FormatUnknown,
            DepthOrArraySize = 1,
            Flags            = ResourceFlags.None
        };

        HeapProperties uploadBufferHeapProperties = new HeapProperties {
            Type                 = HeapType.Upload,
            CPUPageProperty      = CpuPageProperty.None,
            CreationNodeMask     = 0,
            VisibleNodeMask      = 0,
            MemoryPoolPreference = MemoryPool.None
        };
        ComPtr<ID3D12Resource> uploadBuffer = this._backend.Device.CreateCommittedResource<ID3D12Resource>(&uploadBufferHeapProperties, HeapFlags.None, &uploadBufferDesc, ResourceStates.GenericRead, null);

        //Declare the pointer which will point to our mapped data
        void* mappedPtr = null;
        //Map the resource with no read range
        SilkMarshal.ThrowHResult(uploadBuffer.Map(0, new Range(0, 0), &mappedPtr));
        //Copy the image pixel data to the mapped pointer
        img.CopyPixelDataTo(new Span<Rgba32>(mappedPtr, (int)uploadBufferSize));
        //Unmap the buffer
        uploadBuffer.Unmap(0, (Range*)null);

        //Copy the upload buffer into the texture
        this._backend.CommandList.CopyResource(this._texture, uploadBuffer);

        //Tell the command list to wait for the texture to be copied before using it as a pixel shader resource
        ResourceBarrier copyBarrier = new ResourceBarrier {
            Type = ResourceBarrierType.Transition
        };
        copyBarrier.Anonymous.Transition.PResource   = this._texture;
        copyBarrier.Anonymous.Transition.Subresource = 0;
        copyBarrier.Anonymous.Transition.StateAfter  = ResourceStates.PixelShaderResource;
        copyBarrier.Anonymous.Transition.StateBefore = ResourceStates.CopyDest;
        this._backend.CommandList.ResourceBarrier(1, &copyBarrier);

        //Release the upload buffer as we no longer need it
        uploadBuffer.Release();

        ShaderResourceViewDesc srvDesc = new ShaderResourceViewDesc {
            Shader4ComponentMapping = this.Shader4ComponentMapping(0, 1, 2, 3),
            Format = textureDesc.Format,
            ViewDimension = SrvDimension.Texture2D
        };
        srvDesc.Anonymous.Texture2D.MipLevels = textureDesc.MipLevels;
        // this._backend.Device.CreateShaderResourceView(this._texture, &srvDesc, );
    }

    public override TextureFilterType FilterType {
        get;
        set;
    }
    public override bool Mipmaps {
        get;
    }
    public override VixieTexture SetData <pT>(ReadOnlySpan<pT> data)                 => throw new NotImplementedException();
    public override VixieTexture SetData <pT>(ReadOnlySpan<pT> data, Rectangle rect) => throw new NotImplementedException();
    public override Rgba32[]     GetData()                => throw new NotImplementedException();
    public override void CopyTo(VixieTexture tex) {
        throw new NotImplementedException();
    }
}