using System;
using System.IO;
using Furball.Vixie.Graphics.Backends.OpenGL;
using Furball.Vixie.Graphics.Renderers;
using Silk.NET.Windowing;

namespace Furball.Vixie.Graphics.Backends {
    public abstract class GraphicsBackend {
        public static GraphicsBackend Current;

        public static void SetBackend(Backend backend) {
            Current = backend switch {
                Backend.OpenGLES => new OpenGLESBackend(),
                _                => throw new ArgumentOutOfRangeException(nameof(backend), backend, "Invalid API")
            };
        }

        public abstract void Initialize(IWindow window);
        public abstract void Cleanup();
        public abstract void HandleWindowSizeChange(int width, int height);
        public abstract void HandleFramebufferResize(int width, int height);
        public abstract IQuadRenderer CreateTextureRenderer();
        public abstract ILineRenderer CreateLineRenderer();
        public abstract int QueryMaxTextureUnits();
        public abstract void Clear();

        //Render Targets

        public abstract TextureRenderTarget CreateRenderTarget(uint width, uint height);


        //Textures

        public abstract Texture CreateTexture(byte[] imageData, bool qoi = false);
        public abstract Texture CreateTexture(Stream stream);
        public abstract Texture CreateTexture(uint width, uint height);
        public abstract Texture CreateTexture(string filepath);
        public abstract Texture CreateWhitePixelTexture();
    }
}
