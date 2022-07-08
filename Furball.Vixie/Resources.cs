using System;
using System.IO;
using Furball.Vixie.Backends.Shared;

namespace Furball.Vixie {
    public static class Resources {
        public static TextureRenderTarget CreateTextureRenderTarget(uint width, uint height) {
            TextureRenderTarget target = GraphicsBackend.Current.CreateRenderTarget(width, height);

#if DEBUG
            Backends.Shared.Global.TRACKED_RENDER_TARGETS.Add(new WeakReference<TextureRenderTarget>(target));
#endif

            return target;
        }
        
        public static Texture CreateTexture(byte[] imageData, bool qoi = false) {
            Texture tex = GraphicsBackend.Current.CreateTexture(imageData, qoi);

#if DEBUG
            Backends.Shared.Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(tex));
#endif
            return tex;
        }

        public static Texture CreateTexture() {
            Texture tex = GraphicsBackend.Current.CreateWhitePixelTexture();

#if DEBUG
            Backends.Shared.Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(tex));
#endif

            return tex;
        }

        public static Texture CreateTexture(Stream stream) {
            Texture tex = GraphicsBackend.Current.CreateTexture(stream);

#if DEBUG
            Backends.Shared.Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(tex));
#endif

            return tex;
        }

        public static Texture CreateTexture(uint width, uint height) {
            Texture tex = GraphicsBackend.Current.CreateTexture(width, height);

#if DEBUG
            Backends.Shared.Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(tex));
#endif

            return tex;
        }

        public static Texture CreateTexture(string filepath) {
            Texture tex = GraphicsBackend.Current.CreateTexture(filepath);

#if DEBUG
            Backends.Shared.Global.TRACKED_TEXTURES.Add(new WeakReference<Texture>(tex));
#endif

            return tex;
        }
    }
}
