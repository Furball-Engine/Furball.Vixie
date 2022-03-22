using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Numerics;
using Furball.Vixie.Graphics.Backends.OpenGL20.Abstractions;
using Furball.Vixie.Graphics.Backends.OpenGL41;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Helpers;
using Kettu;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy.Extensions.EXT;
using Silk.NET.OpenGL.Legacy.Extensions.ImGui;
using Silk.NET.Windowing;

namespace Furball.Vixie.Graphics.Backends.OpenGL20 {
    public class OpenGL20Backend : GraphicsBackend {
        private GL gl;

        private ImGuiController _imgui;
        
        public  Matrix4x4       ProjectionMatrix;

        /// <summary>
        /// Checks for OpenGL errors
        /// </summary>
        /// <param name="erorr"></param>
        [Conditional("DEBUG")]
        public void CheckError(string erorr = "") {
            GLEnum error = this.gl.GetError();
            
            if (error != GLEnum.NoError) {
#if DEBUGWITHGL
#else
                throw new Exception($"Got GL Error {error}!");
                Debugger.Break();
                Logger.Log($"OpenGL Error! Code: {error.ToString()} Extra Info: {erorr}", LoggerLevelOpenGL20.InstanceError);
#endif
            }
        }
        
        public override void Initialize(IWindow window) {
            this.gl = window.CreateLegacyOpenGL();

            //TODO: Lets just assume they have it for now :^)
            this.framebufferObjectEXT = new ExtFramebufferObject(this.gl.Context);

#if DEBUGWITHGL
            unsafe {
                //Enables Debugging
                gl.Enable(EnableCap.DebugOutput);
                gl.Enable(EnableCap.DebugOutputSynchronous);
                gl.DebugMessageCallback(this.Callback, null);
            }
#endif
            
            //Setup blend mode
            this.gl.Enable(EnableCap.Blend);
            this.gl.BlendFunc(GLEnum.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            
            this.gl.Enable(EnableCap.Texture2D);
            
            Logger.Log($"OpenGL Version: {this.gl.GetStringS(StringName.Version)}",                LoggerLevelOpenGL20.InstanceInfo);
            Logger.Log($"GLSL Version:   {this.gl.GetStringS(StringName.ShadingLanguageVersion)}", LoggerLevelOpenGL20.InstanceInfo);
            Logger.Log($"OpenGL Vendor:  {this.gl.GetStringS(StringName.Vendor)}",                 LoggerLevelOpenGL20.InstanceInfo);
            Logger.Log($"Renderer:       {this.gl.GetStringS(StringName.Renderer)}",               LoggerLevelOpenGL20.InstanceInfo);

            this._imgui = new ImGuiController(this.gl, Global.GameInstance.WindowManager.GameWindow, Global.GameInstance._inputContext);
        }
        
        /// <summary>
        /// Debug Callback
        /// </summary>
        private void Callback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userparam) {
            string stringMessage = SilkMarshal.PtrToString(message);

            LoggerLevel level = severity switch {
                GLEnum.DebugSeverityHigh         => LoggerLevelDebugMessageCallback.InstanceHigh,
                GLEnum.DebugSeverityMedium       => LoggerLevelDebugMessageCallback.InstanceMedium,
                GLEnum.DebugSeverityLow          => LoggerLevelDebugMessageCallback.InstanceLow,
                GLEnum.DebugSeverityNotification => LoggerLevelDebugMessageCallback.InstanceNotification,
                _                                => null
            };

            Console.WriteLine(stringMessage);
        }
        
        public override void Cleanup() {
            this.gl.Dispose();
        }

        public override void HandleWindowSizeChange(int width, int height) {
            this.gl.Viewport(0, 0, (uint)width, (uint)height);
            
            this.gl.MatrixMode(MatrixMode.Projection);
            this.gl.LoadIdentity();
            this.gl.Ortho(0, width, height, 0, 0, 1);
            
            this.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
        }
        
        public override void HandleFramebufferResize(int width, int height) {
            this.gl.Viewport(0, 0, (uint)width, (uint)height);
            
            this.gl.MatrixMode(MatrixMode.Projection);
            this.gl.LoadIdentity();
            this.gl.Ortho(0, width, height, 0, 0, 1);

            this.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
        }
        
        public override IQuadRenderer CreateTextureRenderer() => new QuadRendererGL20(this);
        public override ILineRenderer CreateLineRenderer()    => new LineRendererGL20(this);

        private int    _maxTexUnits = -1;
        private ExtFramebufferObject framebufferObjectEXT;
        public override int QueryMaxTextureUnits() {
            if (this._maxTexUnits == -1)
                this._maxTexUnits = this.gl.GetInteger((GLEnum)GetPName.MaxTextureImageUnits);

            return this._maxTexUnits;
        }
        public override void Clear() {
            this.gl.Clear(ClearBufferMask.ColorBufferBit);
            this.gl.ClearColor(0.5f, 0, 0, 0);
        }

        public override TextureRenderTarget CreateRenderTarget(uint width, uint height) => new TextureRenderTargetGL20(this, width, height);

        public override Texture CreateTexture(byte[] imageData, bool qoi = false) => new TextureGL20(this, imageData, qoi);

        public override Texture CreateTexture(Stream stream) => new TextureGL20(this, stream);

        public override Texture CreateTexture(uint width, uint height) => new TextureGL20(this, width, height);

        public override Texture CreateTexture(string filepath) => new TextureGL20(this, filepath);

        public override Texture CreateWhitePixelTexture() => new TextureGL20(this);

        [Pure]
        public GL GetOpenGL() => this.gl;
        [Pure]
        public ExtFramebufferObject GetOpenGLFramebufferEXT() => this.framebufferObjectEXT;
        
        public override void ImGuiUpdate(double deltaTime) {
            this._imgui.Update((float)deltaTime);
        }
        public override void ImGuiDraw(double deltaTime) {
            this._imgui.Render();
        }
    }
}
