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
        //We can support as many as you can fit into RAM, which is probably a lot
        return int.MaxValue;
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

        int width  = image.Width;
        int height = image.Height;

        MolaTexture tex = new MolaTexture((uint)width, (uint)height);

        //Set the data of the texture
        image.ProcessPixelRows(x => {
            for (int i = 0; i < x.Height; i++) {
                Span<Rgba32> rowSpan = x.GetRowSpan(i);

                tex.SetData<Rgba32>(rowSpan, new System.Drawing.Rectangle(0, i, x.Width, 1));
            }
        });

        return tex;
    }

    public override VixieTexture CreateTextureFromStream(Stream            stream,
                                                         TextureParameters parameters = default(TextureParameters)) {
        Image<Rgba32> image = Image.Load<Rgba32>(stream);

        int width  = image.Width;
        int height = image.Height;
        
        MolaTexture tex = new MolaTexture((uint)width, (uint)height);

        //Set the data of the texture
        image.ProcessPixelRows(x => {
            for (int i = 0; i < x.Height; i++) {
                Span<Rgba32> rowSpan = x.GetRowSpan(i);

                tex.SetData<Rgba32>(rowSpan, new System.Drawing.Rectangle(0, i, x.Width, 1));
            }
        });

        return tex;
    }

    public override VixieTexture
        CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default(TextureParameters)) {
        MolaTexture tex = new MolaTexture(width, height);

        Furball.Mola.Bindings.Mola.ClearRenderBitmap(tex.RenderBitmap);

        return tex;
    }

    public override VixieTexture CreateWhitePixelTexture() {
        MolaTexture tex = new MolaTexture(1, 1);

        Rgba32[] arr = {
            new Rgba32(1f, 1f, 1f)
        };
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