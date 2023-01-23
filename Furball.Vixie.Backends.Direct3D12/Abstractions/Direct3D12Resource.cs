using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Furball.Vixie.Backends.Direct3D12.Abstractions;

public abstract class Direct3D12Resource {
    protected Direct3D12Backend _backend;

    public ComPtr<ID3D12Resource> Resource;

    public ResourceStates CurrentResourceState { get; protected set; }

    public unsafe void BarrierTransition(ResourceStates stateTo, uint subresource = 0) {
        //Dont barrier transition if we are *already* in said state
        if (this.CurrentResourceState == stateTo)
            return; //NOTE: should this be allowed? i dont see a reason but maybe there is

        //Tell the command list that this resource is now in use for `stateTo` purpose
        ResourceBarrier copyBarrier = new ResourceBarrier {
            Type = ResourceBarrierType.Transition
        };
        copyBarrier.Anonymous.Transition.PResource   = this.Resource;
        copyBarrier.Anonymous.Transition.Subresource = subresource;
        copyBarrier.Anonymous.Transition.StateAfter  = stateTo;
        copyBarrier.Anonymous.Transition.StateBefore = this.CurrentResourceState;
        this._backend.CommandList.ResourceBarrier(1, &copyBarrier);

        this.CurrentResourceState = stateTo;
    }
}