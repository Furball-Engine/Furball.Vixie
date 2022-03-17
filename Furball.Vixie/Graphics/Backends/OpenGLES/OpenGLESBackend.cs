using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading;
using Furball.Vixie.Graphics.Backends.OpenGLES.Abstractions;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Helpers;
using Kettu;
using Silk.NET.Core.Native;
using Silk.NET.OpenGLES;
using Silk.NET.Windowing;

namespace Furball.Vixie.Graphics.Backends.OpenGLES {
    // ReSharper disable once InconsistentNaming
    public class OpenGLESBackend : GraphicsBackend {
        // ReSharper disable once InconsistentNaming
        private GL gl;

        internal Matrix4x4 ProjectionMatrix;

        private int _maxTextureUnits = -1;

        private static Thread _MainThread;

        [Conditional("DEBUG")]
        private void GetMainThread() {
            _MainThread = Thread.CurrentThread;
        }

        [Conditional("DEBUG")]
        internal void CheckThread() {
            if (Thread.CurrentThread != _MainThread) throw new ThreadStateException("You are calling GL on the wrong thread!");
        }
        
        public override void Initialize(IWindow window) {
            this.GetMainThread();

            this.gl = window.CreateOpenGLES();

#if DEBUGWITHGL
            unsafe {
                //Enables Debugging
                gl.Enable(EnableCap.DebugOutput);
                gl.Enable(EnableCap.DebugOutputSynchronous);
                gl.DebugMessageCallback(this.Callback, null);
            }
#endif

            //Enables Blending (Required for Transparent Objects)
            this.gl.Enable(EnableCap.Blend);
            this.gl.BlendFunc(GLEnum.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }
        
        [Conditional("DEBUG")]
        public void CheckError() {
            GLEnum error = this.gl.GetError();
            
            if (error != GLEnum.NoError) {
#if DEBUGWITHGL
                throw new Exception($"Got GL Error {error}!");
#else
                Debugger.Break();
#endif
            }
        }

        public override void Cleanup() {
            this.gl.Dispose();
        }

        public override void HandleWindowSizeChange(int width, int height) {
            this.gl.Viewport(0, 0, (uint) width, (uint) height);

            this.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, 1f, 0f);
        }

        public override void HandleFramebufferResize(int width, int height) {
            this.gl.Viewport(0, 0, (uint) width, (uint) height);
        }

        public override IQuadRenderer CreateTextureRenderer() {
            return new QuadRendererGL(this);
        }

        public override ILineRenderer CreateLineRenderer() {
            return new LineRendererGL(this);
        }

        public override int QueryMaxTextureUnits() {
            if (this._maxTextureUnits == -1) {
                this.gl.GetInteger(GetPName.MaxTextureImageUnits, out int maxTexSlots);
                this._maxTextureUnits = maxTexSlots;
            }

            return this._maxTextureUnits;
        }

        public override void Clear() {
            this.gl.Clear(ClearBufferMask.ColorBufferBit);
        }

        public override TextureRenderTarget CreateRenderTarget(uint width, uint height) {
            return new TextureRenderTargetGL(this, width, height);
        }

        public override Texture CreateTexture(byte[] imageData, bool qoi = false) {
            return new TextureGL(this, imageData, qoi);
        }

        public override Texture CreateTexture(Stream stream) {
            return new TextureGL(this, stream);
        }

        public override Texture CreateTexture(uint width, uint height) {
            return new TextureGL(this, width, height);
        }

        public override Texture CreateTexture(string filepath) {
            return new TextureGL(this, filepath);
        }

        public override Texture CreateWhitePixelTexture() {
            return new TextureGL(this);
        }

        public GL GetGlApi() => this.gl;

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
    }
}
