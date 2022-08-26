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

    public override MappedData Reserve(ushort vertexCount, uint indexCount) => throw new NotImplementedException();

    public override long GetTextureId(VixieTexture tex) => throw new NotImplementedException();

    public override void Draw() {

    }
    protected override void DisposeInternal() {

    }
}