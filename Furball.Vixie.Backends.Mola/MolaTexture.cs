using System;
using Furball.Mola.Bindings;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Helpers;
using Silk.NET.Maths;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle = System.Drawing.Rectangle;

namespace Furball.Vixie.Backends.Mola;

public unsafe class MolaTexture : VixieTexture {
    internal RenderBitmap* RenderBitmap;

    public MolaTexture(uint width, uint height) {
        this.Size = new Vector2D<int>((int)width, (int)height);

        this.RenderBitmap = Furball.Mola.Bindings.Mola.CreateRenderBitmap(width, height, PixelType.Rgba32);
    }

    public override TextureFilterType FilterType {
        get;
        set;
    } =
        TextureFilterType
           .Pixelated; //TODO: different filter types, currently the renderer only supports nearest neighbor/pixelated

    public override bool Mipmaps => false;

    public override VixieTexture SetData <T>(ReadOnlySpan<T> data) {
        fixed (void* ptr = data) {
            Buffer.MemoryCopy(ptr, this.RenderBitmap->Rgba32Ptr, data.Length * sizeof(T), data.Length * sizeof(T));
        }

        return this;
    }
    public override VixieTexture SetData <T>(ReadOnlySpan<T> data, Rectangle rect) {
        fixed (T* ptr = data) {
            for (int y = 0; y < rect.Height; y++)
                Buffer.MemoryCopy(
                    ptr + rect.Width * y, //The start of the buffer, seemingly correct
                    this.RenderBitmap->Rgba32Ptr +
                    (this.RenderBitmap->Width * (y + rect.Y) + rect.X), //The destination buffer place, 
                    rect.Width * sizeof(T),                  //The size of the line
                    rect.Width * sizeof(T)                   //The size of the line
                );
        }

        return this;
    }
    public override Rgba32[] GetData() {
        Rgba32[] arr = new Rgba32[this.Width * this.Height];

        fixed (void* ptr = arr) {
            Buffer.MemoryCopy(this.RenderBitmap->Rgba32Ptr, ptr, arr.Length * sizeof(Rgba32),
                              arr.Length                                    * sizeof(Rgba32));
        }

        return arr;
    }
    
    public override void CopyTo(VixieTexture tex) {
        Guard.Assert(tex.Size == this.Size);

        if (tex is not MolaTexture molaTex)
            Guard.Fail($"Texture must be of type {typeof(MolaTexture)}");
        else
            Buffer.MemoryCopy(
                this.RenderBitmap->Rgba32Ptr,
                molaTex.RenderBitmap->Rgba32Ptr,
                sizeof(Rgba32) * this.Width * this.Height,
                sizeof(Rgba32) * this.Width * this.Height
            );
    }

    ~MolaTexture() {
        DisposeQueue.Enqueue(this);
    }
}