using System;
using System.IO;
using Furball.Vixie.Backends.Direct3D9.Helpers;
using Furball.Vixie.Backends.Shared;
using Silk.NET.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Direct3D9;
using Vortice.Mathematics;
using Rectangle=System.Drawing.Rectangle;

namespace Furball.Vixie.Backends.Direct3D9; 

public class TextureD3D9 : VixieTexture {
    private IDirect3DDevice9  _device;
    private IDirect3DTexture9 _texture;
    private bool              _hasMipmaps;

    public override TextureFilterType FilterType { get; set; }

    public override bool Mipmaps => false;

    public TextureD3D9(IDirect3DDevice9 device, byte[] imageData, TextureParameters parameters) {
        this._device = device;

        Image<Argb32> image;

        bool qoi = imageData.Length > 3 && imageData[0] == 'q' && imageData[1] == 'o' && imageData[2] == 'i' &&
                   imageData[3]     == 'f';

        if(qoi) {
            (Argb32[] pixels, QoiLoader.QoiHeader header) data = QoiLoader.LoadArgb(imageData);

            image = Image.LoadPixelData(data.pixels, (int)data.header.Width, (int)data.header.Height);
        } else {
            image = Image.Load<Argb32>(imageData);
        }

        this.Size = new Vector2D<int>(image.Width, image.Height);

        Usage texUsage = Usage.Dynamic;

        if (parameters.RequestMipmaps) {
            texUsage         |= Usage.AutoGenerateMipMap;
            this._hasMipmaps =  true;
        }

        //Apperantly level HAS to be 1, according to the official docs,
        this._texture = this._device.CreateTexture(image.Width, image.Height, 1, texUsage, Format.A8R8G8B8, Pool.Managed);

        this.SetData(image);
    }

    public TextureD3D9(IDirect3DDevice9 device, Stream imageData, TextureParameters parameters) {
        this._device = device;

        Image<Argb32> image = Image.Load<Argb32>(imageData);

        this.Size = new Vector2D<int>(image.Width, image.Height);

        Usage texUsage = Usage.Dynamic;

        if (parameters.RequestMipmaps) {
            texUsage         |= Usage.AutoGenerateMipMap;
            this._hasMipmaps =  true;
        }

        //Apperantly level HAS to be 1, according to the official docs,
        this._texture = this._device.CreateTexture(image.Width, image.Height, 1, texUsage, Format.A8R8G8B8, Pool.Managed);

        this.SetData(image);
    }

    private unsafe void SetData(Image<Argb32> image) {
        image.ProcessPixelRows(
        accessor => {
            for (int i = 0; i < accessor.Height; i++)
                fixed (void* ptr = accessor.GetRowSpan(i)) {
                    LockedRectangle rect = this._texture.LockRect(0, new RectI(0, i, accessor.Width, 1), LockFlags.Discard);

                    Buffer.MemoryCopy(ptr, (void*)rect.DataPointer, sizeof(Argb32) * accessor.Width, sizeof(Argb32) * accessor.Width);

                    this._texture.UnlockRect(0);
                }
        }
        );
    }

    public TextureD3D9(IDirect3DDevice9 device, uint width, uint height, TextureParameters parameters) {
        this._device = device;

        Usage texUsage = Usage.Dynamic;

        if (parameters.RequestMipmaps) {
            texUsage         |= Usage.AutoGenerateMipMap;
            this._hasMipmaps =  true;
        }

        //Apperantly level HAS to be 1, according to the official docs,
        this._texture = this._device.CreateTexture((int) width, (int) height, 1, texUsage, Format.A8R8G8B8, Pool.Managed);
    }

    public unsafe TextureD3D9(IDirect3DDevice9 device) {
        this._device = device;

        this._texture = this._device.CreateTexture(1, 1, 1, Usage.Dynamic, Format.A8R8G8B8, Pool.Managed);

        byte* data = stackalloc byte[] {
            255, 255, 255, 255
        };

        LockedRectangle rect = this._texture.LockRect(0, new RectI(0, 0, 1, 1), LockFlags.Discard);

        Buffer.MemoryCopy(data, (void*)rect.DataPointer, sizeof(Argb32), sizeof(Argb32));

        this._texture.UnlockRect(0);
    }

    public override unsafe VixieTexture SetData<T>(ReadOnlySpan<T> data) {
        byte[] argb = FormatHelpers.ConvertRgbaToArgb(data);

        LockedRectangle rect = this._texture.LockRect(0, new RectI(0, 0, this.Width, this.Height), LockFlags.Discard);

        fixed (void* ptr = argb) {
            Buffer.MemoryCopy(ptr, (void*)rect.DataPointer, sizeof(byte) * argb.Length, sizeof(byte) * argb.Length);
        }

        this._texture.UnlockRect(0);
        this._texture.GenerateMipSubLevels();

        return this;
    }

    public override unsafe VixieTexture SetData<T>(ReadOnlySpan<T> data, Rectangle rectangle) {
        byte[] argb = FormatHelpers.ConvertRgbaToArgb(data);

        LockedRectangle rect = this._texture.LockRect(0, new RectI(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height), LockFlags.Discard);

        fixed (void* ptr = argb) {
            Buffer.MemoryCopy(ptr, (void*)rect.DataPointer, sizeof(Argb32) * rectangle.Width * rectangle.Height, sizeof(Argb32) * rectangle.Width * rectangle.Height);
        }

        this._texture.UnlockRect(0);
        this._texture.GenerateMipSubLevels();

        return this;
    }

    public override Rgba32[] GetData() {
        throw new NotImplementedException();
    }
}