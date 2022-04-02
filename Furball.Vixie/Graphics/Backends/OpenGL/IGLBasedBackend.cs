using System;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Graphics.Backends.OpenGL {
    public enum GLBackendType {
        Modern,
        ES,
        Legacy
    }
    
    public interface IGLBasedBackend {
        public GLBackendType GetType();
        
        public GL                        GetModernGL();
        public Silk.NET.OpenGL.Legacy.GL GetLegacyGL();
        public Silk.NET.OpenGLES.GL      GetGLES();

        public        uint   GenBuffer();
        public        void   BindBuffer(BufferTargetARB        usage,      uint  buf);
        public unsafe void   BufferData(BufferTargetARB        bufferType, nuint size,   void* data, BufferUsageARB bufferUsage);
        public unsafe void   BufferSubData(BufferTargetARB     bufferType, nint  offset, nuint size, void*          data);
        public        void   DeleteBuffer(uint                 bufferId);
        public        void   DeleteFramebuffer(uint            frameBufferId);
        public        void   DeleteTexture(uint                textureId);
        public        void   DeleteRenderbuffer(uint           bufId);
        public        void   DrawBuffers(uint                  i,           in GLEnum[] drawBuffers);
        public        void   BindFramebuffer(FramebufferTarget framebuffer, uint        frameBufferId);
        public        uint   GenFramebuffer();
        public        void   BindTexture(TextureTarget   target, uint   textureId);
        public unsafe void   TexImage2D(TextureTarget    target, int    level, InternalFormat format, uint width, uint height, int border, PixelFormat pxFormat, PixelType type, void* data);
        public        void   TexParameterI(TextureTarget target, GLEnum param, int            paramData);
        public        uint   GenRenderbuffer();
        public        void   Viewport(int x, int y, uint width, uint height);
        public        uint   GenTexture();
        public        void   BindRenderbuffer(RenderbufferTarget       target, uint                  id);
        public        void   RenderbufferStorage(RenderbufferTarget    target, InternalFormat        format,           uint               width,     uint height);
        public        void   FramebufferRenderbuffer(FramebufferTarget target, FramebufferAttachment attachment,       RenderbufferTarget rbTarget,  uint id);
        public        void   FramebufferTexture(FramebufferTarget      target, FramebufferAttachment colorAttachment0, uint               textureId, int  level);
        public        GLEnum CheckFramebufferStatus(FramebufferTarget  target);
        public        void   GetInteger(GetPName                       viewport, Span<int>            oldViewPort);
        public        void   TexParameter(TextureTarget                target,   TextureParameterName paramName, int param);
        public unsafe void   TexSubImage2D(TextureTarget               target,   int                  level,     int x, int y, uint width, uint height, PixelFormat pxformat, PixelType pxtype, void* data);
        public        void   ActiveTexture(TextureUnit                 textureSlot);
        
        public void CheckError(string         error = "");
    }
}
