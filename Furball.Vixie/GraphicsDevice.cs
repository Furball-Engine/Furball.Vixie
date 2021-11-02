using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;

namespace Furball.Vixie {
    public class GraphicsDevice {
        /// <summary>
        /// How many Texture Slots Total does the Graphics Device Support
        /// </summary>
        public int MaxTextureImageUnits { get; internal set; }
        /// <summary>
        /// How big are the Textures allowed to be at max?
        /// </summary>
        public int MaxTextureSize { get; internal set; }
        /// <summary>
        /// Maximum Frame Buffer Size
        /// </summary>
        public Vector2 MaxFrameBufferSize { get; internal set; }
        /// <summary>
        /// Maximum amount of Verticies allowed by the Graphics Device
        /// </summary>
        public int MaxVertexCount { get; internal set; }
        /// <summary>
        /// Maximum amount of Indicies allowed by the Graphics Device
        /// </summary>
        public int MaxIndexCount { get; internal set; }
        /// <summary>
        /// The currently used OpenGL Major Version
        /// </summary>
        public int GlMajorVersion { get; internal set; }
        /// <summary>
        /// The currently used OpenGL Minor Version
        /// </summary>
        public int GlMinorVersion { get; internal set; }
        /// <summary>
        /// The currently used OpenGL Version as a String
        /// </summary>
        public string GlVersionString => $"{GlMajorVersion}.{GlMinorVersion}";
        /// <summary>
        /// OpenGL Api, used to shorten code
        /// </summary>
        private GL gl;

        internal GraphicsDevice(GL gl) {
            this.gl = gl;

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
        /// <summary>
        /// Calls glClear which Clears the Screen
        /// </summary>
        public void GlClear() {
            gl.Clear(ClearBufferMask.ColorBufferBit);
        }
        /// <summary>
        /// Calls glClearColor which changes the Clear Color
        /// </summary>
        /// <param name="color">New Clear Color</param>
        public void GlClearColor(Color color) {
            gl.ClearColor(color);
        }
    }
}
