using System;
using System.IO;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Kettu;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.WebGPU;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;

namespace Furball.Vixie.Backends.WebGPU;

public unsafe class WebGPUBackend : GraphicsBackend {
    private Silk.NET.WebGPU.WebGPU _webgpu;

    private Instance* _instance;
    private Adapter*  _adapter;
    private Device*   _device;
    
    private Surface*  _surface;

    public override void Initialize(IView view, IInputContext inputContext) {
#if USE_IMGUI
            throw new NotImplementedException();
#endif

        this._webgpu = Silk.NET.WebGPU.WebGPU.GetApi();

        this._instance = this._webgpu.CreateInstance(new InstanceDescriptor());

        this._surface = view.CreateWebGPUSurface(this._webgpu, this._instance);

        RequestAdapterOptions adapterOptions = new RequestAdapterOptions {
            PowerPreference = PowerPreference.HighPerformance,
            CompatibleSurface = this._surface
        };

        this._webgpu.InstanceRequestAdapter(
            this._instance,
            adapterOptions,
            new
                PfnRequestAdapterCallback(
                    (response, adapter, message, _) => {
                        Logger.Log($"Got adapter {(ulong)adapter:X} [{response}], with message {SilkMarshal.PtrToString((nint)message)}", LoggerLevelWebGPU.InstanceInfo);
                        
                        if (response != RequestAdapterStatus.Success)
                            throw new Exception("Unable to get adapter!");

                        this._adapter = adapter;
                    }),
            null
        );

        RequiredLimits* requiredLimits = stackalloc RequiredLimits[1] {
            new RequiredLimits {
                Limits = new Limits {
                    MaxBindGroups = 1
                }
            }
        };

        DeviceDescriptor deviceDescriptor = new DeviceDescriptor {
            RequiredLimits = requiredLimits,
        };

        this._webgpu.AdapterRequestDevice(
            this._adapter,
            deviceDescriptor,
            new PfnRequestDeviceCallback((response, device, message, _) => {
                Logger.Log($"Got device {(ulong)device:X} [{response}], with message {SilkMarshal.PtrToString((nint)message)}", LoggerLevelWebGPU.InstanceInfo);
                        
                if (response != RequestDeviceStatus.Success)
                    throw new Exception("Unable to get device!");

                this._device = device;
            }),
            null
        );
    }

    public override void Cleanup() {
        this._webgpu.Dispose();
    }

    public override void HandleFramebufferResize(int width, int height) {
        throw new NotImplementedException();
    }

    public override VixieRenderer CreateRenderer() {
        throw new NotImplementedException();
    }

    public override int QueryMaxTextureUnits() {
        throw new NotImplementedException();
    }

    public override void Clear() {
        throw new NotImplementedException();
    }

    public override void TakeScreenshot() {
        throw new NotImplementedException();
    }

    public override Rectangle ScissorRect {
        get;
        set;
    }

    public override void SetFullScissorRect() {
        throw new NotImplementedException();
    }

    public override ulong GetVramUsage() {
        throw new NotImplementedException();
    }

    public override ulong GetTotalVram() {
        throw new NotImplementedException();
    }

    public override VixieTextureRenderTarget CreateRenderTarget(uint width, uint height) {
        throw new NotImplementedException();
    }

    public override VixieTexture CreateTextureFromByteArray(byte[] imageData, TextureParameters parameters = default) {
        throw new NotImplementedException();
    }

    public override VixieTexture CreateTextureFromStream(Stream stream, TextureParameters parameters = default) {
        throw new NotImplementedException();
    }

    public override VixieTexture CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default) {
        throw new NotImplementedException();
    }

    public override VixieTexture CreateWhitePixelTexture() {
        throw new NotImplementedException();
    }

#if USE_IMGUI
        public override void ImGuiUpdate(double deltaTime) {
            throw new NotImplementedException();
        }
        public override void ImGuiDraw(double deltaTime) {
            throw new NotImplementedException();
        }
#endif
}