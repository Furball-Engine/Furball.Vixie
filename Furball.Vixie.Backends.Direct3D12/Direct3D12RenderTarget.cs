using Furball.Vixie.Backends.Direct3D12.Abstractions;
using Furball.Vixie.Backends.Shared;
using Silk.NET.Direct3D12;
using Silk.NET.Maths;

namespace Furball.Vixie.Backends.Direct3D12;

public sealed unsafe class Direct3D12RenderTarget : VixieTextureRenderTarget {
    private readonly Direct3D12Backend _backend;

    public readonly Direct3D12DescriptorHeap RtvHeap;
    public readonly int                      RtvHeapSlot;

    private readonly (CpuDescriptorHandle Cpu, GpuDescriptorHandle Gpu) Handles;

    private readonly Direct3D12Texture _texture;

    public Direct3D12RenderTarget(Direct3D12Backend backend, int width, int height) {
        this._backend = backend;

        this.Size = new Vector2D<int>(width, height);

        this.RtvHeap     = this._backend.RtvHeap;
        this.RtvHeapSlot = this._backend.RtvHeap.GetSlot();

        this.Handles = this.RtvHeap.GetHandlesForSlot(this.RtvHeapSlot);

        this._texture = new Direct3D12Texture(this._backend, width, height, default, true);

        //TODO: figure out why we get the error 
        //Debug Message: GPU-BASED VALIDATION: Draw, Incompatible resource state: Resource: 0x0000028FA2A3BB40:'textame', Subresource Index: [0], Descriptor heap index to DescriptorTableStart: [0], Descriptor
        //heap index FromTableStart: [0], Binding Type In Descriptor: SRV, Resource State: D3D12_RESOURCE_STATE_RENDER_TARGET(0x4), Index of Descriptor Range: 0, Shader Stage: PIXEL, Root Parameter Index: [1],
        // Draw Index: [0], Shader Code: Shader.hlsl(57,12-12), Asm Instruction Range: [0x1-0xffffffff], Asm Operand Index: [1], Command List: 0x0000028F9B7DF390:'cmdlame', SRV/UAV/CBV Descriptor Heap: 0x00000
        //28FA6C20D90:'CbvSrvU', Sampler Descriptor Heap: 0x0000028FA2B58440:'Sample', Pipeline State: 0x0000028FA6C09500:'Unnamed ID3D12PipelineState Object',
        this._backend.Device.CreateRenderTargetView(this._texture.Texture, null, this.Handles.Cpu);
    }

    public override Vector2D<int> Size {
        get;
        protected set;
    }

    public override void Bind() {
        this._backend.SetRenderTarget(this.Handles.Cpu);
    }

    public override void Unbind() {
        this._backend.SetRenderTarget(default);
    }

    public override VixieTexture GetTexture() => this._texture;
}