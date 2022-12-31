using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Furball.Vixie.Backends.Direct3D12.Abstractions;

public unsafe class Direct3D12DescriptorHeap : IDisposable {
    public static uint DefaultSlotAmount = 2048;

    private readonly Direct3D12Backend _backend;

    public ComPtr<ID3D12DescriptorHeap> Heap;

    private uint _slots;

    public int UsedSlots = 0;

    public Direct3D12DescriptorHeap(Direct3D12Backend backend, DescriptorHeapType type) :
        this(backend, type, DefaultSlotAmount) {
    }

    public Direct3D12DescriptorHeap(Direct3D12Backend backend, DescriptorHeapType type, uint slots) {
        this._backend = backend;
        this._slots   = slots;

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
    }

    public int GetSlot() {
        int slot = this.UsedSlots;
        this.UsedSlots++;

        return slot;
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