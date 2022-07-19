using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Furball.Vixie.Backends.Shared.Renderers;
using Silk.NET.Input;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;

namespace Furball.Vixie.Backends.Shared.Backends; 

/// <summary>
/// Specification for a Graphics Backend
/// </summary>
public abstract class IGraphicsBackend {
    /// <summary>
    /// Used to Initialize the Backend
    /// </summary>
    /// <param name="view"></param>
    /// <param name="inputContext"></param>
    public abstract void Initialize(IView view, IInputContext inputContext);
    /// <summary>
    /// Used to Cleanup the Backend
    /// </summary>
    public abstract void Cleanup();
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
    /// <summary>
    /// Takes a screenshot
    /// </summary>
    public abstract void TakeScreenshot();
    /// <summary>
    /// Called when a screenshot is completed
    /// </summary>
    public event EventHandler<Image> ScreenshotTaken;
    protected void InvokeScreenshotTaken(Image image) {
        this.ScreenshotTaken?.Invoke(this, image);
    }

    public Thread? MainThread {
        get;
        protected set;
    }
    public void SetMainThread() {
        this.MainThread = Thread.CurrentThread;
    }
    [Conditional("DEBUG")]
    public void CheckThread() {
        if (MainThread != Thread.CurrentThread)
            throw new ThreadStateException("You can only run this function on the main thread!");
    }
    /// <summary>
    ///     Sets the scissor rectangle, top left origin going down
    /// </summary>
    public abstract Rectangle ScissorRect { get; set; }
    /// <summary>
    ///     Resets the scissor rect to the full window
    /// </summary>
    public abstract void SetFullScissorRect();

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
    /// <summary>
    /// Presents
    /// </summary>
    public virtual void Present() {

    }

    public virtual void BeginScene() {

    }

    public virtual void EndScene() {

    }

    public List<BackendInfoSection> InfoSections = new();
}