using System;
using System.Drawing;
using Furball.Vixie.Backends.Shared;
using SixLabors.ImageSharp.PixelFormats;

namespace Furball.Vixie.Backends.WebGL; 

// ReSharper disable once InconsistentNaming
public class WebGLTexture : VixieTexture {
    public override TextureFilterType FilterType {
        get;
        set;
    }
    public override bool Mipmaps {
        get;
    }
    public override VixieTexture SetData <pT>(ReadOnlySpan<pT> data) {
        throw new NotImplementedException();
    }
    public override VixieTexture SetData <pT>(ReadOnlySpan<pT> data, Rectangle rect) {
        throw new NotImplementedException();
    }
    public override Rgba32[] GetData() {
        throw new NotImplementedException();
    }
}