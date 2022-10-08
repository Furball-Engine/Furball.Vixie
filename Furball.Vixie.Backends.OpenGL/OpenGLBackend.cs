using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using Furball.Vixie.Backends.OpenGL.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using Kettu;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Legacy.Extensions.APPLE;
using Silk.NET.OpenGL.Legacy.Extensions.EXT;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Rectangle=SixLabors.ImageSharp.Rectangle;
#if USE_IMGUI
#endif

namespace Furball.Vixie.Backends.OpenGL;

// ReSharper disable once InconsistentNaming
public class OpenGLBackend : GraphicsBackend, IGlBasedBackend {
    /// <summary>
    ///     OpenGL API
    /// </summary>
    // ReSharper disable once InconsistentNaming
    private GL gl;
    private Silk.NET.OpenGL.Legacy.GL _legacyGl;
    private Silk.NET.OpenGLES.GL      _gles;

    /// <summary>
    ///     Projection Matrix used to go from Window Coordinates to OpenGL Coordinates
    /// </summary>
    internal Matrix4x4 ProjectionMatrix;
    /// <summary>
    ///     Cache for the Maximum amount of Texture units allowed by the device
    /// </summary>
    private int _maxTextureUnits = -1;
    /// <summary>
    ///     ImGui Controller
    /// </summary>
    internal ShaderGl Shader;

#if USE_IMGUI
    private bool _runImGui = true;
#endif
    internal IView         View;
    private  bool          _screenshotQueued;
    internal Vector2D<int> CurrentViewport;
    private  Rectangle     _lastScissor;

    public static Dictionary<string, FeatureLevel> FeatureLevels = new() {
        {
            "glBindTextures", new FeatureLevel {
                Name        = "Use glBindTextures",
                Description = "Whether to use the glBindTextures function or to emulate it with multiple calls",
                Value       = false
            }
        }, {
            "NeedsFramebufferExtension", new FeatureLevel {
                Name        = "Require the ExtFramebufferObject extension",
                Description = "Whether we require the ExtFramebufferObject to run",
                Value       = false
            }
        }, {
            "NeedsCustomMipmapGeneration", new FeatureLevel {
                Name        = "Require custom mipmap generation",
                Description = "Whether we require a CPU side mipmap generation",
                Value       = true
            }
        }, {
            "BindlessMipmapGeneration", new FeatureLevel {
                Name        = "Use bindless mipmap generation",
                Description = "Whether we use glGenerateTextureMipmap to generate mipmaps",
                Value       = false
            }
        }, {
            "VertexArrayObjects", new FeatureLevel {
                Name = "Use Vertex Array Objects",
                Description = "Whether to use Vertex array objects when rendering",
                Value = false
            }
        }, {
            "AppleVertexArrayObjects", new FeatureLevel {
                Name = "Use the apple specific VAO extension",
                Description = "Whether to use the APPLE_vertex_array_object extension in place of ARB_vertex_array_object",
                Value = false
            }
        }, {
            "FixedFunctionPipeline", new FeatureLevel {
                Name = "Requires Fixed Function Pipeline",
                Description = "Whether to use the fixed function pipeline",
                Value = false
            }
        }
    };

    private ExtFramebufferObject   _framebufferObjectExt;
    private AppleVertexArrayObject _appleVao;

    private readonly  FeatureLevel _glBindTexturesFeatureLevel;
    private readonly  FeatureLevel _needsFrameBufferExtensionFeatureLevel;
    private readonly  FeatureLevel _needsCustomMipmapGenerationFeatureLevel;
    private readonly  FeatureLevel _bindlessMipmapGenerationFeatureLevel;
    internal readonly FeatureLevel VaoFeatureLevel;
    internal readonly FeatureLevel AppleVaoFeatureLevel;
    internal readonly FeatureLevel FixedFunctionPipeline;

    public readonly Backend CreationBackend;
    private         bool    _isFbProjMatrix;
#if USE_IMGUI
    private OpenGlImGuiController  _imgui;
#endif

    public OpenGLBackend(Backend backend) {
        this.CreationBackend = backend;

        this._glBindTexturesFeatureLevel              = FeatureLevels["glBindTextures"];
        this._needsFrameBufferExtensionFeatureLevel   = FeatureLevels["NeedsFramebufferExtension"];
        this._needsCustomMipmapGenerationFeatureLevel = FeatureLevels["NeedsCustomMipmapGeneration"];
        this._bindlessMipmapGenerationFeatureLevel    = FeatureLevels["BindlessMipmapGeneration"];
        this.VaoFeatureLevel                          = FeatureLevels["VertexArrayObjects"];
        this.AppleVaoFeatureLevel                     = FeatureLevels["AppleVertexArrayObjects"];
        this.FixedFunctionPipeline                    = FeatureLevels["FixedFunctionPipeline"];

        if (backend == Backend.OpenGLES) {
            if (Global.LatestSupportedGl.GLES.MajorVersion >= 2) {
                FeatureLevels["NeedsCustomMipmapGeneration"].Value = false;
                Logger.Log("Marking that we dont need custom mipmap generation!", LoggerLevelOpenGl.InstanceInfo);
            }
        } else {
            if (Global.LatestSupportedGl.GL.MajorVersion < 3 || Global.LatestSupportedGl.GL.MajorVersion == 3 &&
                Global.LatestSupportedGl.GL.MinorVersion                                                 < 2) {
                FeatureLevels["NeedsFramebufferExtension"].Value = true;
                Logger.Log("Marking that we require the ExtFramebufferObject!", LoggerLevelOpenGl.InstanceInfo);
            }

            if (Global.LatestSupportedGl.GL.MajorVersion >= 4 && Global.LatestSupportedGl.GL.MinorVersion >= 4 ||
                Global.LatestSupportedGl.GL.MajorVersion > 4) {
                FeatureLevels["glBindTextures"].Value = true;
                Logger.Log("Enabling multi-texture bind!", LoggerLevelOpenGl.InstanceInfo);
            }

            if (Global.LatestSupportedGl.GL.MajorVersion >= 3) {
                this.VaoFeatureLevel.Value = true;
                
                FeatureLevels["NeedsCustomMipmapGeneration"].Value = false;
                Logger.Log("Marking that we dont need custom mipmap generation!", LoggerLevelOpenGl.InstanceInfo);
            }
            else {
                Logger.Log("We need the ARB_vertex_array_object extension!", LoggerLevelOpenGl.InstanceInfo);
            }

            if (Global.LatestSupportedGl.GL.MajorVersion > 4 || Global.LatestSupportedGl.GL.MajorVersion == 4 &&
                Global.LatestSupportedGl.GL.MinorVersion                                                 >= 5) {
                FeatureLevels["BindlessMipmapGeneration"].Value = true;
                Logger.Log("Marking that we can use bindless mipmap generation!", LoggerLevelOpenGl.InstanceInfo);
            }

            if (Global.LatestSupportedGl.GL.MajorVersion < 2) {
                this.FixedFunctionPipeline.Value = true;
                Logger.Log("Marking that we need to use the fixed function pipeline!", LoggerLevelOpenGl.InstanceInfo);
            }
        }
    }

    /// <summary>
    ///     Used to Initialize the Backend
    /// </summary>
    /// <param name="view"></param>
    /// <param name="inputContext"></param>
    public override void Initialize(IView view, IInputContext inputContext) {
        this.gl        = view.CreateOpenGL();
        this._legacyGl = Silk.NET.OpenGL.Legacy.GL.GetApi(this.gl.Context);
        this._gles     = Silk.NET.OpenGLES.GL.GetApi(this.gl.Context);

        this.CheckError("create opengl");

        this.View = view;

#if DEBUGWITHGL
        unsafe {
            //Enables Debugging
            this.gl.Enable(EnableCap.DebugOutput);
            this.gl.Enable(EnableCap.DebugOutputSynchronous);
            this.gl.DebugMessageCallback(this.Callback, null);
        }
#endif

        //Enables Blending (Required for Transparent Objects)
        this.gl.Enable(EnableCap.Blend);
        this.CheckError("enable blend");
        this.gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        this.CheckError("enable srcalpha");

        this.gl.Enable(EnableCap.ScissorTest);

        this.gl.Enable(EnableCap.CullFace);
        this.gl.CullFace(CullFaceMode.Back);
        
#if USE_IMGUI
        OpenGlType type = this.CreationBackend switch {
            Backend.OpenGL when Global.LatestSupportedGl.GL.MajorVersion < 3 => OpenGlType.Legacy,
            Backend.OpenGLES                                                 => OpenGlType.Es,
            _                                                                => OpenGlType.Modern
        };

        this._imgui = new OpenGlImGuiController(this.gl, type, view, inputContext);
        this._imgui.Initialize();
        this.CheckError("create imguicontroller");
#endif

        BackendInfoSection mainSection = new("OpenGL Info");
        mainSection.Contents.Add(("OpenGL Version", this.gl.GetStringS(StringName.Version)));
        this.CheckError("check verison");
        mainSection.Contents.Add(("GLSL Version", this.gl.GetStringS(StringName.ShadingLanguageVersion)));
        this.CheckError("check glsl version");
        mainSection.Contents.Add(("OpenGL Vendor", this.gl.GetStringS(StringName.Vendor)));
        this.CheckError("check check vendor");
        mainSection.Contents.Add(("Renderer", this.gl.GetStringS(StringName.Renderer)));
        this.CheckError("check check renderer");
        if (Global.LatestSupportedGl.GL.MajorVersion >= 3) {
            bool foundExtFramebufferObject = false;
            Exception ex = new(
            "Your OpenGL version is too old and does not support the EXT_framebuffer_object extension! Try updating your video card drivers or try the Direct3D 11 and Vulkan backends!"
            );

            this.gl.GetInteger(GetPName.NumExtensions, out int numExtensions);
            for (uint i = 0; i < numExtensions; i++) {
                string extension = this.gl.GetStringS(GLEnum.Extensions, i);
                mainSection.Contents.Add(("Supported Extension", extension));

                if (extension.Contains("EXT_framebuffer_object"))
                    foundExtFramebufferObject = true;

                if (extension.Contains(AppleVertexArrayObject.ExtensionName) && !this.VaoFeatureLevel.Boolean) {
                    Logger.Log("Marking that we should be using the APPLE_vertex_array_object extension!", LoggerLevelOpenGl.InstanceInfo);

                    this.AppleVaoFeatureLevel.Value = true;
                }
                
                //If we have the ARB_vertex_array_object extension, always enable use of VAOs
                if (extension.Contains("ARB_vertex_array_object")) {
                    Logger.Log("Marking that we have the ARB_vertex_array_object extension!", LoggerLevelOpenGl.InstanceInfo);

                    this.VaoFeatureLevel.Value = true;

                    if (this.AppleVaoFeatureLevel.Boolean) {
                        Logger.Log("Using the ARB extension over the APPLE extension for VAOs!", LoggerLevelOpenGl.InstanceInfo);

                        this.AppleVaoFeatureLevel.Value = false;
                    }
                }
            }

            if (!foundExtFramebufferObject && FeatureLevels["NeedsFramebufferExtension"].Boolean)
                throw ex;

            if (this._needsFrameBufferExtensionFeatureLevel.Boolean)
                this._framebufferObjectExt = new ExtFramebufferObject(this._legacyGl.Context);
        } else {
            Exception ex = new(
            "Your OpenGL version is too old and does not support the EXT_framebuffer_object extension! Try updating your video card drivers or try the Direct3D 11 and Vulkan backends!"
            );

            string extensions = this.gl.GetStringS(StringName.Extensions);
            if (FeatureLevels["NeedsFramebufferExtension"].Boolean && !extensions.Contains("EXT_framebuffer_object"))
                throw ex;

            if (extensions.Contains(AppleVertexArrayObject.ExtensionName) && !this.VaoFeatureLevel.Boolean) {
                Logger.Log("Marking that we should be using the APPLE_vertex_array_object extension!", LoggerLevelOpenGl.InstanceInfo);

                this.AppleVaoFeatureLevel.Value = true;
            }
            
            if (extensions.Contains("ARB_vertex_array_object")) {
                Logger.Log("Marking that we have the ARB_vertex_array_object extension!", LoggerLevelOpenGl.InstanceInfo);
                this.VaoFeatureLevel.Value = true;
                
                if (this.AppleVaoFeatureLevel.Boolean) {
                    Logger.Log("Using the ARB extension over the APPLE extension for VAOs!", LoggerLevelOpenGl.InstanceInfo);

                    this.AppleVaoFeatureLevel.Value = false;
                }
            }
                
            this._framebufferObjectExt = new ExtFramebufferObject(this._legacyGl.Context);
            this._appleVao              = new AppleVertexArrayObject(this._legacyGl.Context);
        }
        this.CheckError("check extensions");
        this.InfoSections.Add(mainSection);

        this.InfoSections.ForEach(x => x.Log(LoggerLevelOpenGl.InstanceInfo));

        view.Closing += delegate {
#if USE_IMGUI
            this._runImGui = false;
#endif
        };
        this.CurrentViewport = new Vector2D<int>(view.FramebufferSize.X, view.FramebufferSize.Y);
        this._lastScissor    = new(0, 0, view.FramebufferSize.X, view.FramebufferSize.Y);

        if (!this.FixedFunctionPipeline.Boolean) {
            this.CreateShaders();
        
            this.gl.Enable(EnableCap.Multisample);
        }
        else {
            this.gl.Enable(EnableCap.Texture2D);
        }
    }
    private void CreateShaders() {
        Guard.Assert(!this.FixedFunctionPipeline.Boolean, "Cannot create shaders when fixed function pipeline is enabled!");
        
        this.Shader = new ShaderGl(this);
        this.Shader.AttachShader(
        ShaderType.VertexShader,
        ResourceHelpers.GetStringResource("Shaders/VertexShader.glsl", typeof(OpenGLBackend))
        );
        this.Shader.AttachShader(ShaderType.FragmentShader, RendererShaderGenerator.GetFragment(this));
        this.Shader.Link();

        this.Shader.Bind();
        for (int i = 0; i < this.QueryMaxTextureUnits(); i++)
            this.Shader.BindUniformToTexUnit($"tex_{i}", i);

        this.gl.BindAttribLocation(this.Shader.ProgramId, 0, "VertexPosition");
        this.gl.BindAttribLocation(this.Shader.ProgramId, 1, "TextureCoordinate");
        this.gl.BindAttribLocation(this.Shader.ProgramId, 2, "VertexColor");
        this.gl.BindAttribLocation(this.Shader.ProgramId, 3, "TextureId2");
        this.gl.BindAttribLocation(this.Shader.ProgramId, 4, "TextureId");

        this.Shader.Unbind();
    }

    public void CheckError(string message) {
        this.CheckErrorInternal(message);
    }
    public void GlCheckThread() {
        this.CheckThread();
    }
    public unsafe void Uniform2(int getUniformLocation, uint count, float* ptr) {
        this.gl.Uniform2(getUniformLocation, count, ptr);
    }
    public unsafe void Uniform1(int getUniformLocation, uint count, float* ptr) {
        this.gl.Uniform1(getUniformLocation, count, ptr);
    }
    public void VertexAttribDivisor(uint iOffset, uint currentElementInstanceDivisor) {
        this.gl.VertexAttribDivisor(iOffset, currentElementInstanceDivisor);
    }
    void IGlBasedBackend.GenerateMipmaps(VixieTextureGl vixieTextureGl) {
        if (this._needsCustomMipmapGenerationFeatureLevel.Boolean) {
            //TODO
        } else {
            if (this._bindlessMipmapGenerationFeatureLevel.Boolean) {
                this.gl.GenerateTextureMipmap(vixieTextureGl.TextureId);
            } else {
                vixieTextureGl.Bind();
                this.gl.GenerateMipmap(TextureTarget.Texture2D);
            }
        }
    }

    public unsafe void GetTexImage(TextureTarget target, int level, PixelFormat format, PixelType type, void* ptr) {
        this.gl.GetTexImage(target, level, format, type, ptr);
    }
    public void SetProjectionMatrixAndViewport(int targetWidth, int targetHeight, bool flip) {
        this._isFbProjMatrix = flip;
        this.HandleFramebufferResize(targetWidth, targetHeight);
    }

    /// <summary>
    ///     Checks for OpenGL errors
    /// </summary>
    /// <param name="erorr"></param>
    [Conditional("DEBUG")]
    private void CheckErrorInternal(string erorr = "") {
        GLEnum error = this.gl.GetError();

        if (error != GLEnum.NoError) {
#if DEBUGWITHGL
                throw new Exception($"Got GL Error {error}!");
#else
            Debugger.Break();
            Logger.Log($"OpenGL Error! Code: {error.ToString()} Extra Info: {erorr}", LoggerLevelOpenGl.InstanceError);
#endif
        }
    }
    /// <summary>
    ///     Used to Cleanup the Backend
    /// </summary>
    public override void Cleanup() {
        this.gl.Dispose();
    }
    /// <summary>
    ///     Used to handle the Framebuffer Resizing
    /// </summary>
    /// <param name="width">New width</param>
    /// <param name="height">New height</param>
    public override unsafe void HandleFramebufferResize(int width, int height) {
        this.gl.Viewport(0, 0, (uint)width, (uint)height);
        this.CurrentViewport = new Vector2D<int>(width, height);

        this.VerticalRatio = this._isFbProjMatrix ? 1 : height / 720f;

        float bottom = this._isFbProjMatrix ? 0f : 720f;
        float top    = this._isFbProjMatrix ? height : 0f;

        float right = this._isFbProjMatrix ? width : width / (float)height * 720f;

        //If we are using the fixed function pipeline, use glOrtho, otherwise, set the projection matrix on the shader
        this.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, right, bottom, top, 1f, 0f);

        //Copy it to a local var so we can get a reference to it
        Matrix4x4 mat = this.ProjectionMatrix;
        
        if (this.FixedFunctionPipeline.Boolean) {
            this._legacyGl.MatrixMode(Silk.NET.OpenGL.Legacy.MatrixMode.Projection);
            this._legacyGl.LoadIdentity();
            this._legacyGl.LoadMatrix((float*)&mat);
            
            this._legacyGl.MatrixMode(Silk.NET.OpenGL.Legacy.MatrixMode.Modelview);
            this._legacyGl.LoadIdentity();
        } else {
            this.Shader.Bind();
            this.Shader.SetUniform("ProjectionMatrix", this.ProjectionMatrix);
            this.Shader.Unbind();
        }
    }

    public override Rectangle ScissorRect {
        get => this._lastScissor;
        set {
            this.gl.Scissor(
            value.X,
            this.CurrentViewport.Y - value.Height - value.Y,
            (uint)value.Width,
            (uint)value.Height
            );
            this._lastScissor = value;
        }
    }
    public override void SetFullScissorRect() {
        this.ScissorRect = new(0, 0, this.CurrentViewport.X, this.CurrentViewport.Y);
    }
    public override ulong GetVramUsage() =>
        //TODO: figure out a way to get this info, AMD has dropped the ATI_meminfo extension
        //and Nvidia has not updated the NVX_gpu_memory_info to the modern core profile
        0;
    public override ulong    GetTotalVram()   => 0;
    public override Renderer CreateRenderer() => this.FixedFunctionPipeline.Boolean 
        ? new FixedFunctionOpenGLRenderer(this) 
        : new OpenGlRenderer(this);
    /// <summary>
    ///     Gets the Amount of Texture Units available for use
    /// </summary>
    /// <returns>Amount of Texture Units supported</returns>
    public override int QueryMaxTextureUnits() {
        if (this._maxTextureUnits == -1) {
            this.gl.GetInteger(GetPName.MaxTextureImageUnits, out int maxTexSlots);
            this.CheckError("get max tex slots");
            this._maxTextureUnits = maxTexSlots;
        }

        return this._maxTextureUnits;
    }
    /// <summary>
    ///     Clears the Screen
    /// </summary>
    public override void Clear() {
        this.gl.Clear(ClearBufferMask.ColorBufferBit);
        this.gl.ClearColor(0, 0, 0, 0);
    }
    public override void TakeScreenshot() {
        this._screenshotQueued = true;
    }
    public override unsafe void Present() {
        if (this._screenshotQueued) {
            this._screenshotQueued = false;

            int[] viewport = new int[4];

            this.gl.GetInteger(GetPName.Viewport, viewport);

            Rgba32[] colorArr = new Rgba32[viewport[2] * viewport[3]];

            fixed (void* ptr = colorArr) {
                this.gl.ReadPixels(
                viewport[0],
                viewport[1],
                (uint)viewport[2],
                (uint)viewport[3],
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                ptr
                );
            }

            Image img = Image.LoadPixelData(colorArr, viewport[2], viewport[3]);

            img = img.CloneAs<Rgb24>();
            img.Mutate(x => x.Flip(FlipMode.Vertical));

            this.InvokeScreenshotTaken(img);
        }
    }
    /// <summary>
    ///     Used to Create a TextureRenderTarget
    /// </summary>
    /// <param name="width">Width of the Target</param>
    /// <param name="height">Height of the Target</param>
    /// <returns></returns>
    public override VixieTextureRenderTarget CreateRenderTarget(uint width, uint height)
        => new VixieTextureRenderTargetGl(this, width, height);
    /// <summary>
    ///     Creates a Texture given some Data
    /// </summary>
    /// <param name="imageData">Image Data</param>
    /// <param name="parameters"></param>
    /// <returns>Texture</returns>
    public override VixieTexture CreateTextureFromByteArray(byte[] imageData, TextureParameters parameters = default)
        => new VixieTextureGl(this, imageData, parameters);
    /// <summary>
    ///     Creates a Texture given a Stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="parameters"></param>
    /// <returns>Texture</returns>
    public override VixieTexture CreateTextureFromStream(Stream stream, TextureParameters parameters = default)
        => new VixieTextureGl(this, stream, parameters);
    /// <summary>
    ///     Creates a Empty Texture given a Size
    /// </summary>
    /// <param name="width">Width of Texture</param>
    /// <param name="height">Height of Texture</param>
    /// <param name="parameters"></param>
    /// <returns>Texture</returns>
    public override VixieTexture CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default)
        => new VixieTextureGl(this, width, height, parameters);
    /// <summary>
    ///     Used to Create a 1x1 Texture with only a white pixel
    /// </summary>
    /// <returns>White Pixel Texture</returns>
    public override VixieTexture CreateWhitePixelTexture() => new VixieTextureGl(this);

#if USE_IMGUI
    /// <summary>
    ///     Used to Update the ImGuiController in charge of rendering ImGui on this backend
    /// </summary>
    /// <param name="deltaTime">Delta Time</param>
    public override void ImGuiUpdate(double deltaTime) {
        if (!this._runImGui)
            return;

        this._imgui.Update((float)deltaTime);
    }
    /// <summary>
    ///     Used to Draw the ImGuiController in charge of rendering ImGui on this backend
    /// </summary>
    /// <param name="deltaTime">Delta Time</param>
    public override void ImGuiDraw(double deltaTime) {
        if (!this._runImGui)
            return;

        this._imgui.Render();
    }
#endif
    /// <summary>
    ///     Debug Callback
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    // ReSharper disable once UnusedMember.Local
    private void Callback(
        GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userparam
    ) {
        string stringMessage = SilkMarshal.PtrToString(message);

        if (stringMessage!.Contains("Buffer detailed info:"))
            return;

        Console.WriteLine(stringMessage);
    }
    public float VerticalRatio {
        get;
        set;
    }
    public GL                        GetModernGl() => this.gl;
    public Silk.NET.OpenGL.Legacy.GL GetLegacyGl() => this._legacyGl;
    public Silk.NET.OpenGLES.GL      GetGles()     => this._gles;

    public uint GenBuffer() => this.gl.GenBuffer();

    public void BindBuffer(BufferTargetARB usage, uint buf) {
        this.gl.BindBuffer(usage, buf);
    }

    public unsafe void BufferData(BufferTargetARB bufferType, nuint size, void* data, BufferUsageARB bufferUsage) {
        this.gl.BufferData(bufferType, size, data, bufferUsage);
    }

    public unsafe void BufferSubData(BufferTargetARB bufferType, nint offset, nuint size, void* data) {
        this.gl.BufferSubData(bufferType, offset, size, data);
    }

    public void DeleteBuffer(uint bufferId) {
        this.gl.DeleteBuffer(bufferId);
    }

    public void ActiveTexture(TextureUnit textureSlot) {
        this.gl.ActiveTexture(textureSlot);
    }

    public void DeleteFramebuffer(uint frameBufferId) {
        if (this._needsFrameBufferExtensionFeatureLevel.Boolean) {
            this._framebufferObjectExt.DeleteFramebuffer(frameBufferId);
            return;
        }
        this.gl.DeleteFramebuffer(frameBufferId);
    }

    public void DeleteTexture(uint textureId) {
        Console.WriteLine($"deleting texture: {textureId}");
        this.gl.DeleteTexture(textureId);
    }

    public void DeleteRenderbuffer(uint bufId) {
        if (this._needsFrameBufferExtensionFeatureLevel.Boolean) {
            this._framebufferObjectExt.DeleteRenderbuffer(bufId);
            return;
        }
        this.gl.DeleteRenderbuffer(bufId);
    }

    public void DrawBuffers(uint i, in GLEnum[] drawBuffers) {
        this.gl.DrawBuffers(i, drawBuffers);
    }

    public void BindFramebuffer(FramebufferTarget framebuffer, uint frameBufferId) {
        if (this._needsFrameBufferExtensionFeatureLevel.Boolean) {
            this._framebufferObjectExt.BindFramebuffer(
            (Silk.NET.OpenGL.Legacy.FramebufferTarget)framebuffer,
            frameBufferId
            );
            return;
        }
        this.gl.BindFramebuffer(framebuffer, frameBufferId);
    }

    public uint GenFramebuffer() => this._needsFrameBufferExtensionFeatureLevel.Boolean
                                        ? this._framebufferObjectExt.GenFramebuffer() : this.gl.GenFramebuffer();

    public void BindTexture(TextureTarget target, uint textureId) {
        this.gl.BindTexture(target, textureId);
    }
    public void BindTextures(uint[] textures, uint count) {
        if (this._glBindTexturesFeatureLevel.Boolean)
            this.gl.BindTextures(0, count, textures);
        else
            for (int i = 0; i < count; i++) {
                uint texture = textures[i];
                this.gl.ActiveTexture(TextureUnit.Texture0 + i);
                this.gl.BindTexture(TextureTarget.Texture2D, texture);
            }
    }

    public unsafe void TexImage2D(
        TextureTarget target,   int       level, InternalFormat format, uint width, uint height, int border,
        PixelFormat   pxFormat, PixelType type,  void*          data
    ) {
        this.gl.TexImage2D(target, level, format, width, height, border, pxFormat, type, data);
    }

    public void TexParameterI(TextureTarget target, GLEnum param, int paramData) {
        this.gl.TexParameterI(target, param, paramData);
    }

    public uint GenRenderbuffer() => this._needsFrameBufferExtensionFeatureLevel.Boolean
                                         ? this._framebufferObjectExt.GenRenderbuffer() : this.gl.GenRenderbuffer();

    public void Viewport(int x, int y, uint width, uint height) {
        this.gl.Viewport(x, y, width, height);
    }

    public uint GenTexture() {
        uint var = this.gl.GenTexture();
        Console.WriteLine($"making tex: {var}");
        return var;
    }

    public void BindRenderbuffer(RenderbufferTarget target, uint id) {
        if (this._needsFrameBufferExtensionFeatureLevel.Boolean) {
            this._framebufferObjectExt.BindRenderbuffer((Silk.NET.OpenGL.Legacy.RenderbufferTarget)target, id);
            return;
        }
        this.gl.BindRenderbuffer(target, id);
    }

    public void RenderbufferStorage(RenderbufferTarget target, InternalFormat format, uint width, uint height) {
        if (this._needsFrameBufferExtensionFeatureLevel.Boolean) {
            this._framebufferObjectExt.RenderbufferStorage(
            (Silk.NET.OpenGL.Legacy.RenderbufferTarget)target,
            (Silk.NET.OpenGL.Legacy.InternalFormat)format,
            width,
            height
            );
            return;
        }
        this.gl.RenderbufferStorage(target, format, width, height);
    }

    public void FramebufferRenderbuffer(
        FramebufferTarget target, FramebufferAttachment attachment, RenderbufferTarget rbTarget, uint id
    ) {
        if (this._needsFrameBufferExtensionFeatureLevel.Boolean) {
            this._framebufferObjectExt.FramebufferRenderbuffer(
            (Silk.NET.OpenGL.Legacy.FramebufferTarget)target,
            (Silk.NET.OpenGL.Legacy.FramebufferAttachment)attachment,
            (Silk.NET.OpenGL.Legacy.RenderbufferTarget)rbTarget,
            id
            );
            return;
        }
        this.gl.FramebufferRenderbuffer(target, attachment, rbTarget, id);
    }

    public void FramebufferTexture(
        FramebufferTarget target, FramebufferAttachment colorAttachment0, uint textureId, int level
    ) {
        if (this._needsFrameBufferExtensionFeatureLevel.Boolean) {
            this._framebufferObjectExt.FramebufferTexture2D(
            (Silk.NET.OpenGL.Legacy.FramebufferTarget)target,
            (Silk.NET.OpenGL.Legacy.FramebufferAttachment)colorAttachment0,
            Silk.NET.OpenGL.Legacy.TextureTarget.Texture2D,
            textureId,
            level
            );
            return;
        }
        this.gl.FramebufferTexture(target, colorAttachment0, textureId, level);
    }

    public GLEnum CheckFramebufferStatus(FramebufferTarget target) {
        if (this._needsFrameBufferExtensionFeatureLevel.Boolean)
            return (GLEnum)this._framebufferObjectExt.CheckFramebufferStatus(
            (Silk.NET.OpenGL.Legacy.FramebufferTarget)target
            );
        return this.gl.CheckFramebufferStatus(target);
    }

    public void GetInteger(GetPName viewport, ref int[] oldViewPort) {
        this.gl.GetInteger(viewport, oldViewPort);
    }

    public void TexParameter(TextureTarget target, TextureParameterName paramName, int param) {
        this.gl.TexParameter(target, paramName, param);
    }

    public unsafe void TexSubImage2D(
        TextureTarget target, int level, int x, int y, uint width, uint height, PixelFormat pxformat, PixelType pxtype,
        void*         data
    ) {
        this.gl.TexSubImage2D(target, level, x, y, width, height, pxformat, pxtype, data);
    }

    public uint CreateProgram() => this.gl.CreateProgram();

    public uint CreateShader(ShaderType type) => this.gl.CreateShader(type);

    public void ShaderSource(uint shaderId, string source) {
        this.gl.ShaderSource(shaderId, source);
    }

    public void CompileShader(uint shaderId) {
        this.gl.CompileShader(shaderId);
    }

    public string GetShaderInfoLog(uint shaderId) => this.gl.GetShaderInfoLog(shaderId);

    public void AttachShader(uint programId, uint shaderId) {
        this.gl.AttachShader(programId, shaderId);
    }

    public void LinkProgram(uint programId) {
        this.gl.LinkProgram(programId);
    }

    public void GetProgram(uint programId, ProgramPropertyARB linkStatus, out int i) {
        this.gl.GetProgram(programId, linkStatus, out i);
    }

    public void DeleteShader(uint shader) {
        this.gl.DeleteShader(shader);
    }

    public void GetShader(uint shaderId, ShaderParameterName paramName, out int returnValue) {
        this.gl.GetShader(shaderId, paramName, out returnValue);
    }

    public string GetProgramInfoLog(uint programId) => this.gl.GetProgramInfoLog(programId);

    public void UseProgram(uint programId) {
        this.gl.UseProgram(programId);
    }

    public int GetUniformLocation(uint programId, string uniformName)
        => this.gl.GetUniformLocation(programId, uniformName);

    public unsafe void UniformMatrix4(int getUniformLocation, uint i, bool b, float* f) {
        this.gl.UniformMatrix4(getUniformLocation, i, b, f);
    }

    public void Uniform1(int getUniformLocation, float f) {
        this.gl.Uniform1(getUniformLocation, f);
    }

    public void Uniform2(int getUniformLocation, float f, float f2) {
        this.gl.Uniform2(getUniformLocation, f, f2);
    }

    public void Uniform1(int getUniformLocation, int f) {
        this.gl.Uniform1(getUniformLocation, f);
    }

    public void Uniform2(int getUniformLocation, int f, int f2) {
        this.gl.Uniform2(getUniformLocation, f, f2);
    }
    public unsafe void Uniform4(int getUniformLocation, uint count, float* ptr) {
        this.gl.Uniform4(getUniformLocation, count, ptr);
    }

    public void DeleteProgram(uint programId) {
        this.gl.DeleteProgram(programId);
    }

    public void DeleteVertexArray(uint arrayId) {
        if (this.AppleVaoFeatureLevel.Boolean)
            this._appleVao.GenVertexArray();
        else
            this.gl.DeleteVertexArray(arrayId);
    }

    public uint GenVertexArray() {
        return this.AppleVaoFeatureLevel.Boolean ? this._appleVao.GenVertexArray() : this.gl.GenVertexArray();
    }

    public void EnableVertexAttribArray(uint u) {
        this.gl.EnableVertexAttribArray(u);
    }

    public unsafe void VertexAttribPointer(
        uint u, int currentElementCount, VertexAttribPointerType currentElementType, bool currentElementNormalized,
        uint getStride, void* offset
    ) {
        this.gl.VertexAttribPointer(
        u,
        currentElementCount,
        (GLEnum)currentElementType,
        currentElementNormalized,
        getStride,
        offset
        );
    }

    public unsafe void VertexAttribIPointer(
        uint u, int currentElementCount, VertexAttribIType vertexAttribIType, uint getStride, void* offset
    ) {
        this.gl.VertexAttribIPointer(u, currentElementCount, (GLEnum)vertexAttribIType, getStride, offset);
    }

    public void BindVertexArray(uint arrayId) {
        this.gl.BindVertexArray(arrayId);
    }
}