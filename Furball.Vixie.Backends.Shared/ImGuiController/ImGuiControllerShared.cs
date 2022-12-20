using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Buffer = System.Buffer;

namespace Furball.Vixie.Backends.Shared.ImGuiController;

public sealed unsafe class ImGuiControllerShared : ImGuiController {
    private readonly GraphicsBackend _backend;
    private          VixieRenderer   _renderer = null!;
    private          Rectangle       _scissor;
    private          GCHandle        _fontTexture;

    public ImGuiControllerShared(GraphicsBackend backend, IView view, IInputContext input, ImGuiFontConfig?
                                     imGuiFontConfig = null,
                                 Action? onConfigureIo = null) : base(view, input, imGuiFontConfig, onConfigureIo) {
        this._backend = backend;
    }

    protected override bool VtxOffset => false;
    
    protected override void SetupRenderState(ImDrawDataPtr drawDataPtr, int framebufferWidth, int framebufferHeight) {
        this._renderer = this._backend.CreateRenderer();
    }

    protected override void PreDraw() {
        // this._scissor = this._backend.ScissorRect;
    }

    protected override void Draw(ImDrawDataPtr drawDataPtr) {
        int framebufferWidth  = (int)(drawDataPtr.DisplaySize.X * drawDataPtr.FramebufferScale.X);
        int framebufferHeight = (int)(drawDataPtr.DisplaySize.Y * drawDataPtr.FramebufferScale.Y);

        // Will project scissor/clipping rectangles into framebuffer space
        Vector2 clipOff   = drawDataPtr.DisplayPos;       // (0,0) unless using multi-viewports
        Vector2 clipScale = drawDataPtr.FramebufferScale; // (1,1) unless using retina display which are often (2,2)

        this._renderer.Begin(CullFace.None);

        // Render command lists
        for (int n = 0; n < drawDataPtr.CmdListsCount; n++) {
            ImDrawListPtr cmdListPtr = drawDataPtr.CmdListsRange[n];

            // Upload vertex/index buffers

            if (cmdListPtr.CmdBuffer.Size == 0)
                break;
            
            GCHandle handle = GCHandle.FromIntPtr(cmdListPtr.CmdBuffer[0].TextureId);
                    
            if(!handle.IsAllocated || handle.Target is not VixieTexture tex)
                throw new Exception("Invalid texture in ImGui!");

            MappedData map =
                this._renderer.Reserve((ushort)cmdListPtr.VtxBuffer.Size, (uint)cmdListPtr.IdxBuffer.Size, tex);
            new Span<ushort>((void*)cmdListPtr.IdxBuffer.Data, cmdListPtr.IdxBuffer.Size).CopyTo(new Span<ushort>(map.IndexPtr, cmdListPtr.IdxBuffer.Size));
            for (int i = 0; i < cmdListPtr.IdxBuffer.Size; i++) {
                map.IndexPtr[i] += (ushort)map.IndexOffset;
            }
            for (int i = 0; i < cmdListPtr.VtxBuffer.Size; i++) {
                ImDrawVertPtr vert = cmdListPtr.VtxBuffer[i];
                map.VertexPtr[i].Color             = new Color(vert.col);
                map.VertexPtr[i].Position          = vert.pos;
                map.VertexPtr[i].TextureCoordinate = vert.uv;
                map.VertexPtr[i].TexId             = map.TextureId;
            }
        }

        this._renderer.End();
        this._renderer.Draw();
    }

    protected override void PostDraw() {
        // this._backend.ScissorRect = this._scissor;
    }

    protected override void CreateDeviceResources() {
        this.RecreateFontDeviceTexture();
    }
    protected override void RecreateFontDeviceTexture() {
        // Build texture atlas
        // Load as RGBA 32-bit (75% of the memory is wasted, but default font is so small)
        // because it is more likely to be compatible with user's existing shaders.
        // If your ImTextureId represent a higher-level concept than just a GL texture id, consider calling GetTexDataAsAlpha8() instead to save on GPU memory.
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height,
                                    out int _); 
        
        // Upload texture to graphics system
        VixieTexture tex = this._backend.CreateEmptyTexture((uint)width, (uint)height,
                                                            new TextureParameters(false, TextureFilterType.Pixelated));

        tex.SetData(new ReadOnlySpan<Rgba32>((void*)pixels, width * height * sizeof(Rgba32)));

        GCHandle handle = this._fontTexture = GCHandle.Alloc(tex);
        
        io.Fonts.SetTexID(GCHandle.ToIntPtr(handle));
    }

    protected override void DisposeInternal() {
        this._renderer.Dispose();
        this._fontTexture.Free();
    }
}