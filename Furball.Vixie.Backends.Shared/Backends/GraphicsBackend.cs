using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Backends.Shared.TextureEffects.Blur;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using Rectangle=SixLabors.ImageSharp.Rectangle;

namespace Furball.Vixie.Backends.Shared.Backends;

/// <summary>
/// Specification for a Graphics Backend
/// </summary>
public abstract class GraphicsBackend {
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
    /// Creates a new renderer from the backend
    /// </summary>
    /// <returns>Returns the new renderer</returns>
    public abstract VixieRenderer CreateRenderer();
    public abstract BoxBlurTextureEffect CreateBoxBlurTextureEffect(VixieTexture source);
    /// <summary>
    /// The maximum texture size supported by the backend
    /// </summary>
    public abstract Vector2D<int> MaxTextureSize { get; }
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
    public event EventHandler<Image>? ScreenshotTaken;
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

    #region Debug info

    public abstract ulong GetVramUsage();
    public abstract ulong GetTotalVram();

    #endregion

    #region Render Targets

    /// <summary>
    /// Used to Create a TextureRenderTarget
    /// </summary>
    /// <param name="width">Width of the Target</param>
    /// <param name="height">Height of the Target</param>
    /// <returns></returns>
    public abstract VixieTextureRenderTarget CreateRenderTarget(uint width, uint height);

    #endregion

    #region Textures

    /// <summary>
    /// Creates a Texture given some Data
    /// </summary>
    /// <param name="imageData">Image Data</param>
    /// <param name="parameters"></param>
    /// <returns>Texture</returns>
    public abstract VixieTexture CreateTextureFromByteArray(byte[] imageData, TextureParameters parameters = default);
    /// <summary>
    /// Creates a Texture given a Stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="parameters"></param>
    /// <returns>Texture</returns>
    public abstract VixieTexture CreateTextureFromStream(Stream stream, TextureParameters parameters = default);
    /// <summary>
    /// Creates a Empty Texture given a Size
    /// </summary>
    /// <param name="width">Width of Texture</param>
    /// <param name="height">Height of Texture</param>
    /// <param name="parameters"></param>
    /// <returns>Texture</returns>
    public abstract VixieTexture CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default);
    /// <summary>
    /// Used to Create a 1x1 Texture with only a white pixel
    /// </summary>
    /// <returns>White Pixel Texture</returns>
    public abstract VixieTexture CreateWhitePixelTexture();

    #endregion

#if USE_IMGUI
    #region Imgui
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
    #endregion
#endif

    /// <summary>
    /// Presents
    /// </summary>
    public virtual void Present() {
        Tracy.Tracy.EmitFrameMark("Present");
    }

    public virtual void BeginScene() {}

    public virtual void EndScene() {}

    public List<BackendInfoSection> InfoSections = new();
}