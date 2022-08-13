using System;
using System.IO;
using Furball.Vixie.Backends.Shared;

namespace Furball.Vixie; 

public static class Resources {
    public static RenderTarget CreateTextureRenderTarget(
        uint width, uint height, TextureParameters parameters = default
    ) {
        VixieTextureRenderTarget target = GraphicsBackend.Current.CreateRenderTarget(width, height);

        RenderTarget managedTarget = new(target);
        
        Global.TRACKED_RENDER_TARGETS.Add(new WeakReference<RenderTarget>(managedTarget));

        return managedTarget;
    }

    public static Texture CreateTextureFromByteArray(byte[] imageData, TextureParameters parameters = default) {
        VixieTexture tex = GraphicsBackend.Current.CreateTextureFromByteArray(imageData, parameters);

        Texture managedTex = new(tex);
        
        Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(managedTex));
        
        return managedTex;
    }

    public static Texture CreateWhitePixelTexture() {
        VixieTexture tex = GraphicsBackend.Current.CreateWhitePixelTexture();

        Texture managedTex = new(tex);

        Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(managedTex));

        return managedTex;
    }

    public static Texture CreateTextureFromStream(Stream stream, TextureParameters parameters = default) {
        VixieTexture tex = GraphicsBackend.Current.CreateTextureFromStream(stream, parameters);

        Texture managedTex = new(tex);

        Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(managedTex));

        return managedTex;
    }

    public static Texture CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default) {
        VixieTexture tex = GraphicsBackend.Current.CreateEmptyTexture(width, height, parameters);

        Texture managedTex = new(tex);

        Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(managedTex));

        return managedTex;
    }
}