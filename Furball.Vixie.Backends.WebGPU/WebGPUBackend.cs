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

    private IView _view;

    private Instance* _instance;
    private Adapter*  _adapter;
    private Device*   _device;
    
    private TextureFormat _swapchainFormat;
    
    private Surface*      _surface;
    private SwapChain*    Swapchain;
    
    public override void Initialize(IView view, IInputContext inputContext) {
        this._view = view;
        
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
                        Logger.Log($"Got adapter {(ulong)adapter:X} [{response}], with message \"{SilkMarshal.PtrToString((nint)message)}\"", LoggerLevelWebGPU.InstanceInfo);
                        
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
                Logger.Log($"Got device {(ulong)device:X} [{response}], with message \"{SilkMarshal.PtrToString((nint)message)}\"", LoggerLevelWebGPU.InstanceInfo);
                        
                if (response != RequestDeviceStatus.Success)
                    throw new Exception("Unable to get device!");

                this._device = device;
            }),
            null
        );

        this.SetCallbacks();
        
        this._swapchainFormat = this._webgpu.SurfaceGetPreferredFormat(this._surface, this._adapter);
        
        this.CreateSwapchain();
    }
    
    private void SetCallbacks() {
        this._webgpu.DeviceSetUncapturedErrorCallback(this._device, new PfnErrorCallback(this.ErrorCallback), null);
    }
    
    private void ErrorCallback(ErrorType errorType, byte* message, void* userData) {
        Logger.Log($"{errorType}: {SilkMarshal.PtrToString((nint)message)}", LoggerLevelWebGPU.InstanceCallbackError);
    }

    private void CreateSwapchain() {
        SwapChainDescriptor descriptor = new SwapChainDescriptor {
            Usage       = TextureUsage.RenderAttachment,
            Format      = this._swapchainFormat,
            PresentMode = PresentMode.Fifo,
            Width       = (uint)this._view.Size.X,
            Height      = (uint)this._view.Size.Y
        };

        this.Swapchain = this._webgpu.DeviceCreateSwapChain(this._device, this._surface, descriptor);
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
        // OPTION 1:
        //  - Create a render pass with the load op set to 'none' and the store op set to 'clear'
        //  - Draw a quad with the area of the scissor rect, the details of the quad do not matter, as long as the pixels
        //    dont get discarded in the shader (so a quad with color[1, 1, 1, 1] is ideal)
        // OPTION 2:
        //  - At the start of the next render pass, set the pipeline to one specifically for clearing, that pipeline
        //    has specific alpha settings, to allow us to draw a color[0, 0, 0, 0] quad, which will clear the screen
        //    then after, switch the pipeline to the normal rendering one
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