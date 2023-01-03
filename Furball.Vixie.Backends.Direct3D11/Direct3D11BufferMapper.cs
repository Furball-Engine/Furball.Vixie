using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace Furball.Vixie.Backends.Direct3D11;

public class Direct3D11BufferMapper : BufferMapper {
    private readonly Direct3D11Backend _backend;

    private readonly BindFlag _usage;

    internal unsafe ComPtr<ID3D11Buffer> Buffer = new ComPtr<ID3D11Buffer>((ID3D11Buffer*)null);

    public unsafe void* Ptr;

    public Direct3D11BufferMapper(Direct3D11Backend backend, uint byteSize, BindFlag usage) {
        this._backend = backend;

        Guard.Assert(
        (usage & ~(BindFlag.IndexBuffer | BindFlag.VertexBuffer)) == 0,
        "(usage & ~(BindFlags.IndexBuffer | BindFlags.VertexBuffer)) == 0"
        );

        this._usage = usage;

        this.SizeInBytes = byteSize;
    }

    public unsafe ComPtr<ID3D11Buffer> ResetFromExistingBuffer(ComPtr<ID3D11Buffer> buffer) {
        this.ReservedBytes = 0;

        //Get the current buffer
        ComPtr<ID3D11Buffer> old = this.Buffer;

        BufferDesc desc = new BufferDesc();
        buffer.GetDesc(ref desc);
        //Ensure the buffer is dynamic
        Guard.Assert(
        desc.Usage.HasFlag(Usage.Dynamic),
        "buffer.Description.Usage.HasFlag(ResourceUsage.Dynamic)"
        );
        //Ensure the bind flags are correct
        Guard.Assert((BindFlag)desc.BindFlags == this._usage, "buffer.Description.BindFlags == this.Usage");

        //Set the current buffer to the new one
        this.Buffer = buffer;

        //Unmap the old buffer
        if (old.Handle != null)
            this._backend.DeviceContext.Unmap(old, 0);

        //Map the new buffer
        MappedSubresource map = new MappedSubresource();
        SilkMarshal.ThrowHResult(this._backend.DeviceContext.Map(this.Buffer, 0u, Silk.NET.Direct3D11.Map.WriteDiscard, 0, &map));

        //Set the data ptr
        this.Ptr = map.PData;

        return old;
    }

    public unsafe ComPtr<ID3D11Buffer> ResetFromFreshBuffer() {
        BufferDesc desc = new BufferDesc((uint?)this.SizeInBytes, Usage.Dynamic, (uint?)this._usage) {
            CPUAccessFlags = (uint)CpuAccessFlag.Write
        };
        ComPtr<ID3D11Buffer> buf = null;
        SilkMarshal.ThrowHResult(this._backend.Device.CreateBuffer(in desc, null, ref buf));

        return this.ResetFromExistingBuffer(buf);
    }

    public override void Map() {
        Guard.Fail("We should not be here! Use the `Reset*` methods!");
    }

    public override void Unmap() {
        Guard.EnsureNonNull(this.Buffer, "this.Buffer");

        //Unmap the buffer
        this._backend.DeviceContext.Unmap(this.Buffer, 0);

        this.Buffer.Dispose();

        //Say we no longer are using any buffers
        this.Buffer = null;
    }

    public override unsafe void* Reserve(nuint byteCount) {
        nuint ptr = (nuint)this.Ptr + this.ReservedBytes;

        //If this reserve will push us over the limit, return nullptr
        if (this.ReservedBytes + byteCount > this.SizeInBytes)
            return null;

        this.ReservedBytes += byteCount;

        return (void*)ptr;
    }

    protected override void DisposeInternal() {
        this.Buffer.Dispose();
    }
}