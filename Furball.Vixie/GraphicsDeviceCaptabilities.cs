using System.Numerics;
using Silk.NET.OpenGL;

namespace Furball.Vixie {
    public class GraphicsDeviceCaptabilities {
        public int MaxTextureImageUnits { get; internal set; }
        public int MaxTextureSize { get; internal set; }
        public Vector2 MaxFrameBufferSize { get; internal set; }
        public int MaxVertexCount { get; internal set; }
        public int MaxIndexCount { get; internal set; }
        public int GlMajorVersion { get; internal set; }
        public int GlMinorVersion { get; internal set; }
        public string GlVersionString => $"{GlMajorVersion}.{GlMinorVersion}";

        internal GraphicsDeviceCaptabilities(GL gl) {
            gl.GetInteger(GetPName.MaxTextureImageUnits, out int maxTexSlots);
            gl.GetInteger(GetPName.MaxTextureSize,       out int maxTexSize);

            gl.GetInteger(GetPName.MaxFramebufferWidth,  out int maxFrameBufWidth);
            gl.GetInteger(GetPName.MaxFramebufferHeight, out int maxFrameBufHeight);

            gl.GetInteger(GetPName.MaxElementsVertices,  out int maxVerticies);
            gl.GetInteger(GetPName.MaxElementsIndices,   out int maxIndicies);

            gl.GetInteger(GetPName.MajorVersion, out int glMajorVersion);
            gl.GetInteger(GetPName.MinorVersion, out int glMinorVersion);

            this.MaxTextureImageUnits = maxTexSlots;
            this.MaxTextureSize       = maxTexSize;
            this.MaxFrameBufferSize   = new Vector2(maxFrameBufWidth, maxFrameBufHeight);
            this.MaxVertexCount       = maxVerticies;
            this.MaxIndexCount        = maxIndicies;
            this.GlMajorVersion       = glMajorVersion;
            this.GlMinorVersion       = glMinorVersion;
        }
    }
}
