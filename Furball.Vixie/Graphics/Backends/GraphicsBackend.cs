using System;
using System.IO;
using System.Runtime.InteropServices;
using Furball.Vixie.Graphics.Backends.Direct3D11;
using Furball.Vixie.Graphics.Backends.OpenGL20;
using Furball.Vixie.Graphics.Backends.OpenGL41;
using Furball.Vixie.Graphics.Backends.OpenGLES;
using Furball.Vixie.Graphics.Backends.Veldrid;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Helpers;
using Kettu;
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
                Backend.OpenGLES   => new OpenGLESBackend(),
                Backend.Direct3D11 => new Direct3D11Backend(),
                Backend.OpenGL20   => new OpenGL20Backend(),
                Backend.OpenGL41   => new OpenGL41Backend(),
                Backend.Veldrid    => new VeldridBackend(),
                _                  => throw new ArgumentOutOfRangeException(nameof (backend), backend, "Invalid API")
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
        /// <summary>
        /// Presents
        /// </summary>
        public virtual void Present() {

        }

        public virtual void BeginScene() {

        }

        public virtual void EndScene() {

        }

        public static bool IsOnUnsupportedPlatform {
            get;
            private set;
        } = false;

        public static Backend PrefferedBackends = Backend.None;
        public static Backend GetReccomendedBackend() {
            bool preferVeldridOverNative  = PrefferedBackends.HasFlag(Backend.Veldrid);
            bool preferOpenGl             = PrefferedBackends.HasFlag(Backend.OpenGL41);
            bool preferOpenGlLegacy       = PrefferedBackends.HasFlag(Backend.OpenGL20);
            bool preferOpenGlesOverOpenGl = PrefferedBackends.HasFlag(Backend.OpenGLES);
            
            if (OperatingSystem.IsWindows()) {
                if (preferVeldridOverNative) 
                    return Backend.Veldrid;
                return preferOpenGlesOverOpenGl ? Backend.OpenGLES : preferOpenGlLegacy ? Backend.OpenGL20 : Backend.OpenGL41;
            }
            if (OperatingSystem.IsAndroid()) {
                return preferVeldridOverNative ? Backend.Veldrid : Backend.OpenGLES;
            }
            if (OperatingSystem.IsLinux()) {
                if (preferVeldridOverNative) 
                    return Backend.Veldrid;
                return preferOpenGlesOverOpenGl ? Backend.OpenGLES : preferOpenGlLegacy ? Backend.OpenGL20 : Backend.OpenGL41;
            }
            if (OperatingSystem.IsMacOSVersionAtLeast(10, 11)) { //Most models that run this version are guarenteed Metal support, so go with veldrid
                if (preferOpenGl) {
                    if(preferOpenGlesOverOpenGl)
                        Logger.Log("OpenGLES is considered unsupported on MacOS!", LoggerLevelDebugMessageCallback.InstanceNotification);

                    return preferOpenGlesOverOpenGl ? Backend.OpenGLES : preferOpenGlLegacy ? Backend.OpenGL20 : Backend.OpenGL41;
                }
                return Backend.Veldrid;
            }
            if(OperatingSystem.IsMacOS()) { //if we are on older macos, then we cant use Metal probably, so just stick with GL
                if (preferVeldridOverNative) 
                    return Backend.Veldrid; //we can just pray that Veldrid doesnt actually choose metal for these old things
                
                if(preferOpenGlesOverOpenGl)
                    Logger.Log("OpenGLES is considered unsupported on MacOS!", LoggerLevelDebugMessageCallback.InstanceNotification);

                return preferOpenGlesOverOpenGl ? Backend.OpenGLES : preferOpenGlLegacy ? Backend.OpenGL20 : Backend.OpenGL41;
            }
            if (OperatingSystem.IsFreeBSD()) {
                return preferOpenGlesOverOpenGl ? Backend.OpenGLES : preferOpenGlLegacy ? Backend.OpenGL20 : Backend.OpenGL41;
            }

            Logger.Log("You are running on an untested, unsupported platform!", LoggerLevelDebugMessageCallback.InstanceNotification);
            IsOnUnsupportedPlatform = true;
            return Backend.OpenGL20; //return the most supported backend
        }
    }
}
