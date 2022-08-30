#if USE_IMGUI
using System;
using Furball.Vixie.Backends.Shared.ImGuiController;
using ImGuiNET;
using JetBrains.Annotations;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Furball.Vixie.Backends.Dummy {
    public class DummyImGuiController : ImGuiController {
        public DummyImGuiController([NotNull] IView            view,        [NotNull] IInputContext input,            ImGuiFontConfig? imGuiFontConfig
 = null, [NotNull] Action onConfigureIo = null) : base(view, input, imGuiFontConfig, onConfigureIo) {}
        protected override void SetupRenderState(ImDrawDataPtr drawDataPtr, int framebufferWidth, int framebufferHeight) {
            // throw new NotImplementedException();
        }
        protected override void PreDraw() {
            // throw new NotImplementedException();
        }
        protected override void Draw(ImDrawDataPtr drawDataPtr) {
            // throw new NotImplementedException();
        }
        protected override void PostDraw() {
            // throw new NotImplementedException();
        }
        protected override void CreateDeviceResources() {
            // throw new NotImplementedException();
        }
        protected override void RecreateFontDeviceTexture() {
            ImGuiIOPtr io = ImGui.GetIO(); 
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pix, out int width, out int height);
            
            io.Fonts.SetTexID((IntPtr)0);
        }
        protected override void DisposeInternal() {
            // throw new NotImplementedException();
        }
    }
}
#endif