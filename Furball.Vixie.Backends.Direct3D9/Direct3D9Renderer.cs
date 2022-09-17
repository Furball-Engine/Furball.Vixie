using System;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Vortice.Direct3D9;

namespace Furball.Vixie.Backends.Direct3D9; 

public unsafe class Direct3D9Renderer : Renderer {
    private readonly IDirect3DDevice9 _device;

    public override void Begin() {

    }

    public override void End() {

    }

    public override MappedData Reserve(ushort vertexCount, uint indexCount) {
        return new MappedData();
    }

    public override long GetTextureId(VixieTexture tex) {
        return 0;
    }

    public override void Draw() {

    }
    protected override void DisposeInternal() {

    }
}