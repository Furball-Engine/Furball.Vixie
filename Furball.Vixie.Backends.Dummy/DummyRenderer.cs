using System;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;

namespace Furball.Vixie.Backends.Dummy;

public class DummyRenderer : Renderer {
    private unsafe void* _dataHandle;
    public override unsafe void Begin() {
        // throw new System.NotImplementedException();
        this._dataHandle = (void*)Marshal.AllocHGlobal(sizeof(Vertex) * 4 * 128);
    }
    public override void End() {
        // throw new System.NotImplementedException();
    }
    public override unsafe MappedData Reserve(ushort vertexCount, uint indexCount) {
        return new MappedData((Vertex*)this._dataHandle, (ushort*)this._dataHandle, vertexCount, indexCount, 0);
    }
    public override long GetTextureId(VixieTexture tex) {
        return 0;
    }
    public override void Draw() {
        // throw new System.NotImplementedException();
    }
    protected override unsafe void DisposeInternal() {
        // throw new System.NotImplementedException();
        Marshal.FreeHGlobal((IntPtr)this._dataHandle);
    }
}