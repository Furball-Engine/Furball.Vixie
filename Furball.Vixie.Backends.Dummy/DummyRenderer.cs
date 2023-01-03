using System;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Kettu;
using Silk.NET.Core.Native;

namespace Furball.Vixie.Backends.Dummy;

public class DummyVixieRenderer : VixieRenderer {
    private unsafe void* _dataHandle;
    public DummyVixieRenderer(DummyBackend backend) {
        Logger.Log("Creating Dummy renderer!", LoggerLevelDummy.InstanceInfo);
    }
    public override unsafe void Begin(CullFace cullFace = CullFace.CCW) {
        // throw new System.NotImplementedException();
        this._dataHandle = (void*)SilkMarshal.Allocate(sizeof(Vertex) * 4 * 128);
    }
    public override void End() {
        // throw new System.NotImplementedException();
    }
    public override unsafe MappedData Reserve(ushort vertexCount, uint indexCount, VixieTexture tex) {
        return new MappedData((Vertex*)this._dataHandle, (ushort*)this._dataHandle, vertexCount, indexCount, 0, this
                                 .GetTextureId(tex));
    }
    private long GetTextureId(VixieTexture tex) {
        return 0;
    }
    public override void Draw() {
        // throw new System.NotImplementedException();
    }
    protected override unsafe void DisposeInternal() {
        // throw new System.NotImplementedException();
        SilkMarshal.Free((IntPtr)this._dataHandle);
    }
}