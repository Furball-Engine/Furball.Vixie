#if USE_IMGUI
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Furball.Vixie.Backends.Shared.ImGuiController;
using ImGuiNET;
using JetBrains.Annotations;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Furball.Vixie.Backends.OpenGL;

public enum OpenGLType {
    Modern,
    Legacy,
    ES
}

public class OpenGlImGuiController : ImGuiController {
    private readonly GL         _gl;
    private readonly OpenGLType _type;

    private int  _attribLocationTex;
    private int  _attribLocationProjMtx;
    private int  _attribLocationVtxPos;
    private int  _attribLocationVtxUv;
    private int  _attribLocationVtxColor;
    private uint _vboHandle;
    private uint _elementsHandle;
    private uint _vertexArrayObject;

    private          bool  _lastEnablePrimitiveRestart;
    private          bool  _lastEnableBlend;
    private          bool  _lastEnableCullFace;
    private          bool  _lastEnableStencilTest;
    private          bool  _lastEnableDepthTest;
    private          bool  _lastEnableScissorTest;
    private          int   _lastActiveTexture;
    private          int   _lastProgram;
    private          int   _lastTexture;
    private          int   _lastSampler;
    private          int   _lastArrayBuffer;
    private          int   _lastVertexArrayObject;
    private readonly int[] _lastPolygonMode = new int[2];
    private readonly int[] _lastScissorBox  = new int[4];
    private          int   _lastBlendSrcRgb;
    private          int   _lastBlendDstRgb;
    private          int   _lastBlendSrcAlpha;
    private          int   _lastBlendDstAlpha;
    private          int   _lastBlendEquationRgb;
    private          int   _lastBlendEquationAlpha;
    private          uint  _shader;

    public OpenGlImGuiController(GL gl, OpenGLType type, [NotNull] IView view, [NotNull] IInputContext input,
                                 ImGuiFontConfig? imGuiFontConfig = null, [NotNull] Action onConfigureIo = null) : base(
        view, input, imGuiFontConfig, onConfigureIo) {
        this._gl   = gl;
        this._type = type;
    }

    protected override unsafe void SetupRenderState(ImDrawDataPtr drawDataPtr, int framebufferWidth,
                                                    int           framebufferHeight) {
        // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, polygon fill
        this._gl.Enable(GLEnum.Blend);
        this._gl.BlendEquation(GLEnum.FuncAdd);
        this._gl.BlendFuncSeparate(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha, GLEnum.One, GLEnum.OneMinusSrcAlpha);
        this._gl.Disable(GLEnum.CullFace);
        this._gl.Disable(GLEnum.DepthTest);
        this._gl.Disable(GLEnum.StencilTest);
        this._gl.Enable(GLEnum.ScissorTest);
        if (this._type == OpenGLType.Modern) {
            this._gl.Disable(GLEnum.PrimitiveRestart);
            this._gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
        }

        float l = drawDataPtr.DisplayPos.X;
        float r = drawDataPtr.DisplayPos.X + drawDataPtr.DisplaySize.X;
        float T = drawDataPtr.DisplayPos.Y;
        float b = drawDataPtr.DisplayPos.Y + drawDataPtr.DisplaySize.Y;

        Span<float> orthoProjection = stackalloc float[] {
            2.0f       / (r - l), 0.0f, 0.0f, 0.0f,
            0.0f, 2.0f / (T - b), 0.0f, 0.0f,
            0.0f, 0.0f, -1.0f, 0.0f,
            (r + l) / (l - r), (T + b) / (b - T), 0.0f, 1.0f
        };

        this._gl.UseProgram(this._shader);
        this._gl.Uniform1(this._attribLocationTex, 0);
        this._gl.UniformMatrix4(this._attribLocationProjMtx, 1, false, orthoProjection);

        this._gl.BindSampler(0, 0);

        // Setup desired GL state
        // Recreate the VAO every time (this is to easily allow multiple GL contexts to be rendered to. VAO are not shared among GL contexts)
        // The renderer would actually work without any VAO bound, but then our VertexAttrib calls would overwrite the default one currently bound.
        this._vertexArrayObject = this._gl.GenVertexArray();
        this._gl.BindVertexArray(this._vertexArrayObject);

        // Bind vertex/index buffers and setup attributes for ImDrawVert
        this._gl.BindBuffer(GLEnum.ArrayBuffer, this._vboHandle);
        this._gl.BindBuffer(GLEnum.ElementArrayBuffer, this._elementsHandle);
        this._gl.EnableVertexAttribArray((uint)this._attribLocationVtxPos);
        this._gl.EnableVertexAttribArray((uint)this._attribLocationVtxUv);
        this._gl.EnableVertexAttribArray((uint)this._attribLocationVtxColor);
        this._gl.VertexAttribPointer((uint)this._attribLocationVtxPos, 2, GLEnum.Float, false, (uint)sizeof(ImDrawVert),
                                     (void*)0);
        this._gl.VertexAttribPointer((uint)this._attribLocationVtxUv, 2, GLEnum.Float, false, (uint)sizeof(ImDrawVert),
                                     (void*)8);
        this._gl.VertexAttribPointer((uint)this._attribLocationVtxColor, 4, GLEnum.UnsignedByte, true,
                                     (uint)sizeof(ImDrawVert), (void*)16);
    }
    protected override void PreDraw() {
        // Backup GL state
        this._gl.GetInteger(GLEnum.ActiveTexture, out this._lastActiveTexture);
        this._gl.ActiveTexture(GLEnum.Texture0);

        this._gl.GetInteger(GLEnum.CurrentProgram, out this._lastProgram);
        this._gl.GetInteger(GLEnum.TextureBinding2D, out this._lastTexture);

        this._gl.GetInteger(GLEnum.SamplerBinding, out this._lastSampler);

        this._gl.GetInteger(GLEnum.ArrayBufferBinding, out this._lastArrayBuffer);
        this._gl.GetInteger(GLEnum.VertexArrayBinding, out this._lastVertexArrayObject);

        if (this._type != OpenGLType.ES)
            this._gl.GetInteger(GLEnum.PolygonMode, this._lastPolygonMode);

        this._gl.GetInteger(GLEnum.ScissorBox, this._lastScissorBox);

        this._gl.GetInteger(GLEnum.BlendSrcRgb, out this._lastBlendSrcRgb);
        this._gl.GetInteger(GLEnum.BlendDstRgb, out this._lastBlendDstRgb);

        this._gl.GetInteger(GLEnum.BlendSrcAlpha, out this._lastBlendSrcAlpha);
        this._gl.GetInteger(GLEnum.BlendDstAlpha, out this._lastBlendDstAlpha);

        this._gl.GetInteger(GLEnum.BlendEquationRgb, out this._lastBlendEquationRgb);
        this._gl.GetInteger(GLEnum.BlendEquationAlpha, out this._lastBlendEquationAlpha);

        this._lastEnableBlend       = this._gl.IsEnabled(GLEnum.Blend);
        this._lastEnableCullFace    = this._gl.IsEnabled(GLEnum.CullFace);
        this._lastEnableDepthTest   = this._gl.IsEnabled(GLEnum.DepthTest);
        this._lastEnableStencilTest = this._gl.IsEnabled(GLEnum.StencilTest);
        this._lastEnableScissorTest = this._gl.IsEnabled(GLEnum.ScissorTest);

        //PrimitiveRestart only exists on modern GL contexts
        if (this._type == OpenGLType.Modern)
            this._lastEnablePrimitiveRestart = this._gl.IsEnabled(GLEnum.PrimitiveRestart);
    }
    protected override unsafe void Draw(ImDrawDataPtr drawDataPtr) {
        int framebufferWidth  = (int)(drawDataPtr.DisplaySize.X * drawDataPtr.FramebufferScale.X);
        int framebufferHeight = (int)(drawDataPtr.DisplaySize.Y * drawDataPtr.FramebufferScale.Y);

        // Will project scissor/clipping rectangles into framebuffer space
        Vector2 clipOff   = drawDataPtr.DisplayPos;       // (0,0) unless using multi-viewports
        Vector2 clipScale = drawDataPtr.FramebufferScale; // (1,1) unless using retina display which are often (2,2)

        // Render command lists
        for (int n = 0; n < drawDataPtr.CmdListsCount; n++) {
            ImDrawListPtr cmdListPtr = drawDataPtr.CmdListsRange[n];

            // Upload vertex/index buffers

            this._gl.BufferData(GLEnum.ArrayBuffer, (nuint)(cmdListPtr.VtxBuffer.Size * sizeof(ImDrawVert)),
                                (void*)cmdListPtr.VtxBuffer.Data, GLEnum.StreamDraw);
            this._gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)(cmdListPtr.IdxBuffer.Size * sizeof(ushort)),
                                (void*)cmdListPtr.IdxBuffer.Data, GLEnum.StreamDraw);

            for (int cmdI = 0; cmdI < cmdListPtr.CmdBuffer.Size; cmdI++) {
                ImDrawCmdPtr cmdPtr = cmdListPtr.CmdBuffer[cmdI];

                if (cmdPtr.UserCallback != IntPtr.Zero)
                    throw new NotImplementedException();
                Vector4 clipRect;
                clipRect.X = (cmdPtr.ClipRect.X - clipOff.X) * clipScale.X;
                clipRect.Y = (cmdPtr.ClipRect.Y - clipOff.Y) * clipScale.Y;
                clipRect.Z = (cmdPtr.ClipRect.Z - clipOff.X) * clipScale.X;
                clipRect.W = (cmdPtr.ClipRect.W - clipOff.Y) * clipScale.Y;

                if (clipRect.X < framebufferWidth && clipRect.Y < framebufferHeight && clipRect.Z >= 0.0f &&
                    clipRect.W >= 0.0f) {
                    // Apply scissor/clipping rectangle
                    this._gl.Scissor((int)clipRect.X, (int)(framebufferHeight - clipRect.W),
                                     (uint)(clipRect.Z - clipRect.X), (uint)(clipRect.W - clipRect.Y));

                    // Bind texture, Draw
                    this._gl.BindTexture(GLEnum.Texture2D, (uint)cmdPtr.TextureId);

                    this._gl.DrawElementsBaseVertex(GLEnum.Triangles, cmdPtr.ElemCount, GLEnum.UnsignedShort,
                                                    (void*)(cmdPtr.IdxOffset * sizeof(ushort)), (int)cmdPtr.VtxOffset);
                }
            }
        }

        // Destroy the temporary VAO
        this._gl.DeleteVertexArray(this._vertexArrayObject);
        this._vertexArrayObject = 0;
    }
    protected override void PostDraw() {
        // Restore modified GL state
        this._gl.UseProgram((uint)this._lastProgram);
        this._gl.BindTexture(GLEnum.Texture2D, (uint)this._lastTexture);

        this._gl.BindSampler(0, (uint)this._lastSampler);

        this._gl.ActiveTexture((GLEnum)this._lastActiveTexture);

        this._gl.BindVertexArray((uint)this._lastVertexArrayObject);

        this._gl.BindBuffer(GLEnum.ArrayBuffer, (uint)this._lastArrayBuffer);
        this._gl.BlendEquationSeparate((GLEnum)this._lastBlendEquationRgb, (GLEnum)this._lastBlendEquationAlpha);
        this._gl.BlendFuncSeparate((GLEnum)this._lastBlendSrcRgb, (GLEnum)this._lastBlendDstRgb,
                                   (GLEnum)this._lastBlendSrcAlpha,
                                   (GLEnum)this._lastBlendDstAlpha);

        if (this._lastEnableBlend)
            this._gl.Enable(GLEnum.Blend);
        else
            this._gl.Disable(GLEnum.Blend);

        if (this._lastEnableCullFace)
            this._gl.Enable(GLEnum.CullFace);
        else
            this._gl.Disable(GLEnum.CullFace);

        if (this._lastEnableDepthTest)
            this._gl.Enable(GLEnum.DepthTest);
        else
            this._gl.Disable(GLEnum.DepthTest);
        if (this._lastEnableStencilTest)
            this._gl.Enable(GLEnum.StencilTest);
        else
            this._gl.Disable(GLEnum.StencilTest);

        if (this._lastEnableScissorTest)
            this._gl.Enable(GLEnum.ScissorTest);
        else
            this._gl.Disable(GLEnum.ScissorTest);

        if (this._type == OpenGLType.Modern) {
            if (this._lastEnablePrimitiveRestart)
                this._gl.Enable(GLEnum.PrimitiveRestart);
            else
                this._gl.Disable(GLEnum.PrimitiveRestart);

            this._gl.PolygonMode(GLEnum.FrontAndBack, (GLEnum)this._lastPolygonMode[0]);
        }

        this._gl.Scissor(this._lastScissorBox[0], this._lastScissorBox[1], (uint)this._lastScissorBox[2],
                         (uint)this._lastScissorBox[3]);
    }

    private readonly Dictionary<string, int> _uniformToLocation = new();
    private readonly Dictionary<string, int> _attribLocation    = new();
    private          uint                    _fontTexture;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetUniformLocation(string uniform) {
        if (this._uniformToLocation.TryGetValue(uniform, out int location) == false) {
            location = this._gl.GetUniformLocation(this._shader, uniform);
            this._uniformToLocation.Add(uniform, location);

            if (location == -1)
                Debug.Print($"The uniform '{uniform}' does not exist in the shader!");
        }

        return location;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetAttribLocation(string attrib) {
        if (this._attribLocation.TryGetValue(attrib, out int location) == false) {
            location = this._gl.GetAttribLocation(this._shader, attrib);
            this._attribLocation.Add(attrib, location);

            if (location == -1)
                Debug.Print($"The attrib '{attrib}' does not exist in the shader!");
        }

        return location;
    }

    protected override void CreateDeviceResources() {
        // Backup GL state

        this._gl.GetInteger(GLEnum.TextureBinding2D, out int lastTexture);
        this._gl.GetInteger(GLEnum.ArrayBufferBinding, out int lastArrayBuffer);
        this._gl.GetInteger(GLEnum.VertexArrayBinding, out int lastVertexArray);

        string vertexSource;
        string fragmentSource;

        switch (this._type) {
            case OpenGLType.Modern:
                vertexSource = @"#version 330
        layout (location = 0) in vec2 Position;
        layout (location = 1) in vec2 UV;
        layout (location = 2) in vec4 Color;
        uniform mat4 ProjMtx;
        out vec2 Frag_UV;
        out vec4 Frag_Color;
        void main()
        {
            Frag_UV = UV;
            Frag_Color = Color;
            gl_Position = ProjMtx * vec4(Position.xy,0,1);
        }";
                fragmentSource = @"#version 330
        in vec2 Frag_UV;
        in vec4 Frag_Color;
        uniform sampler2D Texture;
        layout (location = 0) out vec4 Out_Color;
        void main()
        {
            Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
        }";
                break;
            case OpenGLType.Legacy:
                vertexSource = @"#version 110
attribute vec2 Position;
attribute vec2 UV;
attribute vec4 Color;
uniform mat4 ProjMtx;
varying vec2 Frag_UV;
varying vec4 Frag_Color;
void main()
{
    Frag_UV = UV;
    Frag_Color = Color;
    gl_Position = ProjMtx * vec4(Position.xy,0,1);
}";
                fragmentSource = @"#version 110
varying vec2 Frag_UV;
varying vec4 Frag_Color;
uniform sampler2D Texture;
void main()
{
    gl_FragColor = Frag_Color * texture2D(Texture, Frag_UV.st);
}";
                break;
            case OpenGLType.ES:
                vertexSource = @"#version 300 es
precision highp float;
    
layout (location = 0) in vec2 Position;
layout (location = 1) in vec2 UV;
layout (location = 2) in vec4 Color;
uniform mat4 ProjMtx;
out vec2 Frag_UV;
out vec4 Frag_Color;
void main()
{
    Frag_UV = UV;
    Frag_Color = Color;
    gl_Position = ProjMtx * vec4(Position.xy,0.0,1.0);
}";
                fragmentSource = @"#version 300 es
precision highp float;

in vec2 Frag_UV;
in vec4 Frag_Color;
uniform sampler2D Texture;
layout (location = 0) out vec4 Out_Color;
void main()
{
    Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
}";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        uint vtx = this._gl.CreateShader(ShaderType.VertexShader);
        uint frg = this._gl.CreateShader(ShaderType.FragmentShader);

        this._gl.ShaderSource(vtx, vertexSource);
        this._gl.ShaderSource(frg, fragmentSource);

        this._gl.CompileShader(vtx);
        this._gl.CompileShader(frg);

        this._gl.GetShader(vtx, ShaderParameterName.CompileStatus, out int success);
        if (success == 0) {
            string info = this._gl.GetShaderInfoLog(vtx);
            Debug.WriteLine($"GL.CompileShader for shader [{ShaderType.VertexShader}] had info log:\n{info}");
        }

        this._gl.GetShader(frg, ShaderParameterName.CompileStatus, out success);
        if (success == 0) {
            string info = this._gl.GetShaderInfoLog(frg);
            Debug.WriteLine($"GL.CompileShader for shader [{ShaderType.FragmentShader}] had info log:\n{info}");
        }

        this._shader = this._gl.CreateProgram();

        this._gl.AttachShader(this._shader, vtx);
        this._gl.AttachShader(this._shader, frg);

        this._gl.LinkProgram(this._shader);

        this._gl.GetProgram(this._shader, GLEnum.LinkStatus, out success);
        if (success == 0) {
            string info = this._gl.GetProgramInfoLog(this._shader);
            Debug.WriteLine($"GL.LinkProgram had info log:\n{info}");
        }

        this._gl.DetachShader(this._shader, vtx);
        this._gl.DeleteShader(vtx);

        this._gl.DetachShader(this._shader, frg);
        this._gl.DeleteShader(frg);

        this._attribLocationTex      = this.GetUniformLocation("Texture");
        this._attribLocationProjMtx  = this.GetUniformLocation("ProjMtx");
        this._attribLocationVtxPos   = this.GetAttribLocation("Position");
        this._attribLocationVtxUv    = this.GetAttribLocation("UV");
        this._attribLocationVtxColor = this.GetAttribLocation("Color");

        this._vboHandle      = this._gl.GenBuffer();
        this._elementsHandle = this._gl.GenBuffer();

        this.RecreateFontDeviceTexture();

        // Restore modified GL state
        this._gl.BindTexture(GLEnum.Texture2D, (uint)lastTexture);
        this._gl.BindBuffer(GLEnum.ArrayBuffer, (uint)lastArrayBuffer);

        this._gl.BindVertexArray((uint)lastVertexArray);
    }
    protected override unsafe void RecreateFontDeviceTexture() {
        // Build texture atlas
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height,
                                    out int bytesPerPixel); // Load as RGBA 32-bit (75% of the memory is wasted, but default font is so small) because it is more likely to be compatible with user's existing shaders. If your ImTextureId represent a higher-level concept than just a GL texture id, consider calling GetTexDataAsAlpha8() instead to save on GPU memory.

        // Upload texture to graphics system
        this._gl.GetInteger(GLEnum.TextureBinding2D, out int lastTexture);

        this._fontTexture = this._gl.GenTexture();

        this._gl.BindTexture(TextureTarget.Texture2D, this._fontTexture);
        this._gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)width, (uint)height, 0,
                            PixelFormat.Rgba, PixelType.UnsignedByte, (void*)pixels);

        this._gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                              (int)TextureMagFilter.Linear);
        this._gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                              (int)TextureMinFilter.Linear);

        // Store our identifier
        io.Fonts.SetTexID((IntPtr)this._fontTexture);

        // Restore state
        this._gl.BindTexture(GLEnum.Texture2D, (uint)lastTexture);
    }
    protected override void DisposeInternal() {
        this._gl.DeleteBuffer(this._vboHandle);
        this._gl.DeleteBuffer(this._elementsHandle);
        this._gl.DeleteVertexArray(this._vertexArrayObject);

        this._gl.DeleteShader(this._shader);
        this._gl.DeleteTexture(this._fontTexture);
    }
}
#endif