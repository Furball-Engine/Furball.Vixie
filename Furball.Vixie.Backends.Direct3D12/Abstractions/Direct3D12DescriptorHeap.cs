using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Furball.Vixie.Backends.Direct3D12.Abstractions;

public unsafe class Direct3D12DescriptorHeap : IDisposable {
    public static uint DefaultSamplerSlotAmount   = 2048;
    public static uint DefaultCbvSrvUavSlotAmount = 1048576;

    private readonly Direct3D12Backend  _backend;
    private readonly DescriptorHeapType _type;

    public ComPtr<ID3D12DescriptorHeap> Heap;

    private uint _slots;

    public int UsedSlots = 0;

    private readonly uint                _slotSize;
    private readonly CpuDescriptorHandle CpuHandle;
    private readonly GpuDescriptorHandle GpuHandle;

    public Direct3D12DescriptorHeap(Direct3D12Backend backend, DescriptorHeapType type, uint slots) {
        this._backend = backend;
        this._slots   = slots;
        this._type    = type;

        DescriptorHeapDesc desc = new DescriptorHeapDesc {
            Flags = type switch {
                DescriptorHeapType.Sampler   => DescriptorHeapFlags.ShaderVisible,
                DescriptorHeapType.CbvSrvUav => DescriptorHeapFlags.ShaderVisible,
                _                            => DescriptorHeapFlags.None
            },
            Type           = type,
            NumDescriptors = slots
        };

        this.Heap = backend.Device.CreateDescriptorHeap<ID3D12DescriptorHeap>(in desc);

        ID3D12DescriptorHeap* heap = this.Heap;
        
        this.CpuHandle = heap->GetCPUDescriptorHandleForHeapStart();
        this.GpuHandle = heap->GetGPUDescriptorHandleForHeapStart();
        
        this._slotSize = this._backend.Device.GetDescriptorHandleIncrementSize(this._type);
    }

    public int GetSlot() {
        int slot = this.UsedSlots;
        this.UsedSlots++;

        return slot;
    }

    public (CpuDescriptorHandle Cpu, GpuDescriptorHandle Gpu) GetHandlesForSlot(int slot) {
        return (
            new CpuDescriptorHandle(this.CpuHandle.Ptr + (nuint)(this._slotSize * slot)),
            new GpuDescriptorHandle(this.GpuHandle.Ptr + (nuint)(this._slotSize * slot))
        );
    }

    public int GetSlotFromHandle(CpuDescriptorHandle handle) {
        nuint diff = handle.Ptr - this.CpuHandle.Ptr;

        return (int)(diff / this._slotSize);
    }
    
    public int GetSlotFromHandle(GpuDescriptorHandle handle) {
        ulong diff = handle.Ptr - this.GpuHandle.Ptr;

        return (int)(diff / this._slotSize);
    }

    private void ReleaseUnmanagedResources() {
        if (this.Heap.Handle != null)
            this.Heap.Dispose();
    }

    private void Dispose(bool disposing) {
        this.ReleaseUnmanagedResources();
    }

    public void Dispose() {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Direct3D12DescriptorHeap() {
        this.Dispose(false);
    }
}