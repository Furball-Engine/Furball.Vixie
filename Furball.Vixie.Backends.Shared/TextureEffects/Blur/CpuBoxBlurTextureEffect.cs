using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Furball.Vixie.Backends.Shared.Backends;
using SixLabors.ImageSharp.PixelFormats;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace Furball.Vixie.Backends.Shared.TextureEffects.Blur;

public class CpuBoxBlurTextureEffect : BoxBlurTextureEffect {
    private readonly GraphicsBackend _backend;
    private readonly VixieTexture    _sourceTex;

    public CpuBoxBlurTextureEffect(GraphicsBackend backend, VixieTexture sourceTex) : base(sourceTex) {
        this._backend   = backend;
        this._sourceTex = sourceTex;
        this.Texture    = backend.CreateEmptyTexture((uint)sourceTex.Width, (uint)sourceTex.Height);
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

                    result[(y * width) + x].R = (byte)(accumR / kernelSize);
                    result[(y * width) + x].G = (byte)(accumG / kernelSize);
                    result[(y * width) + x].B = (byte)(accumB / kernelSize);
                    result[(y * width) + x].A = (byte)(accumA / kernelSize);
                }
            }

            if (!lastPass)
                //Copy the result back to the original array
                Array.Copy(result, data, data.Length);
        }

        for (int i = 0; i < this.Passes - 1; i++) {
            DoPass(false);
        }
        DoPass(true);

        this.Texture.SetData<Rgba32>(result);
    }

    public override VixieTexture Texture {
        get;
    }

    public override void Dispose() {
        this.Texture.Dispose();
    }
}