using System;
using System.IO;
using Furball.Vixie.Backends.Shared;

namespace Furball.Vixie; 

public static class Resources {
    public static TextureRenderTarget CreateTextureRenderTarget(
        uint width, uint height, TextureParameters parameters = default
    ) {
        TextureRenderTarget target = GraphicsBackend.Current.CreateRenderTarget(width, height);

#if DEBUG
        Backends.Shared.Global.TRACKED_RENDER_TARGETS.Add(new WeakReference<TextureRenderTarget>(target));
#endif

        return target;
    }

    public static Texture CreateTextureFromByteArray(byte[] imageData, TextureParameters parameters = default) {
        Texture tex = GraphicsBackend.Current.CreateTextureFromByteArray(imageData, parameters);

#if DEBUG
        Backends.Shared.Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(tex));
#endif
        return tex;
    }

    public static Texture CreateWhitePixelTexture() {
        Texture tex = GraphicsBackend.Current.CreateWhitePixelTexture();

#if DEBUG
        Backends.Shared.Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(tex));
#endif

        return tex;
    }

    public static Texture CreateTextureFromStream(Stream stream, TextureParameters parameters = default) {
        Texture tex = GraphicsBackend.Current.CreateTextureFromStream(stream, parameters);

#if DEBUG
        Backends.Shared.Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(tex));
#endif

        return tex;
    }

    public static Texture CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default) {
        Texture tex = GraphicsBackend.Current.CreateEmptyTexture(width, height, parameters);

#if DEBUG
        Backends.Shared.Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(tex));
#endif

        return tex;
    }
}