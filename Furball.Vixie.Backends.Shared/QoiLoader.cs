using System;
using System.IO;
using Furball.Vixie.Helpers;
using Kettu;
using SixLabors.ImageSharp.PixelFormats;

namespace Furball.Vixie.Graphics {
    public static class QoiLoader {
        private const byte QOI_OP_INDEX = 0x00; /* 00xxxxxx */
        private const byte QOI_OP_DIFF  = 0x40; /* 01xxxxxx */
        private const byte QOI_OP_LUMA  = 0x80; /* 10xxxxxx */
        private const byte QOI_OP_RUN   = 0xc0; /* 11xxxxxx */
        private const byte QOI_OP_RGB   = 0xfe; /* 11111110 */
        private const byte QOI_OP_RGBA  = 0xff; /* 11111111 */

        private const byte QOI_MASK_2 = 0xc0; /* 11000000 */
        
        public enum Channel : byte {
            RGB  = 3,
            RGBA = 4
        }

        public enum ColorSpace : byte {
            LinearAlphaSRGB = 0,
            AllLinear       = 1
        }

        public class QoiHeader {
            public uint       Width;
            public uint       Height;
            public Channel    Channels;
            public ColorSpace ColorSpace;
        }

        private static int CalculateIndex(Rgba32 pixel) {
            return (pixel.R * 3 + pixel.G * 5 + pixel.B * 7 + pixel.A * 11) % 64;
        }

        private static readonly byte[] QOI_PADDING = new byte[] {
            0, 0, 0, 0, 0, 0, 0, 1
        };

        public static (Rgba32[] pixels, QoiHeader header) Load(byte[] file) {
            QoiHeader header = new();
            int p   = 0;

            var stream = new MemoryStream(file);
            var reader = new BigEndianBinaryReader(stream);
            
            #region Read Header

            // char magic[4]; // magic bytes "qoif"
            byte[] magic = reader.ReadBytes(4);
            p += 4;

            if (magic[0] != 'q' || magic[1] != 'o' || magic[2] != 'i' || magic[3] != 'f')
                throw new Exception("Magic does not match! (this is likely not a Qoi file!)");

            //uint32_t width; // image width in pixels (BE)
            header.Width =  reader.ReadUInt32();
            p            += 4;
            //uint32_t height; // image height in pixels (BE)
            header.Height =  reader.ReadUInt32();
            p             += 4;

            //uint8_t channels; // 3 = RGB, 4 = RGBA
            header.Channels   = (Channel)reader.ReadByte();
            p++;
            //uint8_t colorspace; // 0 = sRGB with linear alpha
            //                       1 = all channels linear
            header.ColorSpace = (ColorSpace)reader.ReadByte();
            p++;

            Logger.Log($"Read header of Qoi file! width:{header.Width} height:{header.Height} channels:{header.Channels} colorspace:{header.ColorSpace}", LoggerLevelImageLoader.Instance);

            #endregion

            //Since ImageSharp only supports int size images, we error out here
            if (header.Width > int.MaxValue || header.Height > int.MaxValue) {
                throw new Exception("We do not support images that big!");
            }
            
            // An image is complete when all pixels specified by width * height have been covered.
            uint totalPixels   = header.Width * header.Height;
            uint pixelPosition;

            int run = 0;

            Rgba32[] data = new Rgba32[totalPixels];
            
            // A running array[64] (zero-initialized) of previously seen pixel values is maintained by the encoder and decoder.
            Rgba32[] index = new Rgba32[64];

            // This is the pixel we will copy into the array many times
            Rgba32 px = new(0, 0, 0, 255);

            //The length of a chunk?
            int chunksLen = file.Length - QOI_PADDING.Length * sizeof(byte);
            
            for (pixelPosition = 0; pixelPosition < totalPixels; pixelPosition++) {
                if (run > 0) {
                    run--;
                } 
                else if (p < chunksLen) {
                    byte b1 = file[p++];

                    switch (b1) {
                        //A full RGB pixel
                        case QOI_OP_RGB:
                            px.R = file[p++];
                            px.G = file[p++];
                            px.B = file[p++];
                            break;
                        //A full RGBA pixel
                        case QOI_OP_RGBA:
                            px.R = file[p++];
                            px.G = file[p++];
                            px.B = file[p++];
                            px.A = file[p++];
                            break;
                        default: {
                            switch (b1 & QOI_MASK_2) {
                                //Set the pixel to a certain index
                                case QOI_OP_INDEX:
                                    px = index[b1];
                                    break;
                                //Set the pixel to a difference from the last pixel
                                case QOI_OP_DIFF:
                                    px.R += (byte)(((b1 >> 4) & 0x03) - 2);
                                    px.G += (byte)(((b1 >> 2) & 0x03) - 2);
                                    px.B += (byte)(( b1       & 0x03) - 2);
                                    break;
                                //
                                case QOI_OP_LUMA: {
                                    byte b2 = file[p++];
                                    byte vg = (byte)((b1 & 0x3f) - 32);
                                    px.R += (byte)(vg - 8 + ((b2 >> 4) & 0x0f));
                                    px.G += vg;
                                    px.B += (byte)(vg - 8 + (b2 & 0x0f));
                                    break;
                                }
                                //Repeat the same pixel again
                                case QOI_OP_RUN:
                                    run = (b1 & 0x3f);
                                    break;
                            }
                            break;
                        }
                    }

                    index[CalculateIndex(px) % 64] = px;
                }

                data[pixelPosition] = px;
            }

            return (data, header);
        }
    }
}
