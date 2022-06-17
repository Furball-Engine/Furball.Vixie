using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Kettu;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;

namespace Furball.Vixie.Backends.Vulkan {
    public class VulkanBackend : IGraphicsBackend {
        private Instance _instance;
        private Vk       _vk;
        private IWindow  _window;

        private static string[] _Extensions = new[] {
            "VK_EXT_debug_utils"
        };

        public override unsafe void Initialize(IWindow window, IInputContext inputContext) {
            this._window = window;
            
            _vk = Vk.GetApi();

            this.PrintValidationLayers();

            bool debug = window.API.Flags.HasFlag(ContextFlags.Debug);
            
            #region Get required extensions

            byte** required = this._window.VkSurface!.GetRequiredExtensions(out uint count);

            uint   extensionCount = (uint)(count + (debug ? _Extensions.Length : 0));
            byte** extensions     = stackalloc byte*[(int)extensionCount];

            for (uint i = 0; i < count; i++) {
                extensions[i] = required[i];
            }
            if(debug)
                for (var i = 0; i < _Extensions.Length; i++) {
                    extensions[count + i] = (byte*)SilkMarshal.StringToPtr(_Extensions[i]);
                }
            
            #endregion
            
            #region Create instance

            ApplicationInfo appInfo = new ApplicationInfo {
                SType              = StructureType.ApplicationInfo,
                PApplicationName   = (byte*) Marshal.StringToHGlobalAnsi(Assembly.GetEntryAssembly()!.FullName),
                ApplicationVersion = new Version32(1, 0, 0),
                PEngineName        = (byte*)Marshal.StringToHGlobalAnsi("Furball.Vixie"),
                EngineVersion      = new Version32(1, 0, 0),
                ApiVersion         = Vk.Version10
            };

            InstanceCreateInfo createInfo = new InstanceCreateInfo {
                SType            = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo
            };

            
            createInfo.EnabledExtensionCount   = extensionCount;
            createInfo.PpEnabledExtensionNames = extensions;

            createInfo.EnabledLayerCount = 0;

            Instance instance;
            Result   result = this._vk.CreateInstance(&createInfo, null, &instance);

            if (result != Result.Success) {
                throw new Exception($"Creating vulkan instance failed! err:{result}");
            }

            this._instance = instance;
            
            Logger.Log($"Created Vulkan Instance {this._instance}", LoggerLevelVulkan.InstanceInfo);

            #endregion
        }

        private unsafe void PrintValidationLayers() {
            uint count;
            this._vk.EnumerateInstanceLayerProperties(&count, null);

            LayerProperties[] properties = new LayerProperties[count];
            this._vk.EnumerateInstanceLayerProperties(&count, properties);

            foreach (LayerProperties layerProperties in properties) {
                Logger.Log($"Validation layer {SilkMarshal.PtrToString((nint)layerProperties.LayerName)} available!", LoggerLevelVulkan.InstanceInfo);
            }
        }
        
        public override unsafe void Cleanup() {
            this._vk.DestroyInstance(this._instance, null);
        }
        public override void HandleWindowSizeChange(int width, int height) {
            throw new NotImplementedException();
        }
        public override void HandleFramebufferResize(int width, int height) {
            throw new NotImplementedException();
        }
        public override IQuadRenderer CreateTextureRenderer() => throw new NotImplementedException();
        public override ILineRenderer CreateLineRenderer() => throw new NotImplementedException();
        public override int QueryMaxTextureUnits() => throw new NotImplementedException();
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
        public override TextureRenderTarget CreateRenderTarget(uint width, uint height) => throw new NotImplementedException();
        public override Texture CreateTexture(byte[] imageData, bool qoi = false) => throw new NotImplementedException();
        public override Texture CreateTexture(Stream stream) => throw new NotImplementedException();
        public override Texture CreateTexture(uint width, uint height) => throw new NotImplementedException();
        public override Texture CreateTexture(string filepath) => throw new NotImplementedException();
        public override Texture CreateWhitePixelTexture() => throw new NotImplementedException();
        public override void ImGuiUpdate(double deltaTime) {
            throw new NotImplementedException();
        }
        public override void ImGuiDraw(double deltaTime) {
            throw new NotImplementedException();
        }
    }
}
