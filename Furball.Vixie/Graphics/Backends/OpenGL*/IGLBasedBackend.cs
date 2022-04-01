using Silk.NET.OpenGL;

namespace Furball.Vixie.Graphics.Backends.OpenGL_ {
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

        public        uint GenBuffer();
        public        void BindBuffer(BufferTargetARB    usage,      uint  buf);
        public unsafe void BufferData(BufferTargetARB    bufferType, nuint size,   void* data, BufferUsageARB bufferUsage);
        public unsafe void BufferSubData(BufferTargetARB bufferType, nint  offset, nuint size, void*          data);
        public        void DeleteBuffer(uint             bufferId);
        
        public void CheckError(string error = "");
    }
}
