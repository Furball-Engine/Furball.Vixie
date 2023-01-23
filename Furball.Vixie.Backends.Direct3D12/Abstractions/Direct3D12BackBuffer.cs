using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Furball.Vixie.Backends.Direct3D12.Abstractions; 

public class Direct3D12BackBuffer : Direct3D12Resource, IDisposable {
    public Direct3D12BackBuffer(Direct3D12Backend backend, ComPtr<ID3D12Resource> resource) {
        this.Resource             = resource;
        this.CurrentResourceState = ResourceStates.Present;
        this._backend             = backend;
    }
    
    public void Dispose() {
        this.Resource.Dispose();
    }
}