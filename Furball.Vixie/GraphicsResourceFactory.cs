using System.IO;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using SixLabors.ImageSharp;

namespace Furball.Vixie;

public class GraphicsResourceFactory {
    private readonly GraphicsBackend _backend;

    public GraphicsResourceFactory(GraphicsBackend backend) {
        this._backend = backend;
    }

    public Texture CreateTextureFromByteArray(byte[] imageData, TextureParameters
                                                  parameters = default(TextureParameters)) {
        return Texture.CreateTextureFromByteArray(this._backend, imageData, parameters);
    }

    public Texture CreateWhitePixelTexture() {
        return Texture.CreateWhitePixelTexture(this._backend);
    }

    public Texture CreateTextureFromStream(Stream            stream,
                                           TextureParameters parameters = default(TextureParameters)) {
        return Texture.CreateTextureFromStream(this._backend, stream, parameters);
    }

    public Texture CreateEmptyTexture(uint              width, uint height,
                                      TextureParameters parameters = default(TextureParameters)) {
        return Texture.CreateEmptyTexture(this._backend, width, height, parameters);
    }

    public Texture CreateTextureFromImage(Image             image,
                                          TextureParameters parameters = default(TextureParameters)) {
        return Texture.CreateTextureFromImage(this._backend, image, parameters);
    }

    public RenderTarget CreateRenderTarget(uint width, uint height) {
        return new RenderTarget(this._backend, width, height);
    }

    public Renderer CreateRenderer() {
        return new Renderer(this._backend);
    }
}