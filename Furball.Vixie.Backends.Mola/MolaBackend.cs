using System;
using System.IO;
using Furball.Mola.Bindings;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Silk.NET.Input;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Furball.Vixie.Backends.Mola;

public unsafe class MolaBackend : GraphicsBackend {
    private RenderBitmap* _renderBitmap;

    private bool          _screenshotQueued;
    public  RenderBitmap* BoundRenderTarget;

    public RenderBitmap* BitmapToRenderTo
        => this.BoundRenderTarget != null ? this.BoundRenderTarget : this._renderBitmap;
    public override Rectangle ScissorRect {
        get;
        set;
    }

    public override void Initialize(IView view, IInputContext inputContext) {
#if USE_IMGUI
        throw new Exception();
#endif
    }
    public override void Cleanup() {
        Furball.Mola.Bindings.Mola.DeleteRenderBitmap(this._renderBitmap);
    }
    public override void HandleFramebufferResize(int width, int height) {
        if (this._renderBitmap != null)
            Furball.Mola.Bindings.Mola.DeleteRenderBitmap(this._renderBitmap);

        this._renderBitmap =
            Furball.Mola.Bindings.Mola.CreateRenderBitmap((uint)width, (uint)height, PixelType.Rgba32);
    }
    public override Renderer CreateRenderer() {
        return new MolaRenderer(this);
    }
    public override int QueryMaxTextureUnits() {
        return 32;
    }
    public override void Clear() {
        Furball.Mola.Bindings.Mola.ClearRenderBitmap(this._renderBitmap);
    }
    public override void TakeScreenshot() {
        this._screenshotQueued = true;
    }
    public override void SetFullScissorRect() {
        this._renderBitmap->ScissorX = 0;
        this._renderBitmap->ScissorY = 0;
        this._renderBitmap->ScissorW = this._renderBitmap->Width;
        this._renderBitmap->ScissorH = this._renderBitmap->Height;
    }
    public override ulong GetVramUsage() {
        return 0;
    }
    public override ulong GetTotalVram() {
        return 0;
    }
    public override VixieTextureRenderTarget CreateRenderTarget(uint width, uint height) {
        return new MolaRenderTarget(this, (int)width, (int)height);
    }
    public override VixieTexture CreateTextureFromByteArray(byte[]            imageData,
                                                            TextureParameters parameters = default) {
        throw new NotImplementedException();
    }
    public override VixieTexture CreateTextureFromStream(Stream stream, TextureParameters parameters = default) {
        throw new NotImplementedException();
    }
    public override VixieTexture
        CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default) {
        MolaTexture tex = new(width, height);

        Furball.Mola.Bindings.Mola.ClearRenderBitmap(tex.RenderBitmap);

        return tex;
    }
    public override VixieTexture CreateWhitePixelTexture() {
        MolaTexture tex = new(1, 1);

        Rgba32[] arr = { new(1f, 1f, 1f) };
        tex.SetData<Rgba32>(arr);

        return tex;
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