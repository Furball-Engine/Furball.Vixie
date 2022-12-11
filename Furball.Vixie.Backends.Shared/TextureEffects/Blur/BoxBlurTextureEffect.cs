namespace Furball.Vixie.Backends.Shared.TextureEffects.Blur; 

public abstract class BoxBlurTextureEffect : TextureEffect {
    private readonly VixieTexture _sourceTex;

    protected BoxBlurTextureEffect(VixieTexture sourceTex) {
        this._sourceTex = sourceTex;
    }
    
    /// <summary>
    /// The radius from the center pixel to the edge of the blur area in pixels
    /// <remarks>
    /// 1 == 3x3, 2 == 5x5, 3 == 7x7, etc
    /// </remarks>
    /// </summary>
    public int KernelRadius = 2;

    /// <summary>
    /// The amount of passes to do over the image
    /// </summary>
    public int Passes = 3;
}