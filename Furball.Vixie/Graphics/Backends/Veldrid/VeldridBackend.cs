using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Furball.Vixie.Graphics.Renderers;
using Kettu;
using Silk.NET.Input.Extensions;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Extensions.Veldrid;
using Veldrid;
using Veldrid.MetalBindings;
using InputSnapshot=Silk.NET.Input.Extensions.InputSnapshot;

namespace Furball.Vixie.Graphics.Backends.Veldrid {
    public class VeldridBackend : GraphicsBackend {
        public static global::Veldrid.GraphicsBackend PrefferedBackend = VeldridWindow.GetPlatformDefaultBackend();
        
        internal GraphicsDevice  GraphicsDevice;
        internal ResourceFactory ResourceFactory;
        internal CommandList     BackendCommandList;
        
        internal Matrix4x4       ProjectionMatrix;
        private  IWindow         _window;
        private  ImGuiController _imgui;

        public override void Initialize(IWindow window) {
            this._window = window;
            
            GraphicsDeviceOptions options = new() {
                SyncToVerticalBlank = window.VSync,
                Debug               = window.API.Flags.HasFlag(ContextFlags.Debug)
            };

            this.GraphicsDevice  = window.CreateGraphicsDevice(options, PrefferedBackend);
            this.ResourceFactory = this.GraphicsDevice.ResourceFactory;
            this.BackendCommandList     = this.ResourceFactory.CreateCommandList();

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

            this._imgui = new ImGuiController(this.GraphicsDevice, this.GraphicsDevice.SwapchainFramebuffer.OutputDescription, window, Global.GameInstance._inputContext);
        }
        
        public override void Cleanup() {
            this.GraphicsDevice.Dispose();
        }
        
        public override void HandleWindowSizeChange(int width, int height) {
            this.GraphicsDevice.ResizeMainWindow((uint)width, (uint)height);
            this.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, 1f, 0f);
        }

        public override void HandleFramebufferResize(int width, int height) {
            this.GraphicsDevice.ResizeMainWindow((uint)width, (uint)height);
        }
        public override IQuadRenderer CreateTextureRenderer() => throw new System.NotImplementedException();
        public override ILineRenderer CreateLineRenderer()    => throw new System.NotImplementedException();
        public override int           QueryMaxTextureUnits()  => throw new System.NotImplementedException();

        public override void BeginScene() {
            this.BackendCommandList.Begin();
            this.BackendCommandList.SetFramebuffer(this.GraphicsDevice.SwapchainFramebuffer);
        }

        public override void EndScene() {
            this.BackendCommandList.End();
            this.GraphicsDevice.SubmitCommands(this.BackendCommandList);
        }

        public override void Present() {
            this.GraphicsDevice.SwapBuffers();
        }

        public override void Clear() {
            this.BackendCommandList.ClearColorTarget(0, RgbaFloat.Black);
        }
        
        public override TextureRenderTarget CreateRenderTarget(uint width,     uint height)      => throw new System.NotImplementedException();
        public override Texture             CreateTexture(byte[]    imageData, bool qoi = false) => throw new System.NotImplementedException();
        public override Texture             CreateTexture(Stream    stream)             => throw new System.NotImplementedException();
        public override Texture             CreateTexture(uint      width, uint height) => throw new System.NotImplementedException();
        public override Texture             CreateTexture(string    filepath) => throw new System.NotImplementedException();
        public override Texture             CreateWhitePixelTexture()         => throw new System.NotImplementedException();
        
        public override void ImGuiUpdate(double deltaTime) {
            this._imgui.Update((float)deltaTime);
        }
        public override void ImGuiDraw(double deltaTime) {
            this._imgui.Render(this.GraphicsDevice, this.BackendCommandList);
        }
    }
}
