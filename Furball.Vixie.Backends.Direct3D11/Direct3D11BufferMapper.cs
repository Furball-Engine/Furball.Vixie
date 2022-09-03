using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Vortice.Direct3D11;

namespace Furball.Vixie.Backends.Direct3D11;

public class Direct3D11BufferMapper : BufferMapper {
    private readonly Direct3D11Backend _backend;

    private readonly BindFlags _usage;

    internal ID3D11Buffer? Buffer;

    public unsafe void* Ptr;

    public Direct3D11BufferMapper(Direct3D11Backend backend, uint byteSize, BindFlags usage) {
        this._backend = backend;

        Guard.Assert(
        (usage & ~(BindFlags.IndexBuffer | BindFlags.VertexBuffer)) == 0,
        "(usage & ~(BindFlags.IndexBuffer | BindFlags.VertexBuffer)) == 0"
        );

        this._usage = usage;

        this.SizeInBytes = byteSize;
    }

    public unsafe ID3D11Buffer? ResetFromExistingBuffer(ID3D11Buffer buffer) {
        this.ReservedBytes = 0;

        //Get the current buffer
        ID3D11Buffer? old = this.Buffer;

        //Ensure the buffer is dynamic
        Guard.Assert(
        buffer.Description.Usage.HasFlag(ResourceUsage.Dynamic),
        "buffer.Description.Usage.HasFlag(ResourceUsage.Dynamic)"
        );
        //Ensure the bind flags are correct
        Guard.Assert(buffer.Description.BindFlags == this._usage, "buffer.Description.BindFlags == this.Usage");

        //Set the current buffer to the new one
        this.Buffer = buffer;

        //Unmap the old buffer
        if (old != null)
            this._backend.DeviceContext.Unmap(old);

        //Map the new buffer
        MappedSubresource map = this._backend.DeviceContext.Map(this.Buffer, MapMode.WriteDiscard);

        //Set the data ptr
        this.Ptr = (void*)map.DataPointer;

        return old;
    }

    public ID3D11Buffer? ResetFromFreshBuffer() {
        BufferDescription desc = new((int)this.SizeInBytes, this._usage, ResourceUsage.Dynamic) {
            CPUAccessFlags = CpuAccessFlags.Write
        };
        ID3D11Buffer buf = this._backend.Device.CreateBuffer(desc);

        return this.ResetFromExistingBuffer(buf);
    }

    public override void Map() {
        Guard.Fail("We should not be here! Use the `Reset*` methods!");
    }

    public override void Unmap() {
        Guard.EnsureNonNull(this.Buffer, "this.Buffer");

        //Unmap the buffer
        this._backend.DeviceContext.Unmap(this.Buffer!);

        this.Buffer!.Dispose();

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
        this.Buffer?.Dispose();
    }
}