namespace Furball.Vixie.Backends.Shared;

public struct TextureParameters {
    public bool              RequestMipmaps;
    public TextureFilterType FilterType;

    public TextureParameters(bool requestMipmaps = false, TextureFilterType filterType = TextureFilterType.Smooth) {
        this.RequestMipmaps = requestMipmaps;
        this.FilterType     = filterType;
    }
}