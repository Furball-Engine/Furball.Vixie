#if USE_IMGUI
using System;
using System.Collections.Generic;
using System.Numerics;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Vortice;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using ComObject=SharpGen.Runtime.ComObject;

namespace Furball.Vixie.Backends.Direct3D11;

public struct ImGuiFontConfig {
    public string FontPath;
    public int    FontSize;

    public ImGuiFontConfig(string fontPath, int fontSize) {
        this.FontPath = fontPath;
        this.FontSize = fontSize;
    }
}

public class ImGuiControllerD3D11 : IDisposable {
    private int _windowWidth;
    private int _windowHeight;

    private readonly Direct3D11Backend _backend;
    private          IView             _view;
    private          IInputContext     _inputContext;
    private          IKeyboard         _keyboard;
    private          List<char>        _pressedCharacters;

    private Blob                     _vertexShaderBlob;
    private Blob                     _pixelShaderBlob;
    private ID3D11VertexShader       _vertexShader;
    private ID3D11PixelShader        _pixelShader;
    private ID3D11InputLayout        _inputLayout;
    private ID3D11Buffer             _vertexBuffer;
    private ID3D11Buffer             _indexBuffer;
    private ID3D11Buffer             _constantBuffer;
    private ID3D11BlendState         _blendState;
    private ID3D11RasterizerState    _rasterizerState;
    private ID3D11DepthStencilState  _depthStencilState;
    private ID3D11ShaderResourceView _fontTextureView;
    private ID3D11SamplerState       _fontSampler;

    private int _vertexBufferSize;
    private int _indexBufferSize;

    private Dictionary<IntPtr, ID3D11ShaderResourceView> _textureResources = new();

    private bool _frameBegun;

    public ImGuiControllerD3D11(
        Direct3D11Backend backend, IView view, IInputContext context, ImGuiFontConfig? fontConfig,
        Action onConfigureIo = null
    ) {
        this._pressedCharacters = new List<char>();

        this._backend      = backend;
        this._view         = view;
        this._inputContext = context;
        this._windowWidth  = this._view.FramebufferSize.X;
        this._windowHeight = this._view.FramebufferSize.Y;

        IntPtr imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(imGuiContext);
        ImGui.StyleColorsDark();

        ImGuiIOPtr io = ImGui.GetIO();

        if (fontConfig != null)
            io.Fonts.AddFontFromFileTTF(fontConfig.Value.FontPath, fontConfig.Value.FontSize);

        onConfigureIo?.Invoke();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        this.SetKeyMappings();
        this.CreateObjects();

        ImGui.NewFrame();
        this._frameBegun = true;

        this._keyboard         =  this._inputContext.Keyboards[0];
        this._keyboard.KeyChar += this.OnKeyChar;

        this._view.Resize += this.OnViewResized;
    }

    ~ImGuiControllerD3D11() {
        DisposeQueue.Enqueue(this);
    }

    private unsafe void RenderImDrawData() {
        ImDrawDataPtr drawData = ImGui.GetDrawData();

        if (drawData.DisplaySize.X <= 0.0f || drawData.DisplaySize.Y <= 0.0f)
            return;

        if (this._vertexBuffer == null || this._vertexBufferSize < drawData.TotalVtxCount) {
            this._vertexBuffer?.Dispose();

            this._vertexBufferSize += 5000;

            BufferDescription vertexBufferDescription = new BufferDescription {
                Usage          = ResourceUsage.Dynamic,
                ByteWidth      = this._vertexBufferSize * sizeof(ImDrawVert),
                BindFlags      = BindFlags.VertexBuffer,
                CPUAccessFlags = CpuAccessFlags.Write
            };

            this._vertexBuffer = this._backend.Device.CreateBuffer(vertexBufferDescription);
            //this._vertexBuffer.DebugName = "ImGui Vertex Buffer";
        }

        if (this._indexBuffer == null || this._indexBufferSize < drawData.TotalIdxCount) {
            this._indexBuffer?.Dispose();

            this._indexBufferSize += 5000;

            BufferDescription indexBufferDescription = new BufferDescription {
                Usage          = ResourceUsage.Dynamic,
                ByteWidth      = this._indexBufferSize * sizeof(ushort),
                BindFlags      = BindFlags.IndexBuffer,
                CPUAccessFlags = CpuAccessFlags.Write
            };

            this._indexBuffer = this._backend.Device.CreateBuffer(indexBufferDescription);
            //this._indexBuffer.DebugName = "ImGui Index Buffer";
        }

        MappedSubresource vertexResource = this._backend.DeviceContext.Map(this._vertexBuffer, 0, MapMode.WriteDiscard);
        MappedSubresource indexResource  = this._backend.DeviceContext.Map(this._indexBuffer,  0, MapMode.WriteDiscard);

        ImDrawVert* vertexResourcePointer = (ImDrawVert*)vertexResource.DataPointer;
        ushort*     indexResourcePointer  = (ushort*)indexResource.DataPointer;

        for (int i = 0; i != drawData.CmdListsCount; i++) {
            ImDrawListPtr cmdList = drawData.CmdListsRange[i];

            int vertexBytes = cmdList.VtxBuffer.Size * sizeof(ImDrawVert);
            Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vertexResourcePointer, vertexBytes, vertexBytes);

            int indexBytes = cmdList.IdxBuffer.Size * sizeof(ushort);
            Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, indexResourcePointer, indexBytes, indexBytes);

            vertexResourcePointer += cmdList.VtxBuffer.Size;
            indexResourcePointer  += cmdList.IdxBuffer.Size;
        }

        this._backend.DeviceContext.Unmap(this._vertexBuffer, 0);
        this._backend.DeviceContext.Unmap(this._indexBuffer,  0);

        MappedSubresource constantBufferResource =
            this._backend.DeviceContext.Map(this._constantBuffer, 0, MapMode.WriteDiscard);
        Span<float> constantBufferResourceSpan = constantBufferResource.AsSpan<float>(16 * sizeof(float));

        float l = drawData.DisplayPos.X;
        float r = drawData.DisplayPos.X + drawData.DisplaySize.X;
        float t = drawData.DisplayPos.Y;
        float b = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

        float[] projectionMatrix = new[] {
            2.0f / (r - l), 0.0f, 0.0f, 0.0f, 0.0f, 2.0f / (t - b), 0.0f, 0.0f, 0.0f, 0.0f, 0.5f, 0.0f,
            (r + l) / (l - r), (t + b) / (b - t), 0.5f, 1.0f,
        };

        projectionMatrix.CopyTo(constantBufferResourceSpan);

        this._backend.DeviceContext.Unmap(this._constantBuffer, 0);

        #region Save Render State

        int oldNumScissorRects = 0;
        int oldNumViewports    = 0;
        int oldPixelShaderInstancesCount, oldVertexShaderInstancesCount, oldGeometryShaderInstancesCount;

        RawRect[]                  oldScissorRectangles           = new RawRect[16];
        Viewport                   oldViewport                    = new Viewport();
        ID3D11ShaderResourceView[] oldShaderResourceViews         = new ID3D11ShaderResourceView[1];
        ID3D11SamplerState[]       oldSamplerStates               = new ID3D11SamplerState[1];
        ID3D11ClassInstance[]      oldPixelShaderInstances        = new ID3D11ClassInstance[256];
        ID3D11ClassInstance[]      oldVertexShaderInstances       = new ID3D11ClassInstance[256];
        ID3D11ClassInstance[]      oldGeometryShaderInstances     = new ID3D11ClassInstance[256];
        ID3D11Buffer[]             oldVertexShaderConstantBuffers = new ID3D11Buffer[1];
        ID3D11Buffer[]             oldVertexBuffers               = new ID3D11Buffer[1];
        int[]                      oldVertexBufferStrides         = new int[1];
        int[]                      oldVertexBufferOffsets         = new int[1];

        this._backend.DeviceContext.RSGetScissorRects(ref oldNumScissorRects, oldScissorRectangles);
        this._backend.DeviceContext.RSGetViewport(ref oldViewport);
        ID3D11RasterizerState oldRasterizerState = this._backend.DeviceContext.RSGetState();

        ID3D11BlendState oldBlendState = this._backend.DeviceContext.OMGetBlendState(
        out Color4 oldBlendFactor,
        out int oldSampleMask
        );

        this._backend.DeviceContext.OMGetDepthStencilState(
        out ID3D11DepthStencilState oldDepthStencilState,
        out int oldStencilRef
        );

        this._backend.DeviceContext.PSGetShaderResources(0, 1, oldShaderResourceViews);
        this._backend.DeviceContext.PSGetSamplers(0, 1, oldSamplerStates);

        oldPixelShaderInstancesCount = oldVertexShaderInstancesCount = oldGeometryShaderInstancesCount = 256;

        this._backend.DeviceContext.PSGetShader(
        out ID3D11PixelShader oldPixelShader,
        oldPixelShaderInstances,
        ref oldPixelShaderInstancesCount
        );
        this._backend.DeviceContext.VSGetShader(
        out ID3D11VertexShader oldVertexShader,
        oldVertexShaderInstances,
        ref oldVertexShaderInstancesCount
        );
        this._backend.DeviceContext.GSGetShader(
        out ID3D11GeometryShader oldGeometryShader,
        oldGeometryShaderInstances,
        ref oldGeometryShaderInstancesCount
        );

        this._backend.DeviceContext.VSGetConstantBuffers(0, 1, oldVertexShaderConstantBuffers);

        PrimitiveTopology oldPrimitiveTopology = this._backend.DeviceContext.IAGetPrimitiveTopology();
        this._backend.DeviceContext.IAGetIndexBuffer(
        out ID3D11Buffer oldIndexBuffer,
        out Format oldIndexBufferFormat,
        out int oldIndexBufferOffset
        );
        this._backend.DeviceContext.IAGetVertexBuffers(
        0,
        1,
        oldVertexBuffers,
        oldVertexBufferStrides,
        oldVertexBufferOffsets
        );
        ID3D11InputLayout oldInputLayout = this._backend.DeviceContext.IAGetInputLayout();

        #endregion

        SetupRenderState(drawData);

        int globalIndexOffset  = 0;
        int globalVertexOffset = 0;

        Vector2 clipOff = drawData.DisplayPos;

        for (int i = 0; i != drawData.CmdListsCount; i++) {
            var cmdList = drawData.CmdListsRange[i];

            for (int n = 0; n < cmdList.CmdBuffer.Size; n++) {
                var cmd = cmdList.CmdBuffer[n];

                if (cmd.UserCallback != IntPtr.Zero)
                    throw new NotImplementedException("No!");
                else {
                    RawRect rectangle = new RawRect(
                    (int)(cmd.ClipRect.X - clipOff.X),
                    (int)(cmd.ClipRect.Y - clipOff.Y),
                    (int)(cmd.ClipRect.Z - clipOff.X),
                    (int)(cmd.ClipRect.W - clipOff.Y)
                    );
                    this._backend.DeviceContext.RSSetScissorRect(rectangle);

                    this._textureResources.TryGetValue(cmd.TextureId, out ID3D11ShaderResourceView texture);

                    if (texture != null) {
                        this._backend.DeviceContext.PSSetShaderResource(0, texture);
                        this._backend.DeviceContext.DrawIndexed(
                        (int)cmd.ElemCount,
                        (int)(cmd.IdxOffset + globalIndexOffset),
                        (int)(cmd.VtxOffset + globalVertexOffset)
                        );
                    }
                }
            }

            globalIndexOffset  += cmdList.IdxBuffer.Size;
            globalVertexOffset += cmdList.VtxBuffer.Size;
        }

        #region Restore Render State

        this._backend.DeviceContext.RSSetScissorRects(oldNumScissorRects, oldScissorRectangles);
        this._backend.DeviceContext.RSSetViewport(oldViewport);
        this._backend.DeviceContext.RSSetState(oldRasterizerState);

        if (oldRasterizerState?.NativePointer != IntPtr.Zero)
            oldRasterizerState?.Dispose();

        this._backend.DeviceContext.OMSetBlendState(oldBlendState, oldBlendFactor, oldSampleMask);

        if (oldBlendState?.NativePointer != IntPtr.Zero)
            oldBlendState?.Dispose();

        this._backend.DeviceContext.OMSetDepthStencilState(oldDepthStencilState, oldStencilRef);

        if (oldDepthStencilState?.NativePointer != IntPtr.Zero)
            oldDepthStencilState?.Dispose();

        this._backend.DeviceContext.PSSetShaderResources(0, 1, oldShaderResourceViews);

        if (oldShaderResourceViews[0]?.NativePointer != IntPtr.Zero)
            oldShaderResourceViews[0]?.Dispose();

        this._backend.DeviceContext.PSSetSamplers(0, 1, oldSamplerStates);

        if (oldSamplerStates[0]?.NativePointer != IntPtr.Zero)
            oldSamplerStates[0]?.Dispose();

        this._backend.DeviceContext.PSSetShader(oldPixelShader, oldPixelShaderInstances, oldPixelShaderInstancesCount);

        if (oldPixelShader?.NativePointer != IntPtr.Zero)
            oldPixelShader?.Dispose();

        for (int i = 0; i < oldPixelShaderInstancesCount; i++)
            oldPixelShaderInstances[i]?.Dispose();

        this._backend.DeviceContext.VSSetShader(
        oldVertexShader,
        oldVertexShaderInstances,
        oldVertexShaderInstancesCount
        );

        if (oldVertexShader?.NativePointer != IntPtr.Zero)
            oldVertexShader?.Dispose();

        for (int i = 0; i < oldVertexShaderInstancesCount; i++)
            oldVertexShaderInstances[i]?.Dispose();

        this._backend.DeviceContext.GSSetShader(
        oldGeometryShader,
        oldGeometryShaderInstances,
        oldGeometryShaderInstancesCount
        );

        if (oldGeometryShader?.NativePointer != IntPtr.Zero)
            oldGeometryShader?.Dispose();

        for (int i = 0; i < oldGeometryShaderInstancesCount; i++)
            oldGeometryShaderInstances[i]?.Dispose();

        this._backend.DeviceContext.VSSetConstantBuffers(0, 1, oldVertexShaderConstantBuffers);

        if (oldVertexShaderConstantBuffers[0]?.NativePointer != IntPtr.Zero)
            oldVertexShaderConstantBuffers[0]?.Dispose();

        this._backend.DeviceContext.IASetPrimitiveTopology(oldPrimitiveTopology);
        this._backend.DeviceContext.IASetIndexBuffer(oldIndexBuffer, oldIndexBufferFormat, oldIndexBufferOffset);

        if (oldIndexBuffer?.NativePointer != IntPtr.Zero)
            oldIndexBuffer?.Dispose();

        this._backend.DeviceContext.IASetVertexBuffers(
        0,
        1,
        oldVertexBuffers,
        oldVertexBufferStrides,
        oldVertexBufferOffsets
        );

        if (oldVertexBuffers[0]?.NativePointer != IntPtr.Zero)
            oldVertexBuffers[0]?.Dispose();

        this._backend.DeviceContext.IASetInputLayout(oldInputLayout);

        if (oldInputLayout?.NativePointer != IntPtr.Zero)
            oldInputLayout?.Dispose();

        #endregion

    }

    private unsafe void SetupRenderState(ImDrawDataPtr drawData) {
        Viewport viewport = new Viewport(0, 0, drawData.DisplaySize.X, drawData.DisplaySize.Y, 0, 1);

        this._backend.DeviceContext.RSSetViewport(viewport);

        int stride = sizeof(ImDrawVert);
        int offset = 0;

        this._backend.DeviceContext.IASetInputLayout(this._inputLayout);
        this._backend.DeviceContext.IASetVertexBuffers(
        0,
        1,
        new[] {
            _vertexBuffer
        },
        new[] {
            stride
        },
        new[] {
            offset
        }
        );
        this._backend.DeviceContext.IASetIndexBuffer(this._indexBuffer, Format.R16_UInt, 0);
        this._backend.DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        this._backend.DeviceContext.VSSetShader(this._vertexShader);
        this._backend.DeviceContext.VSSetConstantBuffer(0, this._constantBuffer);
        this._backend.DeviceContext.PSSetShader(this._pixelShader);
        this._backend.DeviceContext.PSSetSampler(0, this._fontSampler);
        this._backend.DeviceContext.GSSetShader(null);
        this._backend.DeviceContext.HSSetShader(null);
        this._backend.DeviceContext.DSSetShader(null);
        this._backend.DeviceContext.CSSetShader(null);
        this._backend.DeviceContext.OMSetBlendState(this._blendState);
        this._backend.DeviceContext.OMSetDepthStencilState(this._depthStencilState);
        this._backend.DeviceContext.RSSetState(this._rasterizerState);
    }

    public void Update(float delta) {
        if (this._frameBegun)
            ImGui.Render();

        ImGuiIOPtr io = ImGui.GetIO();

        io.DisplaySize = new Vector2(this._windowWidth, this._windowHeight);

        if (this._windowHeight > 0 && this._windowHeight > 0)
            io.DisplayFramebufferScale = new Vector2(
            this._view.FramebufferSize.X / this._windowWidth,
            this._view.FramebufferSize.Y / this._windowHeight
            );

        io.DeltaTime = delta;

        this.UpdateImGuiInput();

        this._frameBegun = true;
        ImGui.NewFrame();
    }

    public void Render() {
        if (this._frameBegun) {
            this._frameBegun = false;

            ImGui.Render();
            this.RenderImDrawData();
        }
    }

    #region Windowing and Input thingies

    private void OnKeyChar(IKeyboard keyboard, char character) {
        this._pressedCharacters.Add(character);
    }

    private void OnViewResized(Vector2D<int> newSize) {
        this._windowWidth  = this._view.Size.X;
        this._windowHeight = this._view.Size.Y;
    }

    private void SetPerFrameImGuiData(float delta) {
        ImGuiIOPtr io = ImGui.GetIO();

        io.DisplaySize = new Vector2(this._windowWidth, this._windowHeight);

        if (this._windowHeight > 0 && this._windowHeight > 0)
            io.DisplayFramebufferScale = new Vector2(
            this._view.FramebufferSize.X / this._windowWidth,
            this._view.FramebufferSize.Y / this._windowHeight
            );

        io.DeltaTime = delta;
    }

    private void UpdateImGuiInput() {
        ImGuiIOPtr io = ImGui.GetIO();

        MouseState mouse    = this._inputContext.Mice[0].CaptureState();
        IKeyboard  keyboard = this._inputContext.Keyboards[0];

        io.MouseDown[0] = mouse.IsButtonPressed(MouseButton.Left);
        io.MouseDown[1] = mouse.IsButtonPressed(MouseButton.Middle);
        io.MouseDown[2] = mouse.IsButtonPressed(MouseButton.Right);

        io.MousePos = mouse.Position;

        ScrollWheel wheel = mouse.GetScrollWheels()[0];

        io.MouseWheel  = wheel.Y;
        io.MouseWheelH = wheel.X;

        foreach (Key key in Enum.GetValues(typeof(Key))) {
            if (key == Key.Unknown)
                continue;

            io.KeysDown[(int)key] = keyboard.IsKeyPressed(key);
        }

        for (int i = 0; i != this._pressedCharacters.Count; i++)
            io.AddInputCharacter(this._pressedCharacters[i]);

        this._pressedCharacters.Clear();

        io.KeyCtrl  = keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight);
        io.KeyAlt   = keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight);
        io.KeyShift = keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight);
        io.KeySuper = keyboard.IsKeyPressed(Key.SuperLeft) || keyboard.IsKeyPressed(Key.SuperRight);
    }

    private void SetKeyMappings() {
        ImGuiIOPtr io = ImGui.GetIO();

        io.KeyMap[(int)ImGuiKey.Tab]        = (int)Key.Tab;
        io.KeyMap[(int)ImGuiKey.LeftArrow]  = (int)Key.Left;
        io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
        io.KeyMap[(int)ImGuiKey.UpArrow]    = (int)Key.Up;
        io.KeyMap[(int)ImGuiKey.DownArrow]  = (int)Key.Down;
        io.KeyMap[(int)ImGuiKey.PageUp]     = (int)Key.PageUp;
        io.KeyMap[(int)ImGuiKey.PageDown]   = (int)Key.PageDown;
        io.KeyMap[(int)ImGuiKey.Home]       = (int)Key.Home;
        io.KeyMap[(int)ImGuiKey.End]        = (int)Key.End;
        io.KeyMap[(int)ImGuiKey.Delete]     = (int)Key.Delete;
        io.KeyMap[(int)ImGuiKey.Backspace]  = (int)Key.Backspace;
        io.KeyMap[(int)ImGuiKey.Enter]      = (int)Key.Enter;
        io.KeyMap[(int)ImGuiKey.Escape]     = (int)Key.Escape;
        io.KeyMap[(int)ImGuiKey.A]          = (int)Key.A;
        io.KeyMap[(int)ImGuiKey.C]          = (int)Key.C;
        io.KeyMap[(int)ImGuiKey.V]          = (int)Key.V;
        io.KeyMap[(int)ImGuiKey.X]          = (int)Key.X;
        io.KeyMap[(int)ImGuiKey.Y]          = (int)Key.Y;
        io.KeyMap[(int)ImGuiKey.Z]          = (int)Key.Z;
    }

    #endregion

    private void CreateObjects() {
        byte[] vertexShaderData = ResourceHelpers.GetByteResource(
        "Shaders/Compiled/ImGuiController/VertexShader.dxc",
        typeof(Direct3D11Backend)
        );
        byte[] pixelShaderData = ResourceHelpers.GetByteResource(
        "Shaders/Compiled/ImGuiController/PixelShader.dxc",
        typeof(Direct3D11Backend)
        );

        ID3D11VertexShader vertexShader = this._backend.Device.CreateVertexShader(vertexShaderData);
        ID3D11PixelShader  pixelShader  = this._backend.Device.CreatePixelShader(pixelShaderData);

        this._vertexShader = vertexShader;
        //this._vertexShader.DebugName = "ImGui Vertex Shader";

        this._pixelShader = pixelShader;
        //this._pixelShader.DebugName = "ImGui Pixel Shader";

        InputElementDescription[] inputElementDescription = new InputElementDescription[] {
            new InputElementDescription("POSITION", 0, Format.R32G32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, 8, 0, InputClassification.PerVertexData, 0),
            new InputElementDescription("COLOR", 0, Format.R8G8B8A8_UNorm, 16, 0, InputClassification.PerVertexData, 0),
        };

        this._inputLayout = this._backend.Device.CreateInputLayout(inputElementDescription, vertexShaderData);
        //this._inputLayout.DebugName = "ImGui InputLayout";

        BufferDescription constantBufferDescription = new BufferDescription {
            ByteWidth      = 16 * sizeof(float),
            Usage          = ResourceUsage.Dynamic,
            BindFlags      = BindFlags.ConstantBuffer,
            CPUAccessFlags = CpuAccessFlags.Write
        };

        this._constantBuffer = this._backend.Device.CreateBuffer(constantBufferDescription);
        //this._constantBuffer.DebugName = "ImGui ConstantBuffer";

        BlendDescription blendDescription = new BlendDescription {
            AlphaToCoverageEnable = false,
            RenderTarget = new RenderTargetBlendDescription[] {
                new RenderTargetBlendDescription {
                    IsBlendEnabled        = true,
                    SourceBlend           = Blend.SourceAlpha,
                    DestinationBlend      = Blend.InverseSourceAlpha,
                    BlendOperation        = BlendOperation.Add,
                    SourceBlendAlpha      = Blend.InverseSourceAlpha,
                    DestinationBlendAlpha = Blend.Zero,
                    BlendOperationAlpha   = BlendOperation.Add,
                    RenderTargetWriteMask = ColorWriteEnable.All
                }
            }
        };

        this._blendState = this._backend.Device.CreateBlendState(blendDescription);
        //this._blendState.DebugName = "ImGui BlendState";

        RasterizerDescription rasterizerDescription = new RasterizerDescription {
            FillMode        = FillMode.Solid,
            CullMode        = CullMode.None,
            ScissorEnable   = true,
            DepthClipEnable = true
        };

        this._rasterizerState = this._backend.Device.CreateRasterizerState(rasterizerDescription);
        //this._rasterizerState.DebugName = "ImGui RasterizerState";

        DepthStencilOperationDescription depthStencilOperationDescription = new DepthStencilOperationDescription(
        StencilOperation.Keep,
        StencilOperation.Keep,
        StencilOperation.Keep,
        ComparisonFunction.Always
        );

        DepthStencilDescription depthStencilDescription = new DepthStencilDescription {
            DepthEnable    = false,
            DepthWriteMask = DepthWriteMask.All,
            DepthFunc      = ComparisonFunction.Always,
            StencilEnable  = false,
            FrontFace      = depthStencilOperationDescription,
            BackFace       = depthStencilOperationDescription
        };

        this._depthStencilState = this._backend.Device.CreateDepthStencilState(depthStencilDescription);
        //this._depthStencilState.DebugName = "ImGui DepthStencilState";

        CreateFontsTexture();
    }

    private unsafe void CreateFontsTexture() {
        ImGuiIOPtr io = ImGui.GetIO();

        byte* pixels;
        int   width, height;

        io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height);

        Texture2DDescription texture2DDescription = new Texture2DDescription {
            Width     = width,
            Height    = height,
            MipLevels = 1,
            ArraySize = 1,
            Format    = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription {
                Count   = 1,
                Quality = 0
            },
            Usage          = ResourceUsage.Default,
            BindFlags      = BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.None
        };

        SubresourceData subresourceData = new SubresourceData {
            DataPointer = (IntPtr)pixels,
            RowPitch    = width * 4,
            SlicePitch  = 0
        };

        ID3D11Texture2D fontTexture = this._backend.Device.CreateTexture2D(
        texture2DDescription,
        new[] {
            subresourceData
        }
        );
        //fontTexture.DebugName = "ImGui Font Texture";

        ShaderResourceViewDescription shaderResourceViewDescription = new ShaderResourceViewDescription {
            Format        = Format.R8G8B8A8_UNorm,
            ViewDimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new Texture2DShaderResourceView {
                MipLevels       = 1,
                MostDetailedMip = 0
            }
        };

        this._fontTextureView =
            this._backend.Device.CreateShaderResourceView(fontTexture, shaderResourceViewDescription);
        //this._fontTextureView.DebugName = "ImGui Font Texture Atlas";

        fontTexture.Dispose();

        io.Fonts.TexID = RegisterTexture(this._fontTextureView);

        SamplerDescription samplerDescription = new SamplerDescription {
            Filter             = Filter.MinMagMipLinear,
            AddressU           = TextureAddressMode.Wrap,
            AddressV           = TextureAddressMode.Wrap,
            AddressW           = TextureAddressMode.Wrap,
            MipLODBias         = 0f,
            ComparisonFunction = ComparisonFunction.Always,
            MinLOD             = 0f,
            MaxLOD             = 0f
        };

        this._fontSampler = this._backend.Device.CreateSamplerState(samplerDescription);
        //this._fontSampler.DebugName = "ImGui Font Sampler";
    }

    IntPtr RegisterTexture(ID3D11ShaderResourceView texture) {
        IntPtr imGuiId = texture.NativePointer;
        _textureResources.Add(imGuiId, texture);

        return imGuiId;
    }

    private void ReleaseAndNullify<pT>(ref pT o) where pT : ComObject {
        o.Dispose();
        o = null;
    }

    private void InvalidateDeviceObjects() {
        try {
            ReleaseAndNullify(ref _fontSampler);
            ReleaseAndNullify(ref _fontTextureView);
            ReleaseAndNullify(ref _indexBuffer);
            ReleaseAndNullify(ref _vertexBuffer);
            ReleaseAndNullify(ref _blendState);
            ReleaseAndNullify(ref _depthStencilState);
            ReleaseAndNullify(ref _rasterizerState);
            ReleaseAndNullify(ref _pixelShader);
            ReleaseAndNullify(ref _pixelShaderBlob);
            ReleaseAndNullify(ref _constantBuffer);
            ReleaseAndNullify(ref _inputLayout);
            ReleaseAndNullify(ref _vertexShader);
            ReleaseAndNullify(ref _vertexShaderBlob);
        }
        catch (NullReferenceException) {/* Apperantly thing?.Dispose can still throw a NullRefException? */
        }
    }

    private bool _isDisposed = false;

    public void Dispose() {
        if (this._isDisposed)
            return;

        this._isDisposed = true;

        if (this._backend.Device == null)
            return;

        this.InvalidateDeviceObjects();
    }
}
#endif