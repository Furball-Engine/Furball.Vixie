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

        this.CreateSwapchain();
    }
    
    private void CreateSwapchain() {
        
    }

    public override void Cleanup() {
        this._webgpu.Dispose();
    }

    public override void HandleFramebufferResize(int width, int height) {
        this.CreateSwapchain();
    }

    public override VixieRenderer CreateRenderer() {
        throw new NotImplementedException();
    }

    public override int QueryMaxTextureUnits() {
        throw new NotImplementedException();
    }

    public override void Clear() {
        //TODO: implement arbitrary clearing of the screen
        //This is not as simple in WebGPU as in OpenGL
        //To clear the full screen in WebGPU we need to do it at the START of a render pass, it cannot be done at any time
        //This means we need to do it in the renderer, so if there is no scissor set:
        //  - Set a flag backend wide (probably being this.ClearASAP), which will tell the renderer that:
        //    next time it creates a render pass, to set the load op to clear
        //If there *is* a scissor set, the logic is a bit more complicated:
        //  - Create a render pass with the load op set to 'none' and the store op set to 'clear'
        //  - Draw a quad with the area of the scissor rect, the details of the quad do not matter, as long as the pixels
        //    dont get discarded in the shader (so a quad with color[1, 1, 1, 1] is ideal)
    }

    public override void TakeScreenshot() {
        throw new NotImplementedException();
    }

    public override Rectangle ScissorRect {
        get;
        set;
    }

    public override void SetFullScissorRect() {
        //TODO
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