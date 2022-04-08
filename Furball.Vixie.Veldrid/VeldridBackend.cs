using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Furball.Vixie.Graphics.Backends.Veldrid.Abstractions;
using Furball.Vixie.Graphics.Exceptions;
using Furball.Vixie.Graphics.Renderers;
using Kettu;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Extensions.Veldrid;
using Veldrid;
using Veldrid.MetalBindings;
using Vulkan;
using InputSnapshot=Silk.NET.Input.Extensions.InputSnapshot;

namespace Furball.Vixie.Graphics.Backends.Veldrid {
    public class VeldridBackend : IGraphicsBackend {
        public static global::Veldrid.GraphicsBackend PrefferedBackend = VeldridWindow.GetPlatformDefaultBackend();
        
        internal GraphicsDevice  GraphicsDevice;
        internal ResourceFactory ResourceFactory;
        internal CommandList     CommandList;
        
        internal Matrix4x4       ProjectionMatrix;
        private  IWindow         _window;
        private  ImGuiController _imgui;

        internal ResourceLayout SamplerResourceLayout;
        internal ResourceSet    SamplerResourceSet;
        public   TextureVeldrid WhitePixel;
        public   ResourceSet    WhitePixelResourceSet;

        public Framebuffer             RenderFramebuffer;
        public global::Veldrid.Texture MainFramebufferTexture;
        public ResourceSet             MainFramebufferTextureSet;
        public ResourceLayout          MainFramebufferTextureLayout;
        public FullScreenQuad          FullScreenQuad;
        public override void Initialize(IWindow window, IInputContext inputContext) {
            this._window = window;
            
            GraphicsDeviceOptions options = new() {
                SyncToVerticalBlank               = window.VSync,
                Debug                             = window.API.Flags.HasFlag(ContextFlags.Debug),
                ResourceBindingModel              = ResourceBindingModel.Improved,
                PreferStandardClipSpaceYDirection = true
            };

            this.GraphicsDevice     = window.CreateGraphicsDevice(options, PrefferedBackend);
            this.ResourceFactory    = this.GraphicsDevice.ResourceFactory;
            this.CommandList = this.ResourceFactory.CreateCommandList();

            //we do a little trolling
            if(this.GraphicsDevice.BackendType is global::Veldrid.GraphicsBackend.OpenGL or global::Veldrid.GraphicsBackend.OpenGLES && !window.VSync) {
                this.GraphicsDevice.SyncToVerticalBlank = true;
                this.GraphicsDevice.SyncToVerticalBlank = false;
            }

            var features = this.GraphicsDevice.Features;
            Logger.Log(
            $"Available Features: ComputerShader:{features.ComputeShader} DrawIndirect:{features.DrawIndirect} GeometryShader:{features.GeometryShader} IndependentBlend:{features.IndependentBlend} MultipleViewports:{features.MultipleViewports} SamplerAnisotropy:{features.SamplerAnisotropy} ShaderFloat64:{features.ShaderFloat64} StructuredBuffer:{features.StructuredBuffer} TessellationShaders:{features.TessellationShaders} Texture1D:{features.Texture1D} BufferRangeBinding:{features.BufferRangeBinding} DepthClipDisable:{features.DepthClipDisable} DrawBaseInstance:{features.DrawBaseInstance} DrawBaseVertex:{features.DrawBaseVertex} FillModeWireframe:{features.FillModeWireframe} SamplerLodBias:{features.SamplerLodBias} SubsetTextureView:{features.SubsetTextureView} CommandListDebugMarkers:{features.CommandListDebugMarkers} DrawIndirectBaseInstance:{features.DrawIndirectBaseInstance}",
            LoggerLevelVeldrid.InstanceInfo);
            Logger.Log($"Using backend {this.GraphicsDevice.BackendType}", LoggerLevelVeldrid.InstanceInfo);
            Logger.Log($"Vendor Name: {this.GraphicsDevice.VendorName}",   LoggerLevelVeldrid.InstanceInfo);

            switch (this.GraphicsDevice.BackendType) {
                case global::Veldrid.GraphicsBackend.Direct3D11: {
                    //we dont actually get anything useful from this :/
                    BackendInfoD3D11 info = this.GraphicsDevice.GetD3D11Info();
                    
                    Logger.Log($"D3D11 Device ID: {info.DeviceId}", LoggerLevelVeldrid.InstanceInfo);
                    break;
                }
                case global::Veldrid.GraphicsBackend.Vulkan: {
                    BackendInfoVulkan info = this.GraphicsDevice.GetVulkanInfo();
                    
                    Logger.Log($"Vulkan Driver Name: {info.DriverName}", LoggerLevelVeldrid.InstanceInfo);
                    Logger.Log($"Vulkan Driver Info: {info.DriverInfo}",   LoggerLevelVeldrid.InstanceInfo);
                    
                    ReadOnlyCollection<BackendInfoVulkan.ExtensionProperties> availableDeviceExtensions = info.AvailableDeviceExtensions;
                    foreach (BackendInfoVulkan.ExtensionProperties extension in availableDeviceExtensions) 
                        Logger.Log($"Available Vulkan Extension {extension.Name}, Version:{extension.SpecVersion}", LoggerLevelVeldrid.InstanceInfo);
                   
                    ReadOnlyCollection<string> availableLayers = info.AvailableInstanceLayers;
                    foreach (string layer in availableLayers) 
                        Logger.Log($"Available Vulkan Layer {layer}", LoggerLevelVeldrid.InstanceInfo);

                    break;
                }
                case global::Veldrid.GraphicsBackend.OpenGLES:
                case global::Veldrid.GraphicsBackend.OpenGL: {
                    BackendInfoOpenGL info = this.GraphicsDevice.GetOpenGLInfo();

                    Logger.Log($"OpenGL Version: {info.Version}",              LoggerLevelVeldrid.InstanceInfo);
                    Logger.Log($"GLSL Version: {info.ShadingLanguageVersion}", LoggerLevelVeldrid.InstanceInfo);

                    ReadOnlyCollection<string> extensions = info.Extensions;
                    foreach (string extension in extensions) {
                        Logger.Log($"Available OpenGL Extension: {extension}", LoggerLevelVeldrid.InstanceInfo);
                    }
                    
                    break;
                }
                case global::Veldrid.GraphicsBackend.Metal: {
                    BackendInfoMetal info = this.GraphicsDevice.GetMetalInfo();

                    ReadOnlyCollection<MTLFeatureSet> featureSetList = info.FeatureSet;

                    foreach (MTLFeatureSet featureSet in featureSetList) {
                        Logger.Log($"Metal Feature Set Available {featureSet}", LoggerLevelVeldrid.InstanceInfo);
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            this.CreateFramebuffer(this.GraphicsDevice.SwapchainFramebuffer.Width, this.GraphicsDevice.SwapchainFramebuffer.Height);

            this._imgui = new ImGuiController(this.GraphicsDevice, this.RenderFramebuffer.OutputDescription, window, inputContext);

            for (int i = 0; i < MAX_TEXTURE_UNITS; i++) {
                ResourceLayout layout = this.ResourceFactory.CreateResourceLayout(new(new ResourceLayoutElementDescription[] {
                    new($"tex_{i}", ResourceKind.TextureReadOnly, ShaderStages.Fragment)
                }));

                TextureVeldrid.ResourceLayouts[i] = layout;
            }
            
            ResourceLayout blankLayout = this.ResourceFactory.CreateResourceLayout(new(new ResourceLayoutElementDescription[] {
                new($"tex_blank", ResourceKind.TextureReadOnly, ShaderStages.Fragment)
            }));

            this.WhitePixel = this.CreateWhitePixelTexture() as TextureVeldrid;
            
            this.WhitePixelResourceSet = this.ResourceFactory.CreateResourceSet(new(blankLayout, WhitePixel.Texture));

            this.SamplerResourceLayout = this.ResourceFactory.CreateResourceLayout(new(new ResourceLayoutElementDescription("TextureSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            this.SamplerResourceSet = this.ResourceFactory.CreateResourceSet(new(this.SamplerResourceLayout, this.GraphicsDevice.Aniso4xSampler));

            this.CommandList.Begin();
            this.FullScreenQuad = new FullScreenQuad(this);
            this.CommandList.End();
            this.GraphicsDevice.SubmitCommands(this.CommandList);
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
            resourceSetDesc.Layout = MainFramebufferTextureLayout;

            this.MainFramebufferTextureSet = this.ResourceFactory.CreateResourceSet(resourceSetDesc);
        }

        public override void Cleanup() {
            this.WhitePixelResourceSet.Dispose();
            this.WhitePixel.Dispose();
            this.SamplerResourceSet.Dispose();
            this.SamplerResourceLayout.Dispose();
            this._imgui.Dispose();
            this.CommandList.Dispose();

            this.GraphicsDevice.Dispose();
            
        }
        
        public override void HandleWindowSizeChange(int width, int height) {
            this.GraphicsDevice.ResizeMainWindow((uint)width, (uint)height);

            this.SetProjectionMatrix((uint)width, (uint)height);
            
            this.CreateFramebuffer(this.GraphicsDevice.SwapchainFramebuffer.Width, this.GraphicsDevice.SwapchainFramebuffer.Height);
        }

        public void SetProjectionMatrix(uint width, uint height) {
            this.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 1f, 0f);
        }

        public override void HandleFramebufferResize(int width, int height) {
            this.GraphicsDevice.ResizeMainWindow((uint)width, (uint)height);
            
            this.CreateFramebuffer(this.GraphicsDevice.SwapchainFramebuffer.Width, this.GraphicsDevice.SwapchainFramebuffer.Height);
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
        }

        public override void Clear() {
            this.CommandList.ClearColorTarget(0, RgbaFloat.Black);
        }

        public override TextureRenderTarget CreateRenderTarget(uint width, uint height) => new TextureRenderTargetVeldrid(this, width, height);
        
        public override Texture CreateTexture(byte[] imageData, bool qoi = false) => new TextureVeldrid(this, imageData, qoi);

        public override Texture CreateTexture(Stream stream) => new TextureVeldrid(this, stream);

        public override Texture CreateTexture(uint width, uint height) => new TextureVeldrid(this, width, height);

        public override Texture CreateTexture(string filepath) => new TextureVeldrid(this, filepath);

        public override Texture CreateWhitePixelTexture() => new TextureVeldrid(this);
        
        public override void ImGuiUpdate(double deltaTime) {
            this._imgui.Update((float)deltaTime);
        }
        public override void ImGuiDraw(double deltaTime) {
            this._imgui.Render(this.GraphicsDevice, this.CommandList);
        }
    }
}
