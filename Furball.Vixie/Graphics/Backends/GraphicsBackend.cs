using System;
using System.IO;
using Furball.Vixie.Graphics.Backends.OpenGL20;
using Furball.Vixie.Graphics.Backends.OpenGL41;
using Furball.Vixie.Graphics.Backends.OpenGLES;
using Furball.Vixie.Graphics.Renderers;
using Silk.NET.Windowing;

namespace Furball.Vixie.Graphics.Backends {
    /// <summary>
    /// Specification for a Graphics Backend
    /// </summary>
    public abstract class GraphicsBackend {
        /// <summary>
        /// Represents the Currently used Graphics Backend
        /// </summary>
        public static GraphicsBackend Current;
        /// <summary>
        /// Sets the Graphics Backend
        /// </summary>
        /// <param name="backend">What backend to use</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws if a Invalid API was chosen</exception>
        public static void SetBackend(Backend backend) {
            Current = backend switch {
                Backend.OpenGLES => new OpenGLESBackend(),
                Backend.OpenGL20 => new OpenGL20Backend(),
                Backend.OpenGL41 => new OpenGL41Backend(),
                _                => throw new ArgumentOutOfRangeException(nameof (backend), backend, "Invalid API")
            };
        }
        /// <summary>
        /// Used to Initialize the Backend
        /// </summary>
        /// <param name="window"></param>
        public abstract void Initialize(IWindow window);
        /// <summary>
        /// Used to Cleanup the Backend
        /// </summary>
        public abstract void Cleanup();
        /// <summary>
        /// Used to Handle the Window size Changing
        /// </summary>
        /// <param name="width">New width</param>
        /// <param name="height">New height</param>
        public abstract void HandleWindowSizeChange(int width, int height);
        /// <summary>
        /// Used to handle the Framebuffer Resizing
        /// </summary>
        /// <param name="width">New width</param>
        /// <param name="height">New height</param>
        public abstract void HandleFramebufferResize(int width, int height);
        /// <summary>
        /// Used to Create a Texture Renderer
        /// </summary>
        /// <returns>A Texture Renderer</returns>
        public abstract IQuadRenderer CreateTextureRenderer();
        /// <summary>
        /// Used to Create a Line Renderer
        /// </summary>
        /// <returns></returns>
        public abstract ILineRenderer CreateLineRenderer();
        /// <summary>
        /// Gets the Amount of Texture Units available for use
        /// </summary>
        /// <returns>Amount of Texture Units supported</returns>
        public abstract int QueryMaxTextureUnits();
        /// <summary>
        /// Clears the Screen
        /// </summary>
        public abstract void Clear();

        //Render Targets

        /// <summary>
        /// Used to Create a TextureRenderTarget
        /// </summary>
        /// <param name="width">Width of the Target</param>
        /// <param name="height">Height of the Target</param>
        /// <returns></returns>
        public abstract TextureRenderTarget CreateRenderTarget(uint width, uint height);

        //Textures

        /// <summary>
        /// Creates a Texture given some Data
        /// </summary>
        /// <param name="imageData">Image Data</param>
        /// <param name="qoi">Is the Data in the QOI format?</param>
        /// <returns>Texture</returns>
        public abstract Texture CreateTexture(byte[] imageData, bool qoi = false);
        /// <summary>
        /// Creates a Texture given a Stream
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <returns>Texture</returns>
        public abstract Texture CreateTexture(Stream stream);
        /// <summary>
        /// Creates a Empty Texture given a Size
        /// </summary>
        /// <param name="width">Width of Texture</param>
        /// <param name="height">Height of Texture</param>
        /// <returns>Texture</returns>
        public abstract Texture CreateTexture(uint width, uint height);
        /// <summary>
        /// Creates a Texture from a File
        /// </summary>
        /// <param name="filepath">Filepath to Image</param>
        /// <returns>Texture</returns>
        public abstract Texture CreateTexture(string filepath);
        /// <summary>
        /// Used to Create a 1x1 Texture with only a white pixel
        /// </summary>
        /// <returns>White Pixel Texture</returns>
        public abstract Texture CreateWhitePixelTexture();

        //Imgui

        /// <summary>
        /// Used to Update the ImGuiController in charge of rendering ImGui on this backend
        /// </summary>
        /// <param name="deltaTime">Delta Time</param>
        public abstract void ImGuiUpdate(double deltaTime);
        /// <summary>
        /// Used to Draw the ImGuiController in charge of rendering ImGui on this backend
        /// </summary>
        /// <param name="deltaTime">Delta Time</param>
        public abstract void ImGuiDraw(double deltaTime);
    }
}
