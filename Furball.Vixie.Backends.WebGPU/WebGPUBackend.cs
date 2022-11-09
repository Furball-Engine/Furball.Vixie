﻿using System;
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
using Color = Silk.NET.WebGPU.Color;

namespace Furball.Vixie.Backends.WebGPU;

public unsafe class WebGPUBackend : GraphicsBackend {
    public Silk.NET.WebGPU.WebGPU WebGPU;

    public int NumQueuesSubmit;
    public bool ClearASAP;
    
    private IView _view;

    public Instance* _instance;
    public Adapter*  _adapter;
    public  Device*   Device;

    private TextureFormat _swapchainFormat;

    private Surface*     _surface;
    private SwapChain*   Swapchain;
    private TextureView* SwapchainTextureView;


    public override void Initialize(IView view, IInputContext inputContext) {
        this._view = view;

        //These parameters are required for the WebGPU backend
        this._view.IsContextControlDisabled = true;  //Stop Silk from managing the GL context, WebGPU does that
        this._view.ShouldSwapAutomatically  = false; //Stop silk from attempting to swap buffers, we do that

#if USE_IMGUI
            throw new NotImplementedException();
#endif

        this.WebGPU = Silk.NET.WebGPU.WebGPU.GetApi();

        this._instance = this.WebGPU.CreateInstance(new InstanceDescriptor());

        this._surface = view.CreateWebGPUSurface(this.WebGPU, this._instance);

        RequestAdapterOptions adapterOptions = new RequestAdapterOptions {
            PowerPreference   = PowerPreference.HighPerformance,
            CompatibleSurface = this._surface
        };

        this.WebGPU.InstanceRequestAdapter(
            this._instance,
            adapterOptions,
            new
                PfnRequestAdapterCallback(
                    (response, adapter, message, _) => {
                        Logger.Log(
                            $"Got adapter {(ulong)adapter:X} [{response}], with message \"{SilkMarshal.PtrToString((nint)message)}\"",
                            LoggerLevelWebGPU.InstanceInfo);

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

        this.WebGPU.AdapterRequestDevice(
            this._adapter,
            deviceDescriptor,
            new PfnRequestDeviceCallback((response, device, message, _) => {
                Logger.Log(
                    $"Got device {(ulong)device:X} [{response}], with message \"{SilkMarshal.PtrToString((nint)message)}\"",
                    LoggerLevelWebGPU.InstanceInfo);

                if (response != RequestDeviceStatus.Success)
                    throw new Exception("Unable to get device!");

                this.Device = device;
            }),
            null
        );

        this.SetCallbacks();

        this._swapchainFormat = this.WebGPU.SurfaceGetPreferredFormat(this._surface, this._adapter);

        this.CreateSwapchain();
    }

    private void SetCallbacks() {
        this.WebGPU.DeviceSetDeviceLostCallback(this.Device, new PfnDeviceLostCallback(this.DeviceLostCallback),
                                                 null);
        this.WebGPU.DeviceSetUncapturedErrorCallback(this.Device, new PfnErrorCallback(this.ErrorCallback), null);
    }

    private void DeviceLostCallback(DeviceLostReason reason, byte* message, void* userData) {
        Logger.Log($"Device Lost! Reason: {reason}, Message: {SilkMarshal.PtrToString((nint)message)}",
                   LoggerLevelWebGPU.InstanceCallbackError);
    }

    private void ErrorCallback(ErrorType errorType, byte* message, void* userData) {
        Logger.Log($"{errorType}: {SilkMarshal.PtrToString((nint)message)}", LoggerLevelWebGPU.InstanceCallbackError);
    }

    private void CreateSwapchain() {
        SwapChainDescriptor descriptor = new SwapChainDescriptor {
            Usage       = TextureUsage.RenderAttachment,
            Format      = this._swapchainFormat,
            PresentMode = PresentMode.Fifo,
            Width       = (uint)this._view.FramebufferSize.X,
            Height      = (uint)this._view.FramebufferSize.Y
        };

        this.Swapchain = this.WebGPU.DeviceCreateSwapChain(this.Device, this._surface, descriptor);

        Logger.Log($"Created swapchain with width {descriptor.Width} and height {descriptor.Height}",
                   LoggerLevelWebGPU.InstanceInfo);
    }

    public override void BeginScene() {
        base.BeginScene();

        this.SwapchainTextureView = null;

        for (int attempt = 0; attempt < 2; attempt++) {
            this.SwapchainTextureView = this.WebGPU.SwapChainGetCurrentTextureView(this.Swapchain);

            if (attempt == 0 && this.SwapchainTextureView == null) {
                Logger.Log(
                    "SwapChainGetCurrentTextureView failed; trying to create a new swap chain...",
                    LoggerLevelWebGPU.InstanceWarning
                );
                this.CreateSwapchain();
                continue;
            }

            break;
        }
    }

    public override void EndScene() {
        base.EndScene();

        if (this.NumQueuesSubmit != 0)
            return;
        
        //NOTE: this shouldn't be required, but due to an issue in wgpu, it is, as things break if no work is submitted
        //once https://github.com/gfx-rs/wgpu/issues/3189 is fixed, this can be removed
        
        CommandEncoder* encoder = this.WebGPU.DeviceCreateCommandEncoder(this.Device, new CommandEncoderDescriptor());

        RenderPassColorAttachment colorAttachment = new RenderPassColorAttachment {
            View          = this.SwapchainTextureView,
            LoadOp        = this.ClearASAP ? LoadOp.Clear : LoadOp.Load,
            StoreOp       = StoreOp.Store,
            ResolveTarget = null, ClearValue = new Color(0, 0, 0, 0)
        };

        RenderPassDescriptor renderPassDescriptor = new RenderPassDescriptor {
            ColorAttachments       = &colorAttachment,
            ColorAttachmentCount   = 1,
            DepthStencilAttachment = null
        };

        RenderPassEncoder* renderPass = this.WebGPU.CommandEncoderBeginRenderPass(encoder, renderPassDescriptor);
        this.WebGPU.RenderPassEncoderEnd(renderPass);

        CommandBuffer* commandBuffer = this.WebGPU.CommandEncoderFinish(encoder, new CommandBufferDescriptor());

        Queue* queue = this.WebGPU.DeviceGetQueue(this.Device);
        this.WebGPU.QueueSubmit(queue, 1, &commandBuffer);

        //This code clears the screen, so reset this flag
        this.ClearASAP = false;
    }

    public override void Present() {
        base.Present();

        this.WebGPU.SwapChainPresent(this.Swapchain);

        this.NumQueuesSubmit = 0;
        
        this.ClearASAP = false;
    }

    public override void Cleanup() {
        this.WebGPU.Dispose();
    }

    public override void HandleFramebufferResize(int width, int height) {
        this.CreateSwapchain();
    }

    public override VixieRenderer CreateRenderer() {
        throw new NotImplementedException();
    }

    public override int QueryMaxTextureUnits() {
        //This is a technique called: lying!
        return 0;
    }

    public override void Clear() {
        this.ClearASAP = true;

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
        return 0;
    }

    public override ulong GetTotalVram() {
        return 0;
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
        return new WebGPUTexture(this, (int)width, (int)height, parameters);
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