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

        Image<Rgba32> image;

        bool qoi = imageData.Length > 3 && imageData[0] == 'q' && imageData[1] == 'o' && imageData[2] == 'i' &&
                   imageData[3]     == 'f';

        if(qoi) {
            (Rgba32[] pixels, QoiLoader.QoiHeader header) data = QoiLoader.Load(imageData);

            image = Image.LoadPixelData(data.pixels, (int)data.header.Width, (int)data.header.Height);
        } else {
            image = Image.Load<Rgba32>(imageData);
        }

        this.Size = new Vector2D<int>(image.Width, image.Height);

        Usage texUsage = Usage.None;

        if (parameters.RequestMipmaps) {
            texUsage         |= Usage.AutoGenerateMipMap;
            this._hasMipmaps =  true;
        }

        //Apperantly level HAS to be 1, according to the official docs,
        this._texture = this._device.CreateTexture(image.Width, image.Height, 0, texUsage, Format.A8B8G8R8, Pool.Managed);

        this.SetData(image);
    }

    public TextureD3D9(IDirect3DDevice9 device, Stream imageData, TextureParameters parameters) {
        this._device = device;

        Image<Rgba32> image = Image.Load<Rgba32>(imageData);

        this.Size = new Vector2D<int>(image.Width, image.Height);

        Usage texUsage = Usage.None;

        if (parameters.RequestMipmaps) {
            texUsage         |= Usage.AutoGenerateMipMap;
            this._hasMipmaps =  true;
        }

        //Apperantly level HAS to be 1, according to the official docs,
        this._texture = this._device.CreateTexture(image.Width, image.Height, 0, texUsage, Format.A8B8G8R8, Pool.Managed);

        this.SetData(image);
    }

    private unsafe void SetData(Image<Rgba32> image) {
        int length = image.Width * image.Height * sizeof(Rgba32);

        byte[] dataStore = new byte[length];

        image.CopyPixelDataTo(dataStore);

        LockedRectangle rect = this._texture.LockRect(0, LockFlags.None);

        fixed (void* ptr = dataStore) {
            Buffer.MemoryCopy(ptr, (void*)rect.DataPointer, length, length);
        }

        this._texture.UnlockRect(0);
    }

    public TextureD3D9(IDirect3DDevice9 device, uint width, uint height, TextureParameters parameters) {
        this._device = device;

        Usage texUsage = Usage.None;

        if (parameters.RequestMipmaps) {
            texUsage         |= Usage.AutoGenerateMipMap;
            this._hasMipmaps =  true;
        }

        //Apperantly level HAS to be 1, according to the official docs,
        this._texture = this._device.CreateTexture((int) width, (int) height, 0, texUsage, Format.A8B8G8R8, Pool.Managed);
    }

    public unsafe TextureD3D9(IDirect3DDevice9 device) {
        this._device = device;

        this._texture = this._device.CreateTexture(1, 1, 0, Usage.Dynamic, Format.A8B8G8R8, Pool.Managed);

        byte* data = stackalloc byte[] {
            255, 255, 255, 255
        };

        LockedRectangle rect = this._texture.LockRect(0, new RectI(0, 0, 1, 1), LockFlags.Discard);

        Buffer.MemoryCopy(data, (void*)rect.DataPointer, sizeof(Argb32), sizeof(Argb32));

        this._texture.UnlockRect(0);
    }

    public override unsafe VixieTexture SetData<T>(ReadOnlySpan<T> data) {
        LockedRectangle rect = this._texture.LockRect(0, new RectI(0, 0, this.Width, this.Height), LockFlags.None);

        fixed (void* ptr = data) {
            Buffer.MemoryCopy(ptr, (void*)rect.DataPointer, sizeof(T) * data.Length, sizeof(T) * data.Length);
        }

        this._texture.UnlockRect(0);
        this._texture.GenerateMipSubLevels();

        return this;
    }

    public override unsafe VixieTexture SetData<T>(ReadOnlySpan<T> data, Rectangle rectangle) {
        LockedRectangle rect = this._texture.LockRect(0, new RectI(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height), LockFlags.Discard);

        fixed (void* ptr = data) {
            Buffer.MemoryCopy(ptr, (void*)rect.DataPointer, sizeof(Argb32) * rectangle.Width * rectangle.Height, sizeof(Argb32) * rectangle.Width * rectangle.Height);
        }

        this._texture.UnlockRect(0);
        this._texture.GenerateMipSubLevels();

        return this;
    }

    public override Rgba32[] GetData() {
        throw new NotImplementedException();
    }

    public void Bind(int stage) {
        this._device.SetTexture(stage, this._texture);
    }
}