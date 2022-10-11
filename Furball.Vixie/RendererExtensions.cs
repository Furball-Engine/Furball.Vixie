using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using FontStashSharp;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Color = Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie;

public static class RendererExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 FlipVector2(Vector2 vec, TextureFlip flip) {
        //Dont do anything if we dont have to
        if (flip == TextureFlip.None)
            return vec;

        //To prevent unnesessary allocations, we just write to the vec directly, then return it
        vec.X = (flip & TextureFlip.FlipHorizontal) != 0 ? -vec.X : vec.X;
        vec.Y = (flip & TextureFlip.FlipVertical)   != 0 ? -vec.Y : vec.Y;

        return vec;
    }

    private static unsafe void SetQuadVertices(Renderer     Renderer, Vertex* ptr,   Vector2     pos, Vector2 size,
                                               VixieTexture tex,      Color   color, TextureFlip flip) {
        long texId = Renderer.GetTextureId(tex);
        ptr[0].Position          = pos;
        ptr[0].Color             = color;
        ptr[0].TextureCoordinate = FlipVector2(new Vector2(0, tex.InternalFlip ? 1 : 0), flip);
        ptr[0].TexId             = texId;

        ptr[1].Position = pos + size with {
            Y = 0
        };
        ptr[1].Color             = color;
        ptr[1].TextureCoordinate = FlipVector2(new Vector2(1, tex.InternalFlip ? 1 : 0), flip);
        ptr[1].TexId             = texId;

        ptr[2].Position          = pos + size;
        ptr[2].Color             = color;
        ptr[2].TextureCoordinate = FlipVector2(new Vector2(1, tex.InternalFlip ? 0 : 1), flip);
        ptr[2].TexId             = texId;

        ptr[3].Position = pos + size with {
            X = 0
        };
        ptr[3].Color             = color;
        ptr[3].TextureCoordinate = FlipVector2(new Vector2(0, tex.InternalFlip ? 0 : 1), flip);
        ptr[3].TexId             = texId;
    }

    private static unsafe void SetQuadIndices(MappedData mappedData) {
        //Tri 1
        mappedData.IndexPtr[0] = (ushort)(3 + mappedData.IndexOffset);
        mappedData.IndexPtr[1] = (ushort)(2 + mappedData.IndexOffset);
        mappedData.IndexPtr[2] = (ushort)(0 + mappedData.IndexOffset);
        //Tri 2
        mappedData.IndexPtr[3] = (ushort)(2 + mappedData.IndexOffset);
        mappedData.IndexPtr[4] = (ushort)(1 + mappedData.IndexOffset);
        mappedData.IndexPtr[5] = (ushort)(0 + mappedData.IndexOffset);
    }

    public static unsafe void AllocateUnrotatedTexturedQuad(this Renderer Renderer, VixieTexture tex, Vector2 position,
                                                            Vector2       scale,    Color        color,
                                                            TextureFlip   flip = TextureFlip.None) {
        Vector2 size = new(tex.Width * scale.X, tex.Height * scale.Y);

        MappedData mappedData = Renderer.Reserve(4, 6);

        SetQuadVertices(Renderer, mappedData.VertexPtr, position, size, tex, color, flip);
        SetQuadIndices(mappedData);
    }

    public static unsafe void AllocateUnrotatedTexturedQuadWithSourceRect(this Renderer Renderer, VixieTexture tex,
                                                                          Vector2       position,
                                                                          Vector2       scale, Rectangle sourceRect,
                                                                          Color         color,
                                                                          TextureFlip   flip = TextureFlip.None) {
        Vector2 size = new(sourceRect.Width * scale.X, sourceRect.Height * scale.Y);

        if (tex.InternalFlip)
            sourceRect.Height *= -1;

        MappedData mappedData = Renderer.Reserve(4, 6);
        SetQuadVertices(Renderer, mappedData.VertexPtr, position, size, tex, color, flip);
        mappedData.VertexPtr[0].TextureCoordinate =
            FlipVector2(new Vector2(sourceRect.X / (float)tex.Width, sourceRect.Y / (float)tex.Height), flip);
        mappedData.VertexPtr[1].TextureCoordinate =
            FlipVector2(new Vector2(sourceRect.Right / (float)tex.Width, sourceRect.Y / (float)tex.Height), flip);
        mappedData.VertexPtr[2].TextureCoordinate =
            FlipVector2(new Vector2(sourceRect.Right / (float)tex.Width, sourceRect.Bottom / (float)tex.Height), flip);
        mappedData.VertexPtr[3].TextureCoordinate =
            FlipVector2(new Vector2(sourceRect.X / (float)tex.Width, sourceRect.Bottom / (float)tex.Height), flip);

        SetQuadIndices(mappedData);
    }

    public static unsafe void AllocateRotatedTexturedQuad(this Renderer Renderer, VixieTexture tex, Vector2 position,
                                                          Vector2       scale, float rotation, Vector2 rotationOrigin,
                                                          Color         color, TextureFlip flip = TextureFlip.None) {
        Vector2 size = new(tex.Width * scale.X, tex.Height * scale.Y);

        MappedData mappedData = Renderer.Reserve(4, 6);
        mappedData.VertexPtr[0].Position          = Vector2.Zero;
        mappedData.VertexPtr[0].Color             = color;
        mappedData.VertexPtr[0].TexId             = Renderer.GetTextureId(tex);
        mappedData.VertexPtr[0].TextureCoordinate = FlipVector2(new Vector2(0, 0), flip);

        mappedData.VertexPtr[1].Position = size with {
            Y = 0
        };
        mappedData.VertexPtr[1].Color             = color;
        mappedData.VertexPtr[1].TexId             = Renderer.GetTextureId(tex);
        mappedData.VertexPtr[1].TextureCoordinate = FlipVector2(new Vector2(1, 0), flip);

        mappedData.VertexPtr[2].Position          = size;
        mappedData.VertexPtr[2].Color             = color;
        mappedData.VertexPtr[2].TexId             = Renderer.GetTextureId(tex);
        mappedData.VertexPtr[2].TextureCoordinate = FlipVector2(new Vector2(1, 1), flip);

        mappedData.VertexPtr[3].Position = size with {
            X = 0
        };
        mappedData.VertexPtr[3].Color             = color;
        mappedData.VertexPtr[3].TexId             = Renderer.GetTextureId(tex);
        mappedData.VertexPtr[3].TextureCoordinate = FlipVector2(new Vector2(0, 1), flip);

        Matrix4x4 rotMat = Matrix4x4.CreateRotationZ(rotation);

        mappedData.VertexPtr[0].Position -= rotationOrigin;
        mappedData.VertexPtr[1].Position -= rotationOrigin;
        mappedData.VertexPtr[2].Position -= rotationOrigin;
        mappedData.VertexPtr[3].Position -= rotationOrigin;

        mappedData.VertexPtr[0].Position = Vector2.Transform(mappedData.VertexPtr[0].Position, rotMat);
        mappedData.VertexPtr[1].Position = Vector2.Transform(mappedData.VertexPtr[1].Position, rotMat);
        mappedData.VertexPtr[2].Position = Vector2.Transform(mappedData.VertexPtr[2].Position, rotMat);
        mappedData.VertexPtr[3].Position = Vector2.Transform(mappedData.VertexPtr[3].Position, rotMat);

        mappedData.VertexPtr[0].Position += position;
        mappedData.VertexPtr[1].Position += position;
        mappedData.VertexPtr[2].Position += position;
        mappedData.VertexPtr[3].Position += position;

        SetQuadIndices(mappedData);
    }

    public static unsafe void AllocateRotatedTexturedQuadWithSourceRect(this Renderer Renderer, VixieTexture tex,
                                                                        Vector2       position,
                                                                        Vector2       scale, float rotation,
                                                                        Vector2       rotationOrigin,
                                                                        Rectangle     sourceRect, Color color,
                                                                        TextureFlip   flip = TextureFlip.None) {
        Vector2 size = new(sourceRect.Width * scale.X, sourceRect.Height * scale.Y);

        if (tex.InternalFlip)
            sourceRect.Height *= -1;

        MappedData mappedData = Renderer.Reserve(4, 6);
        mappedData.VertexPtr[0] = new Vertex {
            Position = Vector2.Zero,
            Color    = color,
            TexId    = Renderer.GetTextureId(tex),
            TextureCoordinate =
                FlipVector2(new Vector2(sourceRect.X / (float)tex.Width, sourceRect.Y / (float)tex.Height), flip)
        };
        mappedData.VertexPtr[1] = new Vertex {
            Position = size with {
                Y = 0
            },
            Color = color,
            TexId = Renderer.GetTextureId(tex),
            TextureCoordinate =
                FlipVector2(new Vector2(sourceRect.Right / (float)tex.Width, sourceRect.Y / (float)tex.Height), flip)
        };
        mappedData.VertexPtr[2] = new Vertex {
            Position = size,
            Color    = color,
            TexId    = Renderer.GetTextureId(tex),
            TextureCoordinate =
                FlipVector2(new Vector2(sourceRect.Right / (float)tex.Width, sourceRect.Bottom / (float)tex.Height),
                            flip)
        };
        mappedData.VertexPtr[3] = new Vertex {
            Position = size with {
                X = 0
            },
            Color = color,
            TexId = Renderer.GetTextureId(tex),
            TextureCoordinate =
                FlipVector2(new Vector2(sourceRect.X / (float)tex.Width, sourceRect.Bottom / (float)tex.Height), flip)
        };

        Matrix4x4 rotMat = Matrix4x4.CreateRotationZ(rotation);

        mappedData.VertexPtr[0].Position -= rotationOrigin;
        mappedData.VertexPtr[1].Position -= rotationOrigin;
        mappedData.VertexPtr[2].Position -= rotationOrigin;
        mappedData.VertexPtr[3].Position -= rotationOrigin;

        mappedData.VertexPtr[0].Position = Vector2.Transform(mappedData.VertexPtr[0].Position, rotMat);
        mappedData.VertexPtr[1].Position = Vector2.Transform(mappedData.VertexPtr[1].Position, rotMat);
        mappedData.VertexPtr[2].Position = Vector2.Transform(mappedData.VertexPtr[2].Position, rotMat);
        mappedData.VertexPtr[3].Position = Vector2.Transform(mappedData.VertexPtr[3].Position, rotMat);

        mappedData.VertexPtr[0].Position += position;
        mappedData.VertexPtr[1].Position += position;
        mappedData.VertexPtr[2].Position += position;
        mappedData.VertexPtr[3].Position += position;

        SetQuadIndices(mappedData);
    }

    public static void DrawString(this Renderer Renderer, DynamicSpriteFont font,
                                  string        text,     Vector2           position, Color   color, float rotation,
                                  Vector2 scale, Vector2 origin = default(Vector2), TextStyle style = TextStyle.None,
                                  FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0
    ) {
        Guard.EnsureNonNull(Renderer.FontRenderer, "renderer.FontRenderer");

        font.DrawText(Renderer.FontRenderer, text, position,
                      color,
                      scale,
                      rotation,
                      origin,
                      0,
                      0,
                      0,
                      style,
                      effect,
                      effectAmount
        );
    }
    public static void DrawString(this Renderer    Renderer, DynamicSpriteFont font, string text, Vector2 position,
                                  Color            color,    TextStyle         style = TextStyle.None,
                                  FontSystemEffect effect = FontSystemEffect.None
    ) {
        Guard.EnsureNonNull(Renderer.FontRenderer, "renderer.FontRenderer");

        font.DrawText(Renderer.FontRenderer, text, position,
                      color);
    }
    public static void DrawString(this Renderer Renderer, DynamicSpriteFont font, string text, Vector2 position,
                                  FSColor[]     colors, float rotation, Vector2? scale, Vector2 origin,
                                  TextStyle     style = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None,
                                  int           effectAmount = 0
    ) {
        Guard.EnsureNonNull(Renderer.FontRenderer, "renderer.FontRenderer");

        font.DrawText(
        Renderer.FontRenderer,
        text,
        position,
        colors,
        scale,
        rotation,
        origin,
        0,
        0,
        0,
        style,
        effect,
        effectAmount
        );
    }
}