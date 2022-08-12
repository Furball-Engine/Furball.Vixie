using System.Drawing;
using System.IO;
using Furball.Vixie.Backends.Shared;

namespace Furball.Vixie.Backends.Direct3D9; 

public class TextureD3D9 : Texture {
    public TextureD3D9(byte[] imageData, TextureParameters parameters) {
        throw new System.NotImplementedException();
    }
    public TextureD3D9(Stream imageData, TextureParameters parameters) {
        throw new System.NotImplementedException();
    }
    public TextureD3D9(uint imageData, uint parameters, TextureParameters textureParameters) {
        throw new System.NotImplementedException();
    }
    public TextureD3D9() { //white pixel
        throw new System.NotImplementedException();
    }
    public override TextureFilterType FilterType {
        get;
        set;
    }
    
    public override Texture SetData <T>(T[] data) {
        throw new System.NotImplementedException();
    }
    
    public override Texture SetData <T>(T[] data, Rectangle rect) {
        throw new System.NotImplementedException();
    }
}