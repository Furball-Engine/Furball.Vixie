using System.IO;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Backends.Shared.TextureEffects.Blur;
#if USE_IMGUI
using Furball.Vixie.Helpers;
#endif
using Kettu;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle=SixLabors.ImageSharp.Rectangle;

namespace Furball.Vixie.Backends.Dummy;

public class DummyBackend : GraphicsBackend {
#if USE_IMGUI
        private DummyImGuiController _imgui;
#endif
    public override void Initialize(IView view, IInputContext inputContext) {
#if USE_IMGUI
        Guard.Fail("ImGui is currently broken on the Dummy backend! Please disable ImGui in `Directory.Build.props`!");
        this._imgui = new DummyImGuiController(view, inputContext);
        this._imgui.Initialize();
#endif

        Logger.Log("Initializing dummy backend!", LoggerLevelDummy.InstanceInfo);
    }
    public override void Cleanup() {
#if USE_IMGUI
        this._imgui.Dispose();
#endif
    }
    public override void HandleFramebufferResize(int width, int height) {
        // throw new System.NotImplementedException();
    }
    public override VixieRenderer CreateRenderer() {
        return new DummyVixieRenderer(this);
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
        get;
    }
    public override void Clear() {
        // throw new System.NotImplementedException();
    }
    public override void TakeScreenshot() {
        // throw new System.NotImplementedException();
    }
    public override Rectangle ScissorRect {
        get;
        set;
    }
    public override void SetFullScissorRect() {
        // throw new System.NotImplementedException();
    }
    public override ulong GetVramUsage() => 0;
    public override ulong GetTotalVram() => 0;
    public override VixieTextureRenderTarget CreateRenderTarget(uint width, uint height) {
        return new DummyTextureRenderTarget((int)width, (int)height);
    }
    public override VixieTexture CreateTextureFromByteArray(byte[]            imageData,
                                                            TextureParameters parameters = default) {
        Image image;
        bool qoi = imageData.Length > 3 && imageData[0] == 'q' && imageData[1] == 'o' && imageData[2] == 'i' &&
                   imageData[3]     == 'f';

        if(qoi) {
            (Rgba32[] pixels, QoiLoader.QoiHeader header) data = QoiLoader.Load(imageData);

            image = Image.LoadPixelData(data.pixels, (int)data.header.Width, (int)data.header.Height);
        } else {
            image = Image.Load<Rgba32>(imageData);
        }
        
        int   width  = image.Width;
        int   height = image.Height;
        image.Dispose();
        return new DummyTexture(parameters, width, height);
    }
    public override VixieTexture CreateTextureFromStream(Stream stream, TextureParameters parameters = default) {
        Image image  = Image.Load(stream);
        int   width  = image.Width;
        int   height = image.Height;
        image.Dispose();
        return new DummyTexture(parameters, width, height);
    }
    public override VixieTexture
        CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default) {
        return new DummyTexture(parameters, (int)width, (int)height);
    }
    public override VixieTexture CreateWhitePixelTexture() {
        return new DummyTexture(default, 1, 1);
    }
#if USE_IMGUI
    public override void ImGuiUpdate(double deltaTime) {
        this._imgui.Update((float)deltaTime);
    }
    public override void ImGuiDraw(double deltaTime) {
        this._imgui.Render();
    }
#endif
}