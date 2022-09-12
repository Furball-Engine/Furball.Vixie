using System;
using Furball.Mola.Bindings;
using Furball.Vixie.Backends.Shared;
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
    } = TextureFilterType.Pixelated;
    public override bool Mipmaps => false;
    public override VixieTexture SetData <T>(ReadOnlySpan<T> data) {
        fixed(void* ptr = data)
            Buffer.MemoryCopy(ptr, this.RenderBitmap->Rgba32Ptr, data.Length * sizeof(T), data.Length * sizeof(T));
        
        return this;
    }
    public override VixieTexture SetData <T>(ReadOnlySpan<T> data, Rectangle rect) {
        fixed(void* ptr = data)
            for (int y = rect.Y; y < rect.Bottom; y++) {
                Buffer.MemoryCopy((T*)ptr + rect.Width * y, this.RenderBitmap->Rgba32Ptr + (this.RenderBitmap->Width * y + rect.X), rect.Width * sizeof(T), rect.Width * sizeof(T));
            }
        
        return this;
    }
    public override Rgba32[] GetData() {
        Rgba32[] arr = new Rgba32[this.Width * this.Height];
        
        fixed(void* ptr = arr)
            Buffer.MemoryCopy(this.RenderBitmap->Rgba32Ptr, ptr, arr.Length * sizeof(Rgba32), arr.Length * sizeof(Rgba32));

        return arr;
    }
}