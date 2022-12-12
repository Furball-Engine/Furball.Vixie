using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Backends.Shared.TextureEffects.Blur;
using Furball.Vixie.Helpers.Helpers;
using Kettu;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.Disposal;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Buffer = Silk.NET.WebGPU.Buffer;
using Color = Silk.NET.WebGPU.Color;
using Rectangle=SixLabors.ImageSharp.Rectangle;

namespace Furball.Vixie.Backends.WebGPU;

public unsafe class WebGPUBackend : GraphicsBackend {
    public Silk.NET.WebGPU.WebGPU WebGPU;

    public int  NumQueuesSubmit;
    public bool ClearAsap;

    private IView _view;

    public Instance* Instance;
    public Adapter*  Adapter;
    public Device*   Device;
    public Queue*    Queue;

    private TextureFormat _swapchainFormat;

    private Surface*     _surface;
    public  SwapChain*   Swapchain;
    public  TextureView* SwapchainTextureView;

    public Sampler* LinearSampler;
    public Sampler* NearestSampler;

    public BindGroupLayout* TextureSamplerBindGroupLayout;

    public Buffer*          ProjectionMatrixBuffer;
    public BindGroupLayout* ProjectionMatrixBindGroupLayout;
    public BindGroup*       ProjectionMatrixBindGroup;

    public PipelineLayout* PipelineLayout;
    public RenderPipeline* Pipeline;

    public ShaderModule* Shader;

    public WebGPUDisposal Disposal;

    public override void Initialize(IView view, IInputContext inputContext) {
        this._view = view;

        //These parameters are required for the WebGPU backend
        this._view.IsContextControlDisabled = true;  //Stop Silk from managing the GL context, WebGPU does that
        this._view.ShouldSwapAutomatically  = false; //Stop silk from attempting to swap buffers, we do that

#if USE_IMGUI
        // throw new NotImplementedException();
#endif

        this.WebGPU = Silk.NET.WebGPU.WebGPU.GetApi();

        this.Instance = this.WebGPU.CreateInstance(new InstanceDescriptor());

        this._surface = view.CreateWebGPUSurface(this.WebGPU, this.Instance);

        RequestAdapterOptions adapterOptions = new RequestAdapterOptions {
            PowerPreference   = PowerPreference.HighPerformance,
            CompatibleSurface = this._surface
        };

        this.WebGPU.InstanceRequestAdapter(
            this.Instance,
            adapterOptions,
            new
                PfnRequestAdapterCallback(
                    (response, adapter, message, _) => {
                        Logger.Log(
                            $"Got adapter {(ulong)adapter:X} [{response}], with message \"{SilkMarshal.PtrToString((nint)message)}\"",
                            LoggerLevelWebGPU.InstanceInfo);

                        if (response != RequestAdapterStatus.Success)
                            throw new Exception("Unable to get adapter!");

                        this.Adapter = adapter;
                    }),
            null
        );

        SupportedLimits supported = new SupportedLimits();
        
        BackendInfoSection supportedLimitsSection = new BackendInfoSection("Supported Limits");
        if (this.WebGPU.AdapterGetLimits(this.Adapter, &supported)) {
            supportedLimitsSection.Contents.Add(("MaxBindGroups", supported.Limits.MaxBindGroups.ToString()));
            supportedLimitsSection.Contents.Add(("MaxBufferSize", supported.Limits.MaxBufferSize.ToString()));
            supportedLimitsSection.Contents.Add(("MaxVertexBuffers", supported.Limits.MaxVertexBuffers.ToString()));
            supportedLimitsSection.Contents.Add(("MaxVertexAttributes", supported.Limits.MaxVertexAttributes.ToString()));
            supportedLimitsSection.Contents.Add(("MaxVertexBufferArrayStride", supported.Limits.MaxVertexBufferArrayStride.ToString()));
            supportedLimitsSection.Contents.Add(("MaxDynamicUniformBuffersPerPipelineLayout", supported.Limits.MaxDynamicUniformBuffersPerPipelineLayout.ToString()));
            supportedLimitsSection.Contents.Add(("MaxDynamicStorageBuffersPerPipelineLayout", supported.Limits.MaxDynamicStorageBuffersPerPipelineLayout.ToString()));
            supportedLimitsSection.Contents.Add(("MaxSampledTexturesPerShaderStage", supported.Limits.MaxSampledTexturesPerShaderStage.ToString()));
            supportedLimitsSection.Contents.Add(("MaxSamplersPerShaderStage", supported.Limits.MaxSamplersPerShaderStage.ToString()));
            supportedLimitsSection.Contents.Add(("MaxStorageBuffersPerShaderStage", supported.Limits.MaxStorageBuffersPerShaderStage.ToString()));
            supportedLimitsSection.Contents.Add(("MaxStorageTexturesPerShaderStage", supported.Limits.MaxStorageTexturesPerShaderStage.ToString()));
            supportedLimitsSection.Contents.Add(("MaxUniformBuffersPerShaderStage", supported.Limits.MaxUniformBuffersPerShaderStage.ToString()));
            supportedLimitsSection.Contents.Add(("MaxUniformBufferBindingSize", supported.Limits.MaxUniformBufferBindingSize.ToString()));
            supportedLimitsSection.Contents.Add(("MaxStorageBufferBindingSize", supported.Limits.MaxStorageBufferBindingSize.ToString()));
            supportedLimitsSection.Contents.Add(("MaxInterStageShaderComponents", supported.Limits.MaxInterStageShaderComponents.ToString()));
            supportedLimitsSection.Contents.Add(("MaxComputeWorkgroupStorageSize", supported.Limits.MaxComputeWorkgroupStorageSize.ToString()));
            supportedLimitsSection.Contents.Add(("MaxComputeInvocationsPerWorkgroup", supported.Limits.MaxComputeInvocationsPerWorkgroup.ToString()));
            supportedLimitsSection.Contents.Add(("MaxComputeWorkgroupSizeX", supported.Limits.MaxComputeWorkgroupSizeX.ToString()));
            supportedLimitsSection.Contents.Add(("MaxComputeWorkgroupSizeY", supported.Limits.MaxComputeWorkgroupSizeY.ToString()));
            supportedLimitsSection.Contents.Add(("MaxComputeWorkgroupSizeZ", supported.Limits.MaxComputeWorkgroupSizeZ.ToString()));
            supportedLimitsSection.Contents.Add(("MaxComputeWorkgroupsPerDimension", supported.Limits.MaxComputeWorkgroupsPerDimension.ToString()));
            supportedLimitsSection.Contents.Add(("MaxTextureDimension1D", supported.Limits.MaxTextureDimension1D.ToString()));
            supportedLimitsSection.Contents.Add(("MaxTextureDimension2D", supported.Limits.MaxTextureDimension2D.ToString()));
            supportedLimitsSection.Contents.Add(("MaxTextureDimension3D", supported.Limits.MaxTextureDimension3D.ToString()));
            supportedLimitsSection.Contents.Add(("MaxTextureArrayLayers", supported.Limits.MaxTextureArrayLayers.ToString()));
            supportedLimitsSection.Contents.Add(("MaxColorAttachments", supported.Limits.MaxColorAttachments.ToString()));
        }
        this.InfoSections.Add(supportedLimitsSection);

        RequiredLimits* requiredLimits = stackalloc RequiredLimits[1] {
            new RequiredLimits {
                Limits = new Limits {
                    MaxBindGroups =
                        2 //We use 2 bind groups, one for the projection matrix, one for the sampler and texture
                }
            }
        };

        DeviceDescriptor deviceDescriptor = new DeviceDescriptor {
            RequiredLimits = requiredLimits
        };

        this.WebGPU.AdapterRequestDevice(
            this.Adapter,
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

        this.Queue = this.WebGPU.DeviceGetQueue(this.Device);

        this.SetCallbacks();

        this._swapchainFormat = this.WebGPU.SurfaceGetPreferredFormat(this._surface, this.Adapter);

        this.CreateProjectionMatrixBuffer();

        this.CreateSamplers();
        this.CreateShaders();
        this.CreatePipelines();
        
        this.Disposal = new WebGPUDisposal(this.WebGPU);
        
        this.InfoSections.ForEach(x => x.Log(LoggerLevelWebGPU.InstanceInfo));
    }

    private void CreateShaders() {
        ShaderModuleWGSLDescriptor wgslDescriptor = new ShaderModuleWGSLDescriptor {
            Code = (byte*)SilkMarshal.StringToPtr(
                ResourceHelpers.GetStringResource("Shaders/Shader.wgsl", typeof(WebGPUBackend))
            ),
            Chain = new ChainedStruct {
                SType = SType.ShaderModuleWgsldescriptor
            }
        };

        this.Shader = this.WebGPU.DeviceCreateShaderModule(this.Device, new ShaderModuleDescriptor {
            NextInChain = (ChainedStruct*)(&wgslDescriptor)
        });

        //Free the shader code, we dont want that permanently in memory, that would be simply silly
        SilkMarshal.FreeString((nint)wgslDescriptor.Code);

        Logger.Log(
            $"Created Shader {(ulong)this.Shader:X}",
            LoggerLevelWebGPU.InstanceInfo
        );
    }

    private void CreateProjectionMatrixBuffer() {
        this.ProjectionMatrixBuffer = this.WebGPU.DeviceCreateBuffer(this.Device, new BufferDescriptor {
            Size             = (ulong)sizeof(Matrix4x4),
            Usage            = BufferUsage.Uniform | BufferUsage.CopyDst,
            MappedAtCreation = false
        });

        BindGroupLayoutEntry bindGroupLayoutEntry = new BindGroupLayoutEntry {
            Buffer = new BufferBindingLayout {
                Type           = BufferBindingType.Uniform,
                MinBindingSize = (ulong)sizeof(Matrix4x4)
            },
            Binding    = 0,
            Visibility = ShaderStage.Vertex
        };

        this.ProjectionMatrixBindGroupLayout = this.WebGPU.DeviceCreateBindGroupLayout(
            this.Device,
            new BindGroupLayoutDescriptor {
                Entries    = &bindGroupLayoutEntry,
                EntryCount = 1
            }
        );

        BindGroupEntry bindGroupEntry = new BindGroupEntry {
            Binding = 0,
            Buffer  = this.ProjectionMatrixBuffer,
            Size    = (ulong)sizeof(Matrix4x4)
        };

        this.ProjectionMatrixBindGroup = this.WebGPU.DeviceCreateBindGroup(this.Device, new BindGroupDescriptor {
            Entries    = &bindGroupEntry,
            EntryCount = 1,
            Layout     = this.ProjectionMatrixBindGroupLayout
        });
    }

    private void CreatePipelines() {
        BindGroupLayoutEntry* textureSamplerBindGroupLayoutEntries = stackalloc BindGroupLayoutEntry[2];
        textureSamplerBindGroupLayoutEntries[0] = new BindGroupLayoutEntry {
            Binding = 0,
            Texture = new TextureBindingLayout {
                Multisampled  = false,
                SampleType    = TextureSampleType.Float,
                ViewDimension = TextureViewDimension.TextureViewDimension2D
            },
            Visibility = ShaderStage.Fragment
        };
        textureSamplerBindGroupLayoutEntries[1] = new BindGroupLayoutEntry {
            Binding = 1,
            Sampler = new SamplerBindingLayout {
                Type = SamplerBindingType.Filtering
            },
            Visibility = ShaderStage.Fragment
        };

        this.TextureSamplerBindGroupLayout = this.WebGPU.DeviceCreateBindGroupLayout(
            this.Device, new BindGroupLayoutDescriptor {
                Entries    = textureSamplerBindGroupLayoutEntries,
                EntryCount = 2
            });

        BindGroupLayout** bindGroupLayouts = stackalloc BindGroupLayout*[2];
        bindGroupLayouts[0] = this.TextureSamplerBindGroupLayout;
        bindGroupLayouts[1] = this.ProjectionMatrixBindGroupLayout;

        this.PipelineLayout = this.WebGPU.DeviceCreatePipelineLayout(this.Device, new PipelineLayoutDescriptor {
            BindGroupLayouts     = bindGroupLayouts,
            BindGroupLayoutCount = 2
        });

        BlendState blendState = new BlendState {
            Color = new BlendComponent {
                SrcFactor = BlendFactor.SrcAlpha,
                DstFactor = BlendFactor.OneMinusSrcAlpha,
                Operation = BlendOperation.Add
            },
            Alpha = new BlendComponent {
                SrcFactor = BlendFactor.One,
                DstFactor = BlendFactor.OneMinusSrcAlpha,
                Operation = BlendOperation.Add
            }
        };

        ColorTargetState colorTargetState = new ColorTargetState {
            Blend     = &blendState,
            Format    = this._swapchainFormat,
            WriteMask = ColorWriteMask.All
        };

        FragmentState fragmentState = new FragmentState {
            Module      = this.Shader,
            Targets     = &colorTargetState,
            TargetCount = 1,
            EntryPoint  = (byte*)SilkMarshal.StringToPtr("fs_main") //TODO: free this
        };

        VertexAttribute* vertexAttributes = stackalloc VertexAttribute[4];

        //Position
        vertexAttributes[0] = new VertexAttribute {
            Format         = VertexFormat.Float32x2,
            Offset         = (ulong)Marshal.OffsetOf<Vertex>(nameof (Vertex.Position)),
            ShaderLocation = 0
        };
        //Texture coord
        vertexAttributes[1] = new VertexAttribute {
            Format         = VertexFormat.Float32x2,
            Offset         = (ulong)Marshal.OffsetOf<Vertex>(nameof (Vertex.TextureCoordinate)),
            ShaderLocation = 1
        };
        //Vertex color
        vertexAttributes[2] = new VertexAttribute {
            Format         = VertexFormat.Float32x4,
            Offset         = (ulong)Marshal.OffsetOf<Vertex>(nameof (Vertex.Color)),
            ShaderLocation = 2
        };
        //Texture index
        vertexAttributes[3] = new VertexAttribute {
            Format         = VertexFormat.Uint32x2, //Note: this is a ulong, not 2 uints, but theres no u64 in WGSL
            Offset         = (ulong)Marshal.OffsetOf<Vertex>(nameof (Vertex.TexId)),
            ShaderLocation = 3
        };

        VertexBufferLayout vertexBufferLayout = new VertexBufferLayout {
            Attributes     = vertexAttributes,
            AttributeCount = 4,
            StepMode       = VertexStepMode.Vertex,
            ArrayStride    = (ulong)sizeof(Vertex)
        };

        this.Pipeline = this.WebGPU.DeviceCreateRenderPipeline(this.Device, new RenderPipelineDescriptor {
            Layout   = this.PipelineLayout,
            Fragment = &fragmentState,
            Vertex = new VertexState {
                Buffers     = &vertexBufferLayout,
                BufferCount = 1,
                Module      = this.Shader,
                EntryPoint  = (byte*)SilkMarshal.StringToPtr("vs_main") //TODO: free this
            },
            Multisample = new MultisampleState {
                Count                  = 1,
                Mask                   = ~0u,
                AlphaToCoverageEnabled = false
            },
            Primitive = new PrimitiveState {
                CullMode  = CullMode.Back,
                Topology  = PrimitiveTopology.TriangleList,
                FrontFace = FrontFace.Ccw
            },
            DepthStencil = null
        });

        Logger.Log(
            $"Created Pipeline {(ulong)this.Pipeline:X}",
            LoggerLevelWebGPU.InstanceInfo
        );
    }

    private void CreateSamplers() {
        this.LinearSampler = this.WebGPU.DeviceCreateSampler(this.Device, new SamplerDescriptor {
            AddressModeU = AddressMode.Repeat,
            AddressModeV = AddressMode.Repeat,
            AddressModeW = AddressMode.Repeat,
            Compare      = CompareFunction.Undefined,
            MagFilter    = FilterMode.Linear,
            MinFilter    = FilterMode.Linear,
            MipmapFilter = MipmapFilterMode.Linear
        });
        this.NearestSampler = this.WebGPU.DeviceCreateSampler(this.Device, new SamplerDescriptor {
            AddressModeU = AddressMode.Repeat,
            AddressModeV = AddressMode.Repeat,
            AddressModeW = AddressMode.Repeat,
            Compare      = CompareFunction.Undefined,
            MagFilter    = FilterMode.Nearest,
            MinFilter    = FilterMode.Nearest,
            MipmapFilter = MipmapFilterMode.Nearest
        });

        Logger.Log(
            $"Created Linear Sampler {(ulong)this.LinearSampler:X} and Nearest Sampler {(ulong)this.NearestSampler:X}",
            LoggerLevelWebGPU.InstanceInfo
        );
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
            PresentMode = PresentMode.Immediate,
            Width       = (uint)this._view.FramebufferSize.X,
            Height      = (uint)this._view.FramebufferSize.Y
        };

        this.Swapchain = this.WebGPU.DeviceCreateSwapChain(this.Device, this._surface, descriptor);

        Logger.Log($"Created swapchain with width {descriptor.Width} and height {descriptor.Height}",
                   LoggerLevelWebGPU.InstanceInfo);
    }

    public CommandEncoder* CommandEncoder;

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

        this.CommandEncoder = this.WebGPU.DeviceCreateCommandEncoder(this.Device, new CommandEncoderDescriptor());
    }

    public override void EndScene() {
        base.EndScene();

        if (this.ClearAsap) {
            //NOTE: this shouldn't be required, but due to an issue in wgpu, it is, as things break if no work is submitted
            //once https://github.com/gfx-rs/wgpu/issues/3189 is fixed, this can be removed

            CommandEncoder* encoder = this.WebGPU.DeviceCreateCommandEncoder(
            this.Device,
            new CommandEncoderDescriptor()
            );

            RenderPassColorAttachment colorAttachment = new RenderPassColorAttachment {
                View          = this.SwapchainTextureView,
                LoadOp        = this.ClearAsap ? LoadOp.Clear : LoadOp.Load,
                StoreOp       = StoreOp.Store,
                ResolveTarget = null,
                ClearValue    = new Color(0, 0, 0, 0)
            };

            RenderPassDescriptor renderPassDescriptor = new RenderPassDescriptor {
                ColorAttachments       = &colorAttachment,
                ColorAttachmentCount   = 1,
                DepthStencilAttachment = null
            };

            RenderPassEncoder* renderPass = this.WebGPU.CommandEncoderBeginRenderPass(encoder, renderPassDescriptor);
            this.WebGPU.RenderPassEncoderEnd(renderPass);

            CommandBuffer* clearCommandBuffer = this.WebGPU.CommandEncoderFinish(encoder, new CommandBufferDescriptor());

            this.AddCommandBuffer(clearCommandBuffer);

            //This code clears the screen, so reset this flag
            this.ClearAsap = false;
        }
        
        CommandBuffer* commandBuffer = this.WebGPU.CommandEncoderFinish(this.CommandEncoder, new CommandBufferDescriptor());
        this.AddCommandBuffer(commandBuffer);
        
        fixed(CommandBuffer** buffers = this._commandBuffers)
            this.WebGPU.QueueSubmit(this.Queue, this._commandBuffersUsed, buffers);
        
        // Console.WriteLine(this._commandBuffersUsed);

        this._commandBuffersUsed = 0;
    }

    private CommandBuffer*[] _commandBuffers = new CommandBuffer*[100];
    private uint             _commandBuffersUsed;
    public void AddCommandBuffer(CommandBuffer* commandBuffer) {
        if (this._commandBuffersUsed == this._commandBuffers.Length) {
            //Resize _commandBuffers array to be 10 larger than it currently is if we dont have the space for any more
            
            CommandBuffer*[] newCommandBuffers = new CommandBuffer*[this._commandBuffers.Length + 10];
            Array.Copy(this._commandBuffers, newCommandBuffers, this._commandBuffers.Length);
            this._commandBuffers = newCommandBuffers;
        }
        
        this._commandBuffers[this._commandBuffersUsed++] = commandBuffer;
    }

    public override void Present() {
        base.Present();

        this.WebGPU.SwapChainPresent(this.Swapchain);
        
        this.Disposal.Dispose(this.SwapchainTextureView);

        this.NumQueuesSubmit = 0;

        this.ClearAsap = false;
    }

    public override void Cleanup() {
        this.WebGPU.Dispose();
    }

    public void UpdateProjectionMatrix(float width, float height, bool fbProjMatrix) {
        float right  = fbProjMatrix ? width : width / (float)height * 720f;
        float bottom = fbProjMatrix ? height : 720f;

        Matrix4x4 projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, right, bottom, 0,
                                                                           0, 1); 
        
        this.WebGPU.QueueWriteBuffer(this.Queue, this.ProjectionMatrixBuffer, 0, &projectionMatrix, (nuint)sizeof(Matrix4x4));
    }
    
    public override void HandleFramebufferResize(int width, int height) {
        this.CreateSwapchain();
        this.UpdateProjectionMatrix(width, height, false);
    }

    public override VixieRenderer CreateRenderer() {
        return new WebGPURenderer(this);
    }
    public override BoxBlurTextureEffect CreateBoxBlurTextureEffect(VixieTexture source) {
        try {
            return new OpenCLBoxBlurTextureEffect(this, source);
        }
        catch {
            return new CpuBoxBlurTextureEffect(this, source);
        }
    }
    public override Vector2D<int> MaxTextureSize {
        get {
            SupportedLimits limits = new SupportedLimits();
            return this.WebGPU.AdapterGetLimits(this.Adapter, ref limits) 
                       ? new Vector2D<int>((int)limits.Limits.MaxTextureDimension2D) 
                       : new Vector2D<int>(1024); //if it fails for whatever reason, just assume 1024
        }
    }

    public override void Clear() {
        this.ClearAsap = true;

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

    public override VixieTexture CreateTextureFromByteArray(byte[]            imageData,
                                                            TextureParameters parameters = default(TextureParameters)) {
        Image<Rgba32> image;

        bool qoi = imageData.Length > 3 && imageData[0] == 'q' && imageData[1] == 'o' && imageData[2] == 'i' &&
                   imageData[3]     == 'f';

        if (qoi) {
            (Rgba32[] pixels, QoiLoader.QoiHeader header) data = QoiLoader.Load(imageData);

            image = Image.LoadPixelData(data.pixels, (int)data.header.Width, (int)data.header.Height);
        }
        else {
            image = Image.Load<Rgba32>(imageData);
        }

        WebGPUTexture texture = new WebGPUTexture(this, image.Width, image.Height, parameters);

        image.ProcessPixelRows(x => {
            for (int y = 0; y < x.Height; y++) {
                texture.SetData<Rgba32>(x.GetRowSpan(y), new System.Drawing.Rectangle(0, y, x.Width, 1));
            }
        });

        return texture;
    }

    public override VixieTexture CreateTextureFromStream(Stream            stream,
                                                         TextureParameters parameters = default(TextureParameters)) {
        Image<Rgba32> image = Image.Load<Rgba32>(stream);

        WebGPUTexture texture = new WebGPUTexture(this, image.Width, image.Height, parameters);

        image.ProcessPixelRows(x => {
            for (int y = 0; y < x.Height; y++) {
                texture.SetData<Rgba32>(x.GetRowSpan(y), new System.Drawing.Rectangle(0, y, x.Width, 1));
            }
        });

        return texture;
    }

    public override VixieTexture CreateEmptyTexture(uint              width, uint height,
                                                    TextureParameters parameters = default(TextureParameters)) {
        return new WebGPUTexture(this, (int)width, (int)height, parameters);
    }

    public override VixieTexture CreateWhitePixelTexture() {
        WebGPUTexture texture = new WebGPUTexture(this, 1, 1, new TextureParameters());

        //Set the data to one pixel of all white
        texture.SetData<byte>(new byte[] { 255, 255, 255, 255 });

        return texture;
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