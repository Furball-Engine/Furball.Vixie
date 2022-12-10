#nullable enable
using System;
using System.IO;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Silk.NET.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle = System.Drawing.Rectangle;

namespace Furball.Vixie;

public class Texture : IDisposable {
    public string Name = "";

    private readonly VixieTexture _texture;

    public Vector2D<int> Size {
        get;
    }

    public int Width  => this.Size.X;
    public int Height => this.Size.Y;

    public TextureFilterType FilterType {
        get => this._texture.FilterType;
        set => this._texture.FilterType = value;
    }

    public static Texture CreateTextureFromByteArray(GraphicsBackend backend, byte[] imageData, TextureParameters
                                                         parameters = default(TextureParameters)) {
        VixieTexture tex = backend.CreateTextureFromByteArray(imageData, parameters);

        Texture managedTex = new Texture(tex);

        Global.TrackedTextures.Add(new WeakReference<Texture>(managedTex));

        return managedTex;
    }

    public static Texture CreateWhitePixelTexture(GraphicsBackend backend) {
        VixieTexture tex = backend.CreateWhitePixelTexture();

        Texture managedTex = new Texture(tex);

        Global.TrackedTextures.Add(new WeakReference<Texture>(managedTex));

        return managedTex;
    }

    public static Texture CreateTextureFromStream(GraphicsBackend   backend, Stream stream,
                                                  TextureParameters parameters = default(TextureParameters)) {
        VixieTexture tex = backend.CreateTextureFromStream(stream, parameters);

        Texture managedTex = new Texture(tex);

        Global.TrackedTextures.Add(new WeakReference<Texture>(managedTex));

        return managedTex;
    }

    public static Texture CreateEmptyTexture(GraphicsBackend   backend, uint width, uint height,
                                             TextureParameters parameters = default(TextureParameters)) {
        VixieTexture tex = backend.CreateEmptyTexture(width, height, parameters);

        Texture managedTex = new Texture(tex);

        Global.TrackedTextures.Add(new WeakReference<Texture>(managedTex));

        return managedTex;
    }

    public static Texture CreateTextureFromImage(GraphicsBackend   backend, Image image,
                                                 TextureParameters parameters = default(TextureParameters)) {
        VixieTexture tex = backend.CreateEmptyTexture((uint)image.Width, (uint)image.Height, parameters);

        Texture managedTex = new Texture(tex);

        Global.TrackedTextures.Add(new WeakReference<Texture>(managedTex));

        managedTex.SetData(image);

        return managedTex;
    }

    internal Texture(VixieTexture texture) {
        this._texture = texture;

        this.Size = this._texture.Size;
    }

    public void SetData(Image<Rgba32> image) {
        if (image.Width != this.Width || this.Height != image.Height)
            throw new InvalidImageContentException(
                $"That image does not have the right size! Expected: {this.Width}x{this.Height} Got: {image.Width}x{image.Height}");

        image.ProcessPixelRows(
            x => {
                for (int i = 0; i < x.Height; i++) {
                    Span<Rgba32> span = x.GetRowSpan(i);

                    this.SetData<Rgba32>(span, new Rectangle(0, i, x.Width, 1));
                }
            });
    }

    public void SetData(Image image) {
        this.SetData(image.CloneAs<Rgba32>());
    }

    public void SetData <pT>(pT[] data) where pT : unmanaged {
        this._texture.SetData<pT>(data);
    }

    public void SetData <pT>(ReadOnlySpan<pT> data) where pT : unmanaged {
        this._texture.SetData(data);
    }

    // ReSharper disable once MethodOverloadWithOptionalParameter
    public void SetData <pT>(ReadOnlySpan<pT> arr, Rectangle? rect = null) where pT : unmanaged {
        rect ??= new Rectangle(0, 0, this.Size.X, this.Size.Y);

        this._texture.SetData(arr, rect.Value);
    }

    public Rgba32[] GetData() {
        return this._texture.GetData();
    }

    public void CopyTo(Texture texture) {
        this._texture.CopyTo(texture._texture);
    }

    private bool _isDisposed;
    public void Dispose() {
        if (this._isDisposed)
            return;

        this._isDisposed = true;
        this._texture.Dispose();
    }

    public static implicit operator VixieTexture(Texture tex) {
        return tex._texture;
    }

    public override string ToString() => string.IsNullOrWhiteSpace(this.Name) ? base.ToString() : this.Name;
}