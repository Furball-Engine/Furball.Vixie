using Furball.Vixie.Backends.Direct3D12.Abstractions;
using Furball.Vixie.Backends.Shared;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Maths;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle=System.Drawing.Rectangle;

namespace Furball.Vixie.Backends.Direct3D12;

public unsafe class Direct3D12Texture : VixieTexture {
    private readonly Direct3D12Backend _backend;
    public readonly  bool              RenderTarget;

    public readonly ComPtr<ID3D12Resource> Texture;

    public readonly Direct3D12DescriptorHeap Heap;
    public readonly Direct3D12DescriptorHeap SamplerHeap;

    public readonly int SRVHeapSlot;
    public readonly int SamplerHeapSlot;

    public uint Shader4ComponentMapping(uint src0, uint src1, uint src2, uint src3) {
        return src0 & 0x7            |
               (src1 & 0x7) << 3     |
               (src2 & 0x7) << 3 * 2 |
               (src3 & 0x7) << 3 * 3 |
               1            << 3 * 4;
    }

    public Direct3D12Texture(Direct3D12Backend backend, int width, int height, TextureParameters parameters, bool renderTarget = false) {
        this._backend      = backend;
        this.RenderTarget = renderTarget;
        this.Size          = new Vector2D<int>(width, height);

        //Store whether or not we are using mipmaps
        this.Mipmaps = parameters.RequestMipmaps;

        //Create a description of the texture resource
        ResourceDesc textureDesc = new ResourceDesc {
            MipLevels        = (ushort)(parameters.RequestMipmaps ? this.MipMapCount(width, height) : 1),
            Format           = Format.FormatR8G8B8A8Unorm,
            Width            = (ulong)width,
            Height           = (uint)height,
            Flags            = renderTarget ? ResourceFlags.AllowRenderTarget : ResourceFlags.None,
            DepthOrArraySize = 1,
            Dimension        = ResourceDimension.Texture2D,
            SampleDesc = new SampleDesc {
                Count   = 1,
                Quality = 0
            }
        };

        //List the heap properties of the texture, being all default
        HeapProperties textureHeapProperties = new HeapProperties {
            Type                 = HeapType.Default,
            CPUPageProperty      = CpuPageProperty.None,
            CreationNodeMask     = 0,
            VisibleNodeMask      = 0,
            MemoryPoolPreference = MemoryPool.None
        };
        this.CurrentResourceState = ResourceStates.PixelShaderResource;
        //Create the texture
        this.Texture = this._backend.Device.CreateCommittedResource<ID3D12Resource>(
            &textureHeapProperties,
            HeapFlags.None,
            &textureDesc,
            this.CurrentResourceState,
            null
        );

        //The description for the shader resource view of the texture
        ShaderResourceViewDesc srvDesc = new ShaderResourceViewDesc {
            Shader4ComponentMapping = this.Shader4ComponentMapping(0, 1, 2, 3),
            Format                  = textureDesc.Format,
            ViewDimension           = SrvDimension.Texture2D
        };
        srvDesc.Anonymous.Texture2D.MipLevels = textureDesc.MipLevels;

        //Get the heap that this texture is stored in
        this.Heap = this._backend.CbvSrvUavHeap;
        //Get the slot in the heap this texture is in
        this.SRVHeapSlot = this._backend.CbvSrvUavHeap.GetSlot();
        //Get the handles of this slot
        (CpuDescriptorHandle Cpu, GpuDescriptorHandle Gpu) handles =
            this._backend.CbvSrvUavHeap.GetHandlesForSlot(this.SRVHeapSlot);

        //Create the shader resource view for this texture
        this._backend.Device.CreateShaderResourceView(this.Texture, &srvDesc, handles.Cpu);

        this.Texture.SetName(renderTarget ? "rendertarget" : "texture");

        //Get the slot and heap for the sampler
        this.SamplerHeap     = this._backend.SamplerHeap;
        this.SamplerHeapSlot = this.SamplerHeap.GetSlot();

        //Get the CPU and GPU handles for the sampler
        handles = this.SamplerHeap.GetHandlesForSlot(this.SamplerHeapSlot);

        //Set the details of the sampler
        SamplerDesc samplerDesc = new SamplerDesc {
            Filter = Filter.MinMagMipLinear, //TODO: set the filter properly, and re-use samplers as much as possible
            //                                       currently we create a new sampler for every texture, when in D3D12, 
            //                                       samplers arent bound to textures, so we can re-use them
            AddressU       = TextureAddressMode.Wrap,
            AddressW       = TextureAddressMode.Wrap,
            AddressV       = TextureAddressMode.Wrap,
            ComparisonFunc = ComparisonFunc.Never,
            MinLOD         = 0,
            MipLODBias     = 0,
            MaxLOD         = float.MaxValue,
            MaxAnisotropy  = 16
        };

        this._backend.Device.CreateSampler(in samplerDesc, handles.Cpu);
    }
    
    public ResourceStates CurrentResourceState { get; private set; }

    public unsafe void BarrierTransition(ResourceStates stateTo) {
        //Dont barrier transition if we are *already* in said state
        if (this.CurrentResourceState == stateTo)
            return; //NOTE: should this be allowed? i dont see a reason but maybe there is

        //Tell the command list that this resource is now in use for `stateTo` purpose
        ResourceBarrier copyBarrier = new ResourceBarrier {
            Type = ResourceBarrierType.Transition
        };
        copyBarrier.Anonymous.Transition.PResource   = this.Texture;
        copyBarrier.Anonymous.Transition.Subresource = 0;
        copyBarrier.Anonymous.Transition.StateAfter  = stateTo;
        copyBarrier.Anonymous.Transition.StateBefore = this.CurrentResourceState;
        this._backend.CommandList.ResourceBarrier(1, &copyBarrier);

        this.CurrentResourceState = stateTo;
    }

    public override TextureFilterType FilterType {
        get;
        set;
    }

    public override bool Mipmaps {
        get;
    }

    public override VixieTexture SetData <T>(ReadOnlySpan<T> data) {
        this.SetData(data, new Rectangle(0, 0, this.Width, this.Height));

        return this;
    }

    public override VixieTexture SetData <T>(ReadOnlySpan<T> data, Rectangle rect) {
        this.BarrierTransition(ResourceStates.CopyDest);

        //Create the subresource footprint of the texture
        SubresourceFootprint footprint = new SubresourceFootprint {
            Format   = Format.FormatR8G8B8A8Unorm,
            Width    = (uint)rect.Width,
            Height   = (uint)rect.Height,
            Depth    = 1,
            RowPitch = Direct3D12Backend.Align((uint)(rect.Width * sizeof(Rgba32)), D3D12.TextureDataPitchAlignment)
        };

        //The size of our upload buffer
        ulong uploadBufferSize = (ulong)(footprint.RowPitch * rect.Height);

        //The description of the upload buffer
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

        //The heap properties of the upload buffer, being of type `Upload`
        HeapProperties uploadBufferHeapProperties = new HeapProperties {
            Type                 = HeapType.Upload,
            CPUPageProperty      = CpuPageProperty.None,
            CreationNodeMask     = 0,
            VisibleNodeMask      = 0,
            MemoryPoolPreference = MemoryPool.None
        };
        //Create the upload buffer
        ComPtr<ID3D12Resource> uploadBuffer = this._backend.Device.CreateCommittedResource<ID3D12Resource>(
            &uploadBufferHeapProperties,
            HeapFlags.None,
            &uploadBufferDesc,
            ResourceStates.GenericRead,
            null
        );
        uploadBuffer.SetName("upload buf tex");
        
        //Declare the pointer which will point to our mapped data
        void* mapBegin = null;

        //Map the resource with no read range (since we are only going to be writing)
        SilkMarshal.ThrowHResult(uploadBuffer.Map(0, new Range(0, 0), &mapBegin));

        //Create the placed subresource footprint of the texture
        PlacedSubresourceFootprint placedTexture2D = new PlacedSubresourceFootprint {
            Offset    = 0,
            Footprint = footprint
        };

        fixed (void* dataPtr = data) {
            Span<Rgba32> rgbaSpan = new Span<Rgba32>(dataPtr, data.Length * sizeof(T));
            for (int y = 0; y < rect.Height; y++) {
                rgbaSpan.Slice(rect.Width * y, rect.Width)
                        .CopyTo(
                             new Span<Rgba32>(
                                 (void*)((nint)mapBegin + (nint)placedTexture2D.Offset + y * footprint.RowPitch), rect.Width));
            }
        }

        //Unmap the buffer
        uploadBuffer.Unmap(0, (Range*)null);

        //Copy the upload buffer into the texture, uploading the whole texture
        this._backend.CommandList.CopyTextureRegion(
            new TextureCopyLocation(
                this.Texture,
                TextureCopyType.SubresourceIndex,
                new TextureCopyLocationUnion(null, 0),
                null,
                0
            ),
            (uint)rect.X, (uint)rect.Y, 0,
            new TextureCopyLocation(
                uploadBuffer,
                TextureCopyType.PlacedFootprint,
                new TextureCopyLocationUnion(placedTexture2D),
                placedTexture2D
            ),
            null
        );
        
        //Release the upload buffer as we no longer need it
        this._backend.GraphicsItemsToGo.Push(uploadBuffer);
        
        this.BarrierTransition(ResourceStates.PixelShaderResource);
        
        return this;
    }

    public override Rgba32[] GetData() {
        this.BarrierTransition(ResourceStates.CopySource);
        
        //Create the subresource footprint of the texture
        SubresourceFootprint footprint = new SubresourceFootprint {
            Format   = Format.FormatR8G8B8A8Unorm,
            Width    = (uint)this.Width,
            Height   = (uint)this.Height,
            Depth    = 1,
            RowPitch = Direct3D12Backend.Align((uint)(this.Width * sizeof(Rgba32)), D3D12.TextureDataPitchAlignment)
        };

        //The size of our download buffer
        ulong readbackBufferSize = footprint.RowPitch * footprint.Height; 
        
        //The description of the upload buffer
        ResourceDesc readbackBufferDesc = new ResourceDesc {
            Dimension        = ResourceDimension.Buffer,
            Width            = readbackBufferSize,
            Height           = 1,
            Format           = Format.FormatUnknown,
            DepthOrArraySize = 1,
            Flags            = ResourceFlags.None,
            MipLevels        = 1,
            SampleDesc       = new SampleDesc(1, 0),
            Layout           = TextureLayout.LayoutRowMajor
        };
        
        //The heap properties of the upload buffer, being of type `Upload`
        HeapProperties readbackBufferHeapProperties = new HeapProperties {
            Type                 = HeapType.Readback,
            CPUPageProperty      = CpuPageProperty.None,
            CreationNodeMask     = 0,
            VisibleNodeMask      = 0,
            MemoryPoolPreference = MemoryPool.None
        };
        //Create the upload buffer
        ComPtr<ID3D12Resource> readbackBuffer = this._backend.Device.CreateCommittedResource<ID3D12Resource>(
            &readbackBufferHeapProperties,
            HeapFlags.None,
            &readbackBufferDesc,
            ResourceStates.CopyDest,
            null
        );
        readbackBuffer.SetName("texture readback buffer");
        
        //Create the placed subresource footprint of the texture
        PlacedSubresourceFootprint placedTexture2D = new PlacedSubresourceFootprint {
            Offset    = 0,
            Footprint = footprint
        }; 
        
        //Copy the texture to the readback buffer
        this._backend.CommandList.CopyTextureRegion(
            new TextureCopyLocation(
                readbackBuffer,
                TextureCopyType.PlacedFootprint,
                new TextureCopyLocationUnion(placedTexture2D),
                placedTexture2D
            ),
            0, 0, 0,
            new TextureCopyLocation(
                this.Texture,
                TextureCopyType.SubresourceIndex,
                new TextureCopyLocationUnion(null, 0),
                null,
                0
            ),
            null
        );
        
        this._backend.EndAndExecuteCommandList();
        this._backend.FenceCommandList();
        this._backend.ResetCommandListAndAllocator();
        this._backend.SetCommandListProps();
        
        //Declare the pointer which will point to our mapped data
        void* mapBegin = null;

        //Map the resource
        SilkMarshal.ThrowHResult(readbackBuffer.Map(0, new Range(0, (nuint?)readbackBufferSize), &mapBegin));

        Rgba32[]     pixData     = new Rgba32[this.Width * this.Height];
        Span<Rgba32> pixDataSpan = pixData;
        
        //Copy the data from the map into the buffer
        for (int y = 0; y < this.Height; y++) {
            new Span<Rgba32>(
                (void*)((nint)mapBegin + (nint)placedTexture2D.Offset + y * footprint.RowPitch), this.Width)
               .CopyTo(pixDataSpan.Slice(y * this.Width));
        }
        
        readbackBuffer.Unmap(0, new Range(0, 0));
        
        //Send the readback buffer to be disposed
        this._backend.GraphicsItemsToGo.Push(readbackBuffer);
        
        this.BarrierTransition(ResourceStates.PixelShaderResource);

        return pixData;
    }

    public override void CopyTo(VixieTexture tex) {
        throw new NotImplementedException();
    }
}