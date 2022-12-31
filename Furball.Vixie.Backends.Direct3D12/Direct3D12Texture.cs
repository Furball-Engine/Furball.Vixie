using Furball.Vixie.Backends.Direct3D12.Abstractions;
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

    public readonly Direct3D12DescriptorHeap Heap;
    public readonly int                      HeapSlot;

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
        this._texture = this._backend.Device.CreateCommittedResource<ID3D12Resource>(
            &textureHeapProperties,
            HeapFlags.None,
            &textureDesc,
            ResourceStates.CopyDest, //TODO: figure out how to use a texture as a copy source
            null
        );
        
        SubresourceFootprint footprint = new SubresourceFootprint {
            Format   = Format.FormatR8G8B8A8Unorm,
            Width    = (uint)img.Width,
            Height   = (uint)img.Height,
            Depth    = 1,
            RowPitch = Align((uint)(img.Width * sizeof(Rgba32)), D3D12.TextureDataPitchAlignment)
        };
        
        ulong uploadBufferSize = (ulong)(footprint.RowPitch * img.Height);
        
        ResourceDesc uploadBufferDesc = new ResourceDesc {
            Dimension        = ResourceDimension.Buffer,
            Width            = uploadBufferSize,
            Height           = 1,
            Format           = Format.FormatUnknown,
            DepthOrArraySize = 1,
            Flags            = ResourceFlags.None,
            MipLevels        = 1,
            SampleDesc       = new SampleDesc(1, 0),
            Layout           = TextureLayout.LayoutRowMajor
        };

        HeapProperties uploadBufferHeapProperties = new HeapProperties {
            Type                 = HeapType.Upload,
            CPUPageProperty      = CpuPageProperty.None,
            CreationNodeMask     = 0,
            VisibleNodeMask      = 0,
            MemoryPoolPreference = MemoryPool.None
        };
        ComPtr<ID3D12Resource> uploadBuffer = this._backend.Device.CreateCommittedResource<ID3D12Resource>(
            &uploadBufferHeapProperties, HeapFlags.None, &uploadBufferDesc, ResourceStates.GenericRead, null);

        //Declare the pointer which will point to our mapped data
        void* mapBegin = null;
        
        //Map the resource with no read range
        SilkMarshal.ThrowHResult(uploadBuffer.Map(0, new Range(0, 0), &mapBegin));
        void* mapCurrent = mapBegin;
        void* mapEnd     = (void*)((ulong)mapBegin + uploadBufferDesc.Width);
        
        PlacedSubresourceFootprint placedTexture2D = new PlacedSubresourceFootprint {
            Offset = (ulong)mapCurrent - (ulong)mapBegin,
            Footprint = footprint
        };

        void* mapBegin2 = mapBegin;
        
        //Copy all the pixel data to the placed mapped pointer
        img.ProcessPixelRows(imgAccessor => {
            for (int y = 0; y < imgAccessor.Height; y++) {
                imgAccessor.GetRowSpan(y).CopyTo(
                    new Span<Rgba32>((void*)((nint)mapBegin2 + (nint)placedTexture2D.Offset + y * footprint.RowPitch),
                                     sizeof(Rgba32) * imgAccessor.Width));
            }
        });
        
        //Unmap the buffer
        uploadBuffer.Unmap(0, (Range*)null);

        this._backend.CommandList.CopyTextureRegion(
            new TextureCopyLocation(
                this._texture,
                TextureCopyType.SubresourceIndex,
                new TextureCopyLocationUnion(null, 0),
                null,
                0
            ),
            0, 0, 0,
            new TextureCopyLocation(
                uploadBuffer,
                TextureCopyType.PlacedFootprint,
                new TextureCopyLocationUnion(placedTexture2D),
                placedTexture2D
            ),
            null
        );

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
            Format                  = textureDesc.Format,
            ViewDimension           = SrvDimension.Texture2D
        };
        srvDesc.Anonymous.Texture2D.MipLevels = textureDesc.MipLevels;

        this.Heap     = this._backend.CbvSrvUavHeap;
        this.HeapSlot = this._backend.CbvSrvUavHeap.GetSlot();
        (CpuDescriptorHandle Cpu, GpuDescriptorHandle Gpu) handles =
            this._backend.CbvSrvUavHeap.GetHandlesForSlot(this.HeapSlot);

        this._backend.Device.CreateShaderResourceView(this._texture, &srvDesc, handles.Cpu);
    }

    static uint Align(uint uValue, uint uAlign) {
        // Assert power of 2 alignment
        // Guard.Assert(0 == (uAlign & (uAlign - 1)));
        uint uMask   = uAlign - 1;
        uint uResult = (uValue + uMask) & ~uMask;
        // Guard.Assert(uResult >= uValue);
        // Guard.Assert(0       == (uResult % uAlign));
        return uResult;
    }

    public override TextureFilterType FilterType {
        get;
        set;
    }
    public override bool Mipmaps {
        get;
    }
    public override VixieTexture SetData <pT>(ReadOnlySpan<pT> data) => throw new NotImplementedException();
    public override VixieTexture SetData <pT>(ReadOnlySpan<pT> data, Rectangle rect) =>
        throw new NotImplementedException();
    public override Rgba32[] GetData() => throw new NotImplementedException();
    public override void CopyTo(VixieTexture tex) {
        throw new NotImplementedException();
    }
}