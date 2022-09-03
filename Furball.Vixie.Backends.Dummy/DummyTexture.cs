using System;
using Furball.Vixie.Backends.Shared;
using Kettu;
using Silk.NET.Maths;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle = System.Drawing.Rectangle;

namespace Furball.Vixie.Backends.Dummy;

public class DummyTexture : VixieTexture {
    public DummyTexture(int w, int h) {
        this.Size = new Vector2D<int>(w, h);
        
        Logger.Log($"Creating Dummy texture({w}x{h})", LoggerLevelDummy.InstanceInfo);
    }
    public override TextureFilterType FilterType {
        get;
        set;
    }
    public override bool Mipmaps {
        get;
    }
    public override VixieTexture SetData <pT>(ReadOnlySpan<pT> data) {
        // throw new NotImplementedException();
        return this;
    }
    public override VixieTexture SetData <pT>(ReadOnlySpan<pT> data, Rectangle rect) {
        // throw new NotImplementedException();
        return this;
    }
    public override Rgba32[] GetData() {
        // throw new NotImplementedException();
        return Array.Empty<Rgba32>();
    }
}