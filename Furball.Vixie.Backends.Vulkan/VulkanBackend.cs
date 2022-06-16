using System;
using System.IO;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Silk.NET.Input;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;

namespace Furball.Vixie.Backends.Vulkan {
    public class VulkanBackend : IGraphicsBackend {
        public override void Initialize(IWindow window, IInputContext inputContext) {
            throw new NotImplementedException();
        }
        public override void Cleanup() {
            throw new NotImplementedException();
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
