using Furball.Vixie.Backends.Shared.Backends;
using SixLabors.ImageSharp.PixelFormats;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace Furball.Vixie.Backends.Shared.TextureEffects.Blur;

public sealed class CpuBoxBlurTextureEffect : BoxBlurTextureEffect {
    private readonly GraphicsBackend _backend;
    private VixieTexture    _sourceTex;
    private VixieTexture? _texture;

    public CpuBoxBlurTextureEffect(GraphicsBackend backend, VixieTexture sourceTex) : base(sourceTex) {
        this._backend   = backend;
        this.SetSourceTexture(sourceTex);
    }

    public override void UpdateTexture() {
        Rgba32[] data   = this._sourceTex.GetData();
        Rgba32[] result = new Rgba32[data.Length];

        //Amount of pixels in kernel
        uint kernelSize = (uint)(this.KernelRadius * 2 + 1);

        int width  = this._sourceTex.Width;
        int height = this._sourceTex.Height;
        
        void DoPass(bool lastPass) {
            //TODO: handle edges of the image
            //Do the horizontal pass
            for (int x = this.KernelRadius; x < width - this.KernelRadius; x++) {
                for (int y = this.KernelRadius; y < height - this.KernelRadius; y++) {
                    int accumR = 0;
                    int accumG = 0;
                    int accumB = 0;
                    int accumA = 0;

                    int offset = y * width + x;
                    for (int i = -this.KernelRadius; i <= this.KernelRadius; i++) {
                        Rgba32 pixel = data[offset + i];

                        accumR += (byte)(pixel.R * (pixel.A / 255f));
                        accumG += (byte)(pixel.G * (pixel.A / 255f));
                        accumB += (byte)(pixel.B * (pixel.A / 255f));
                        accumA += pixel.A;
                    }

                    result[(y * width) + x].R = (byte)(accumR / kernelSize);
                    result[(y * width) + x].G = (byte)(accumG / kernelSize);
                    result[(y * width) + x].B = (byte)(accumB / kernelSize);
                    result[(y * width) + x].A = (byte)(accumA / kernelSize);
                }
            }

            //Do the vertical pass
            for (int x = this.KernelRadius; x < width - this.KernelRadius; x++) {
                for (int y = this.KernelRadius; y < height - this.KernelRadius; y++) {
                    int accumR = 0;
                    int accumG = 0;
                    int accumB = 0;
                    int accumA = 0;

                    for (int i = -this.KernelRadius; i <= this.KernelRadius; i++) {
                        Rgba32 pixel = result[((y + i) * width) + x];

                        accumR += (byte)(pixel.R * (pixel.A / 255f));
                        accumG += (byte)(pixel.G * (pixel.A / 255f));
                        accumB += (byte)(pixel.B * (pixel.A / 255f));
                        accumA += pixel.A;
                    }

                    data[(y * width) + x].R = (byte)(accumR / kernelSize);
                    data[(y * width) + x].G = (byte)(accumG / kernelSize);
                    data[(y * width) + x].B = (byte)(accumB / kernelSize);
                    data[(y * width) + x].A = (byte)(accumA / kernelSize);
                }
            }
        }

        for (int i = 0; i < this.Passes - 1; i++) {
            DoPass(false);
        }
        DoPass(true);

        this.Texture.SetData<Rgba32>(data);
    }

    public override void SetSourceTexture(VixieTexture tex) {
        this._sourceTex = tex;
        
        if (this._texture != null && tex.Size == this.Texture.Size) return;
        
        this.Texture?.Dispose();
        this._texture = this._backend.CreateEmptyTexture((uint)tex.Width, (uint)tex.Height);
    }

    public override VixieTexture Texture => _texture;

    public override void Dispose() {
        this.Texture.Dispose();
    }
}