using System;
using System.Collections.Generic;
using System.Drawing;
using SixLabors.ImageSharp.PixelFormats;

namespace Furball.Vixie.Backends.Direct3D9.Helpers;

public static class FormatHelpers {
    public static unsafe byte[] ConvertRgbaToArgb<T>(ReadOnlySpan<T> input) where T : unmanaged {
        int length = sizeof(T) * input.Length;
        int offset = 0;

        byte[] buffer = new byte[length];

        fixed (void* rgba = input) {
            int* rgbaPtr = (int*)rgba;

            while (offset != length) {
                int value = *rgbaPtr;

                int alpha = ((value))       & 0xFF;
                int blue  = ((value) >> 8)  & 0xFF;
                int green = ((value) >> 16) & 0xFF;
                int red   = ((value) >> 24) & 0xFF;

                buffer[offset + 0] = (byte) alpha;
                buffer[offset + 1] = (byte) red;
                buffer[offset + 2] = (byte) green;
                buffer[offset + 3] = (byte) blue;

                rgbaPtr++;
                offset += 4;
            }
        }

        return buffer;
    }
}
