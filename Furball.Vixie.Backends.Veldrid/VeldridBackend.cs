using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Backends.Veldrid.Abstractions;
using Silk.NET.Input;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Extensions.Veldrid;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Veldrid;
using Veldrid.MetalBindings;
using Rectangle=SixLabors.ImageSharp.Rectangle;
using Texture=Veldrid.Texture;

namespace Furball.Vixie.Backends.Veldrid; 

public class VeldridBackend : IGraphicsBackend {
    public static GraphicsBackend PrefferedBackend = VeldridWindow.GetPlatformDefaultBackend();
        
    internal GraphicsDevice  GraphicsDevice;
    internal ResourceFactory ResourceFactory;
    internal CommandList     CommandList;

    public GraphicsBackend ChosenBackend;
        
    internal Matrix4x4       ProjectionMatrix;
    private  IView           _view;
    private  ImGuiController _imgui;

    internal TextureVeldrid WhitePixel;
    internal ResourceSet    WhitePixelResourceSet;

    internal  Framebuffer    RenderFramebuffer;
    internal  Texture        MainFramebufferTexture;
    internal  ResourceSet    MainFramebufferTextureSet;
    internal  ResourceLayout MainFramebufferTextureLayout;
    internal  FullScreenQuad FullScreenQuad;
    private bool           _screenshotQueued;
    private Rectangle      _lastScissor;

    public override void Initialize(IView view, IInputContext inputContext) {
        this._view = view;
            
        GraphicsDeviceOptions options = new() {
            SyncToVerticalBlank               = view.VSync,
            Debug                             = view.API.Flags.HasFlag(ContextFlags.Debug),
            ResourceBindingModel              = ResourceBindingModel.Improved,
            PreferStandardClipSpaceYDirection = true
        };

        this.GraphicsDevice  = view.CreateGraphicsDevice(options, PrefferedBackend);
        this.ResourceFactory = this.GraphicsDevice.ResourceFactory;
        this.CommandList     = this.ResourceFactory.CreateCommandList();

        //we do a little trolling
        if(this.GraphicsDevice.BackendType is GraphicsBackend.OpenGL or GraphicsBackend.OpenGLES && !view.VSync) {
            this.GraphicsDevice.SyncToVerticalBlank = true;
            this.GraphicsDevice.SyncToVerticalBlank = false;
        }

        BackendInfoSection mainSection = new("General Info");
        mainSection.Contents.Add(("Backend", this.GraphicsDevice.BackendType.ToString()));
        mainSection.Contents.Add(("Vendor", this.GraphicsDevice.VendorName));
        this.InfoSections.Add(mainSection);

        this.ChosenBackend = this.GraphicsDevice.BackendType;
            
        BackendInfoSection backendSection = new("Backend Info");
        switch (this.GraphicsDevice.BackendType) {
            case GraphicsBackend.Direct3D11: {
                //we dont actually get anything useful from this :/
                BackendInfoD3D11 info = this.GraphicsDevice.GetD3D11Info();
                    
                backendSection.Contents.Add(("Device ID", info.DeviceId.ToString()));
                break;
            }
            case GraphicsBackend.Vulkan: {
                BackendInfoVulkan info = this.GraphicsDevice.GetVulkanInfo();
                    
                backendSection.Contents.Add(("Driver Name", info.DriverName));
                backendSection.Contents.Add(("Driver Info", info.DriverInfo));

                ReadOnlyCollection<BackendInfoVulkan.ExtensionProperties> availableDeviceExtensions = info.AvailableDeviceExtensions;
                foreach (BackendInfoVulkan.ExtensionProperties extension in availableDeviceExtensions) {
                    backendSection.Contents.Add(($"Available Extension {extension.Name}", $"Version {extension.SpecVersion}"));
                }

                ReadOnlyCollection<string> availableLayers = info.AvailableInstanceLayers;
                foreach (string layer in availableLayers) {
                    backendSection.Contents.Add(("Available Layer", layer));
                }

                break;
            }
            case GraphicsBackend.OpenGLES:
            case GraphicsBackend.OpenGL: {
                BackendInfoOpenGL info = this.GraphicsDevice.GetOpenGLInfo();

                backendSection.Contents.Add(("OpenGL Version", info.Version));
                backendSection.Contents.Add(("GLSL Version", info.ShadingLanguageVersion));

                ReadOnlyCollection<string> extensions = info.Extensions;
                foreach (string extension in extensions) {
                    backendSection.Contents.Add(("Available Extension", extension));
                }
                    
                break;
            }
            case GraphicsBackend.Metal: {
                BackendInfoMetal info = this.GraphicsDevice.GetMetalInfo();

                ReadOnlyCollection<MTLFeatureSet> featureSetList = info.FeatureSet;

                foreach (MTLFeatureSet featureSet in featureSetList) {
                    backendSection.Contents.Add(("Available FeatureSet", featureSet.ToString()));
                    // Logger.Log($"Metal Feature Set Available {featureSet}", LoggerLevelVeldrid.InstanceInfo);
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
        this.InfoSections.Add(backendSection);
            
        #region Available features
        var features = this.GraphicsDevice.Features;
            
        BackendInfoSection section = new("Available Features");
        section.Contents.Add(("Compute Shader", features.ComputeShader.ToString()));
        section.Contents.Add(("Geometry Shader", features.GeometryShader.ToString()));
        section.Contents.Add(("DrawIndirect", features.DrawIndirect.ToString()));
        section.Contents.Add(("DrawBaseInstance", features.DrawBaseInstance.ToString()));
        section.Contents.Add(("DrawBaseVertex", features.DrawBaseVertex.ToString()));
        section.Contents.Add(("DrawIndirectBaseInstance", features.DrawIndirectBaseInstance.ToString()));
        section.Contents.Add(("Independent Blend", features.IndependentBlend.ToString()));
        section.Contents.Add(("Multiple Viewports", features.MultipleViewports.ToString()));
        section.Contents.Add(("Sampler Anisotropy", features.SamplerAnisotropy.ToString()));
        section.Contents.Add(("Shader f64 Support", features.ShaderFloat64.ToString()));
        section.Contents.Add(("Structured Buffers", features.StructuredBuffer.ToString()));
        section.Contents.Add(("Tessellation Shaders", features.TessellationShaders.ToString()));
        section.Contents.Add(("Texture1D", features.Texture1D.ToString()));
        section.Contents.Add(("BufferRangeBinding", features.BufferRangeBinding.ToString()));
        section.Contents.Add(("DepthClipDisable", features.DepthClipDisable.ToString()));
        section.Contents.Add(("FillModeWireframe", features.FillModeWireframe.ToString()));
        section.Contents.Add(("SamplerLodBias", features.SamplerLodBias.ToString()));
        section.Contents.Add(("SubsetTextureView", features.SubsetTextureView.ToString()));
        section.Contents.Add(("CommandListDebugMarkers", features.CommandListDebugMarkers.ToString()));
        this.InfoSections.Add(section);
        #endregion

        this.CreateFramebuffer(this.GraphicsDevice.SwapchainFramebuffer.Width, this.GraphicsDevice.SwapchainFramebuffer.Height);

        this._imgui = new ImGuiController(this.GraphicsDevice, this.RenderFramebuffer.OutputDescription, view, inputContext);

        for (int i = 0; i < MAX_TEXTURE_UNITS; i++) {
            ResourceLayout layout = this.ResourceFactory.CreateResourceLayout(new(new ResourceLayoutElementDescription[] {
                new($"tex_{i}", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new($"sampler_{i}", ResourceKind.Sampler, ShaderStages.Fragment)
            }));

            TextureVeldrid.ResourceLayouts[i] = layout;
        }
            
        ResourceLayout blankLayout = this.ResourceFactory.CreateResourceLayout(new(new ResourceLayoutElementDescription[] {
            new($"tex_blank", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new($"sampler_blank", ResourceKind.Sampler, ShaderStages.Fragment),
        }));

        this.WhitePixel = (TextureVeldrid)this.CreateWhitePixelTexture();
            
        this.WhitePixelResourceSet = this.ResourceFactory.CreateResourceSet(new(blankLayout, this.WhitePixel.Texture, this.GraphicsDevice.PointSampler));

        this.CommandList.Begin();
        this.FullScreenQuad = new FullScreenQuad(this);
        this.CommandList.End();
        this.GraphicsDevice.SubmitCommands(this.CommandList);
            
        this.InfoSections.ForEach(x => x.Log(LoggerLevelVeldrid.InstanceInfo));

        this._lastScissor = new Rectangle(0, 0, view.Size.X, view.Size.Y);
    }
        

    private void CreateFramebuffer(uint width, uint height) {
        this.RenderFramebuffer?.Dispose();
        this.MainFramebufferTexture?.Dispose();
        this.MainFramebufferTextureSet?.Dispose();
        this.MainFramebufferTextureLayout?.Dispose();

        this.MainFramebufferTexture = this.ResourceFactory.CreateTexture(TextureDescription.Texture2D(width, height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.RenderTarget | TextureUsage.Sampled));

        FramebufferDescription fbdesc = new FramebufferDescription {
            ColorTargets = new[] {
                new FramebufferAttachmentDescription(this.MainFramebufferTexture, 0)
            }
        };
        this.RenderFramebuffer = this.ResourceFactory.CreateFramebuffer(fbdesc);

        this.MainFramebufferTextureLayout = this.ResourceFactory.CreateResourceLayout(new(new[] {
            new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment), 
            new ResourceLayoutElementDescription("SourceSampler", ResourceKind.Sampler, ShaderStages.Fragment)
        }));
            
        var resourceSetDesc = new ResourceSetDescription {
            BoundResources = new BindableResource[] {
                this.MainFramebufferTexture,
                this.GraphicsDevice.PointSampler,
            }
        };
        resourceSetDesc.Layout = this.MainFramebufferTextureLayout;

        this.MainFramebufferTextureSet = this.ResourceFactory.CreateResourceSet(resourceSetDesc);
    }

    public override void Cleanup() {
        this.WhitePixelResourceSet.Dispose();
        this.WhitePixel.Dispose();
        this._imgui.Dispose();
        this.CommandList.Dispose();

        this.GraphicsDevice.Dispose();
            
    }
        
    internal void SetProjectionMatrix(uint width, uint height) {
        this.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width / (float)height * 720f, 720, 0, 1f, 0f);
    }

    public override void HandleFramebufferResize(int width, int height) {
        this.GraphicsDevice.ResizeMainWindow((uint)width, (uint)height);

        this.SetProjectionMatrix((uint)width, (uint)height);

        this.CreateFramebuffer(this.GraphicsDevice.SwapchainFramebuffer.Width, this.GraphicsDevice.SwapchainFramebuffer.Height);

        this.SetFullScissorRect();
    }
    public override IQuadRenderer CreateTextureRenderer() => new QuadRendererVeldrid(this);
    public override ILineRenderer CreateLineRenderer()    => new LineRendererVeldrid(this);

    public const int MAX_TEXTURE_UNITS = 4;
        
    public override int QueryMaxTextureUnits() {
        //this is a trick we call
        //lying
        return MAX_TEXTURE_UNITS;
    }

    public override void BeginScene() {
        this.CommandList.Begin();
        this.CommandList.SetFramebuffer(this.RenderFramebuffer);

        this.CommandList.SetFullViewports();
        this.SetProjectionMatrix(this.RenderFramebuffer.Width, this.RenderFramebuffer.Height);
    }

    public override void EndScene() {
        this.CommandList.End();
        this.GraphicsDevice.SubmitCommands(this.CommandList);
    }

    public void Flush() {
        this.EndScene();
        this.BeginScene();
    }
        
    public override void Present() {
        this.CommandList.Begin();
        this.CommandList.SetFramebuffer(this.GraphicsDevice.SwapchainFramebuffer);
        // this.CommandList.CopyTexture(this.MainFramebufferTexture, this.GraphicsDevice.SwapchainFramebuffer.ColorTargets[0].Target);
        this.FullScreenQuad.Render();
        this.CommandList.End();
        this.GraphicsDevice.SubmitCommands(this.CommandList);
            
        this.GraphicsDevice.SwapBuffers();

        if (this._screenshotQueued) {
            this._screenshotQueued = false;
                
            TextureDescription desc = TextureDescription.Texture2D(
                this.MainFramebufferTexture.Width, 
                this.MainFramebufferTexture.Height, 
                this.MainFramebufferTexture.MipLevels, 
                this.MainFramebufferTexture.ArrayLayers, 
                this.MainFramebufferTexture.Format, 
                TextureUsage.Staging
            );

            Texture? tex = this.ResourceFactory.CreateTexture(desc);
                
            this.CommandList.Begin();
            this.CommandList.CopyTexture(this.MainFramebufferTexture, tex);
            this.CommandList.End();
            this.GraphicsDevice.SubmitCommands(this.CommandList);

            MappedResource mapped = this.GraphicsDevice.Map(tex, MapMode.Read);
                
            byte[] bytes = new byte[mapped.SizeInBytes];
            Marshal.Copy(mapped.Data, bytes, 0, (int)mapped.SizeInBytes);

            Image img = Image.LoadPixelData<Rgba32>(bytes, (int)tex.Width, (int)tex.Height);
            this.GraphicsDevice.Unmap(tex);

            img = img.CloneAs<Rgb24>();

            if (!GraphicsDevice.IsUvOriginTopLeft) {
                img.Mutate(x => x.Flip(FlipMode.Vertical));
            }
                
            this.InvokeScreenshotTaken(img);
                
            tex.Dispose();
        }
    }

    public override void Clear() {
        this.CommandList.ClearColorTarget(0, RgbaFloat.Black);
    }
    public override void TakeScreenshot() {
        this._screenshotQueued = true;
            
    }

    public override Rectangle ScissorRect {
        get => this._lastScissor;
        set {
            this.CommandList.SetFramebuffer(this.RenderFramebuffer);
            this.CommandList.SetScissorRect(0, (uint)value.X, (uint)value.Y, (uint)value.Width, (uint)value.Height);
            this._lastScissor = value;
        }
    }
    public override void SetFullScissorRect() {
        CommandList commandList = this.ResourceFactory.CreateCommandList();
        commandList.Begin();
        this._lastScissor = new Rectangle(0, 0, this._view.Size.X, this._view.Size.Y);
        commandList.SetFramebuffer(this.RenderFramebuffer);
        commandList.SetFullScissorRect(0);
        commandList.End();
        this.GraphicsDevice.SubmitCommands(commandList);
        this.GraphicsDevice.WaitForIdle();
    }
    public override TextureRenderTarget CreateRenderTarget(uint width, uint height) => new TextureRenderTargetVeldrid(this, width, height);
        
    public override Shared.Texture CreateTexture(byte[] imageData, bool qoi = false) => new TextureVeldrid(this, imageData, qoi);

    public override Shared.Texture CreateTexture(Stream stream) => new TextureVeldrid(this, stream);

    public override Shared.Texture CreateTexture(uint width, uint height) => new TextureVeldrid(this, width, height);

    public override Shared.Texture CreateTexture(string filepath) => new TextureVeldrid(this, filepath);

    public override Shared.Texture CreateWhitePixelTexture() => new TextureVeldrid(this);
        
    public override void ImGuiUpdate(double deltaTime) {
        this._imgui.Update((float)deltaTime);
    }
    public override void ImGuiDraw(double deltaTime) {
        this._imgui.Render(this.GraphicsDevice, this.CommandList);
    }
}