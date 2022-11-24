#if USE_IMGUI
using System;
using System.Collections.Generic;
using System.Numerics;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using ImGuiNET;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using Silk.NET.Windowing;

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

    private ComPtr<ID3D10Blob>               _vertexShaderBlob  = null!;
    private ComPtr<ID3D10Blob>               _pixelShaderBlob   = null!;
    private ComPtr<ID3D11VertexShader>       _vertexShader      = null!;
    private ComPtr<ID3D11PixelShader>        _pixelShader       = null!;
    private ComPtr<ID3D11InputLayout>        _inputLayout       = null!;
    private ComPtr<ID3D11Buffer>             _vertexBuffer      = null!;
    private ComPtr<ID3D11Buffer>             _indexBuffer       = null!;
    private ComPtr<ID3D11Buffer>             _constantBuffer    = null!;
    private ComPtr<ID3D11BlendState>         _blendState        = null!;
    private ComPtr<ID3D11RasterizerState>    _rasterizerState   = null!;
    private ComPtr<ID3D11DepthStencilState>  _depthStencilState = null!;
    private ComPtr<ID3D11ShaderResourceView> _fontTextureView   = null!;
    private ComPtr<ID3D11SamplerState>       _fontSampler       = null!;

    private int _vertexBufferSize;
    private int _indexBufferSize;

    private Dictionary<IntPtr, ComPtr<ID3D11ShaderResourceView>> _textureResources = new();

    private bool _frameBegun;

    public ImGuiControllerD3D11(
        Direct3D11Backend backend, IView view, IInputContext context, ImGuiFontConfig? fontConfig,
        Action?           onConfigureIo = null
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

        if (this._vertexBuffer.Handle == null || this._vertexBufferSize < drawData.TotalVtxCount) {
            this._vertexBuffer.Dispose();

            this._vertexBufferSize += 5000;

            BufferDesc vertexBufferDesc = new() {
                Usage          = Usage.Dynamic,
                ByteWidth      = (uint)(this._vertexBufferSize * sizeof(ImDrawVert)),
                BindFlags      = (uint)BindFlag.VertexBuffer,
                CPUAccessFlags = (uint)CpuAccessFlag.Write
            };

            this._backend.Device.CreateBuffer(vertexBufferDesc, null, ref this._vertexBuffer);
            //this._vertexBuffer.DebugName = "ImGui Vertex Buffer";
        }

        if (this._indexBuffer.Handle == null || this._indexBufferSize < drawData.TotalIdxCount) {
            this._indexBuffer.Dispose();

            this._indexBufferSize += 5000;

            BufferDesc indexBufferDesc = new() {
                Usage          = Usage.Dynamic,
                ByteWidth      = (uint)(this._indexBufferSize * sizeof(ushort)),
                BindFlags      = (uint)BindFlag.IndexBuffer,
                CPUAccessFlags = (uint)CpuAccessFlag.Write
            };

            this._backend.Device.CreateBuffer(indexBufferDesc, null, ref this._indexBuffer);
            //this._indexBuffer.DebugName = "ImGui Index Buffer";
        }

        MappedSubresource vertexResource = new MappedSubresource();
        MappedSubresource indexResource  = new MappedSubresource();

        this._backend.DeviceContext.Map(this._vertexBuffer, 0, Map.WriteDiscard, 0, ref vertexResource);
        this._backend.DeviceContext.Map(this._indexBuffer, 0, Map.WriteDiscard, 0, ref indexResource);

        ImDrawVert* vertexResourcePointer = (ImDrawVert*)vertexResource.PData;
        ushort*     indexResourcePointer  = (ushort*)indexResource.PData;

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
        this._backend.DeviceContext.Unmap(this._indexBuffer, 0);

        MappedSubresource constantBufferResource = new();
        this._backend.DeviceContext.Map(this._constantBuffer!, 0, Map.WriteDiscard, 0, ref constantBufferResource);
        Span<float> constantBufferResourceSpan = new Span<float>(constantBufferResource.PData, 16 * sizeof(float));

        float l = drawData.DisplayPos.X;
        float r = drawData.DisplayPos.X + drawData.DisplaySize.X;
        float t = drawData.DisplayPos.Y;
        float b = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

        float[] projectionMatrix = new[] {
            2.0f / (r - l), 0.0f, 0.0f, 0.0f, 0.0f, 2.0f / (t - b), 0.0f, 0.0f, 0.0f, 0.0f, 0.5f, 0.0f,
            (r        + l)                               / (l - r), (t + b) / (b - t), 0.5f, 1.0f,
        };

        projectionMatrix.CopyTo(constantBufferResourceSpan);

        this._backend.DeviceContext.Unmap(this._constantBuffer, 0);

        #region Save Render State

        uint oldNumScissorRects = 0;
        uint oldVertexShaderInstancesCount, oldGeometryShaderInstancesCount;

        Box2D<int>[]                     oldScissorRectangles           = new Box2D<int>[16];
        Viewport                         oldViewport                    = new();
        ComPtr<ID3D11ShaderResourceView> oldShaderResourceView          = new ComPtr<ID3D11ShaderResourceView>();
        ComPtr<ID3D11SamplerState>       oldSamplerState                = new ComPtr<ID3D11SamplerState>();
        ComPtr<ID3D11ClassInstance>[]    oldPixelShaderInstances        = new ComPtr<ID3D11ClassInstance>[256];
        ComPtr<ID3D11ClassInstance>[]    oldVertexShaderInstances       = new ComPtr<ID3D11ClassInstance>[256];
        ComPtr<ID3D11ClassInstance>[]    oldGeometryShaderInstances     = new ComPtr<ID3D11ClassInstance>[256];
        ComPtr<ID3D11Buffer>[]           oldVertexShaderConstantBuffers = new ComPtr<ID3D11Buffer>[1];
        ComPtr<ID3D11Buffer>[]           oldVertexBuffers               = new ComPtr<ID3D11Buffer>[1];
        uint[]                            oldVertexBufferStrides         = new uint[1];
        uint[]                            oldVertexBufferOffsets         = new uint[1];

        this._backend.DeviceContext.RSGetScissorRects(ref oldNumScissorRects, ref oldScissorRectangles[0]);
        uint viewportCount = 0;
        this._backend.DeviceContext.RSGetViewports(ref viewportCount, ref oldViewport);

        if (viewportCount == 0)
            throw new Exception("No viewports?");

        ComPtr<ID3D11RasterizerState> oldRasterizerState = null;
        this._backend.DeviceContext.RSGetState(ref oldRasterizerState);

        ComPtr<ID3D11BlendState> oldBlendState  = null;
        D3Dcolorvalue            oldBlendFactor = new D3Dcolorvalue();
        uint                     oldSampleMask  = 0;
        this._backend.DeviceContext.OMGetBlendState(
            ref oldBlendState,
            (float*)&oldBlendFactor,
            ref oldSampleMask
        );

        ComPtr<ID3D11DepthStencilState> oldDepthStencilState = null;
        uint                            oldStencilRef        = 0;
        this._backend.DeviceContext.OMGetDepthStencilState(
            ref oldDepthStencilState,
            ref oldStencilRef
        );

        this._backend.DeviceContext.PSGetShaderResources(0, 1, ref oldShaderResourceView);
        this._backend.DeviceContext.PSGetSamplers(0, 1, ref oldSamplerState);

        uint oldPixelShaderInstancesCount = oldVertexShaderInstancesCount = oldGeometryShaderInstancesCount = 256;

        ComPtr<ID3D11VertexShader>   oldVertexShader   = null;
        ComPtr<ID3D11PixelShader>    oldPixelShader    = null;
        ComPtr<ID3D11GeometryShader> oldGeometryShader = null;
        this._backend.DeviceContext.PSGetShader(ref oldPixelShader, ref oldPixelShaderInstances[0],
                                                ref oldPixelShaderInstancesCount);
        this._backend.DeviceContext.VSGetShader(ref oldVertexShader, ref oldVertexShaderInstances[0],
                                                ref oldVertexShaderInstancesCount);
        this._backend.DeviceContext.GSGetShader(ref oldGeometryShader, ref oldGeometryShaderInstances[0],
                                                ref oldGeometryShaderInstancesCount);

        this._backend.DeviceContext.VSGetConstantBuffers(0, 1, ref oldVertexShaderConstantBuffers[0]);

        D3DPrimitiveTopology oldPrimitiveTopology = D3DPrimitiveTopology.D3D10PrimitiveTopologyLinelist;
        this._backend.DeviceContext.IAGetPrimitiveTopology(ref oldPrimitiveTopology);
        ComPtr<ID3D11Buffer> oldIndexBuffer       = null;
        Format               oldIndexBufferFormat = Format.FormatUnknown;
        uint                 oldIndexBufferOffset = 0;
        this._backend.DeviceContext.IAGetIndexBuffer(ref oldIndexBuffer, ref oldIndexBufferFormat,
                                                     ref oldIndexBufferOffset);
        this._backend.DeviceContext.IAGetVertexBuffers(
            0,
            1,
            ref oldVertexBuffers[0],
            ref oldVertexBufferStrides[0],
            ref oldVertexBufferOffsets[0]
        );
        ComPtr<ID3D11InputLayout> oldInputLayout = null;
        this._backend.DeviceContext.IAGetInputLayout(ref oldInputLayout);

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

                Box2D<int> rectangle = new(
                    (int)(cmd.ClipRect.X - clipOff.X),
                    (int)(cmd.ClipRect.Y - clipOff.Y),
                    (int)(cmd.ClipRect.Z - clipOff.X),
                    (int)(cmd.ClipRect.W - clipOff.Y)
                );
                this._backend.DeviceContext.RSSetScissorRects(1, rectangle);

                this._textureResources.TryGetValue(cmd.TextureId, out ComPtr<ID3D11ShaderResourceView> texture);

                if (texture != null) {
                    this._backend.DeviceContext.PSSetShaderResources(0, 1, texture);
                    this._backend.DeviceContext.DrawIndexed(
                        cmd.ElemCount,
                        (uint)(cmd.IdxOffset + globalIndexOffset),
                        (int)(cmd.VtxOffset  + globalVertexOffset)
                    );
                }
            }

            globalIndexOffset  += cmdList.IdxBuffer.Size;
            globalVertexOffset += cmdList.VtxBuffer.Size;
        }

        #region Restore Render State

        this._backend.DeviceContext.RSSetScissorRects(oldNumScissorRects, oldScissorRectangles);
        this._backend.DeviceContext.RSSetViewports(1, oldViewport);
        this._backend.DeviceContext.RSSetState(oldRasterizerState);

        if (oldRasterizerState.Handle != null)
            oldRasterizerState.Dispose();

        this._backend.DeviceContext.OMSetBlendState(oldBlendState, (float*)&oldBlendFactor, oldSampleMask);

        if (oldBlendState.Handle != null)
            oldBlendState.Dispose();

        this._backend.DeviceContext.OMSetDepthStencilState(oldDepthStencilState, oldStencilRef);

        if (oldDepthStencilState.Handle != null)
            oldDepthStencilState.Dispose();

        this._backend.DeviceContext.PSSetShaderResources(0, 1, oldShaderResourceView!);

        if (oldShaderResourceView.Handle != null)
            oldShaderResourceView.Dispose();

        this._backend.DeviceContext.PSSetSamplers(0, 1, ref oldSamplerState);

        if (oldSamplerState.Handle != null)
            oldSamplerState.Dispose();

        this._backend.DeviceContext.PSSetShader(oldPixelShader, ref oldPixelShaderInstances[0],
                                                oldPixelShaderInstancesCount);

        if (oldPixelShader.Handle != null)
            oldPixelShader.Dispose();

        for (int i = 0; i < oldPixelShaderInstancesCount; i++)
            oldPixelShaderInstances[i].Dispose();

        this._backend.DeviceContext.VSSetShader(
            oldVertexShader,
            ref oldVertexShaderInstances[0],
            oldVertexShaderInstancesCount
        );

        if (oldVertexShader.Handle != null)
            oldVertexShader.Dispose();

        for (int i = 0; i < oldVertexShaderInstancesCount; i++)
            oldVertexShaderInstances[i].Dispose();

        this._backend.DeviceContext.GSSetShader(
            oldGeometryShader,
            ref oldGeometryShaderInstances[0],
            oldGeometryShaderInstancesCount
        );

        if (oldGeometryShader.Handle != null)
            oldGeometryShader.Dispose();

        for (int i = 0; i < oldGeometryShaderInstancesCount; i++)
            oldGeometryShaderInstances[i].Dispose();

        this._backend.DeviceContext.VSSetConstantBuffers(0, 1, ref oldVertexShaderConstantBuffers[0]);

        if (oldVertexShaderConstantBuffers[0].Handle != null)
            oldVertexShaderConstantBuffers[0].Dispose();

        this._backend.DeviceContext.IASetPrimitiveTopology(oldPrimitiveTopology);
        this._backend.DeviceContext.IASetIndexBuffer(oldIndexBuffer, oldIndexBufferFormat, oldIndexBufferOffset);

        if (oldIndexBuffer.Handle != null)
            oldIndexBuffer.Dispose();

        this._backend.DeviceContext.IASetVertexBuffers(
            0u,
            1u,
            in oldVertexBuffers[0].Handle,
            in oldVertexBufferStrides[0],
            in oldVertexBufferOffsets[0]
        );

        if (oldVertexBuffers[0].Handle != null)
            oldVertexBuffers[0].Dispose();

        this._backend.DeviceContext.IASetInputLayout(oldInputLayout);

        if (oldInputLayout.Handle != null)
            oldInputLayout.Dispose();

        #endregion
    }

    private unsafe void SetupRenderState(ImDrawDataPtr drawData) {
        Viewport viewport = new(0, 0, drawData.DisplaySize.X, drawData.DisplaySize.Y, 0, 1);

        this._backend.DeviceContext.RSSetViewports(1, in viewport);

        uint stride = (uint)sizeof(ImDrawVert);
        uint offset = 0;

        this._backend.DeviceContext.IASetInputLayout(this._inputLayout);
        this._backend.DeviceContext.IASetVertexBuffers(
            0u,
            1u,
            in this._vertexBuffer.Handle,
            in stride,
            in offset
        );
        this._backend.DeviceContext.IASetIndexBuffer(this._indexBuffer, Format.FormatR16Uint, 0);
        this._backend.DeviceContext.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
        this._backend.DeviceContext.VSSetShader(this._vertexShader, null, 0);
        this._backend.DeviceContext.VSSetConstantBuffers(0, 1, this._constantBuffer);
        this._backend.DeviceContext.PSSetShader(this._pixelShader, null, 0);
        this._backend.DeviceContext.PSSetSamplers(0, 1, this._fontSampler);
        this._backend.DeviceContext.GSSetShader((ID3D11GeometryShader*)null, null, 0);
        this._backend.DeviceContext.HSSetShader((ID3D11HullShader*)null, null, 0);
        this._backend.DeviceContext.DSSetShader((ID3D11DomainShader*)null, null, 0);
        this._backend.DeviceContext.CSSetShader((ID3D11ComputeShader*)null, null, 0);
        this._backend.DeviceContext.OMSetBlendState(this._blendState, null, 0xFFFFFFFF);
        this._backend.DeviceContext.OMSetDepthStencilState(this._depthStencilState, 0);
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

        foreach (Key key in this._keys) {
            if (key == Key.Unknown)
                continue;

            io.KeysDown[(int)key] = keyboard.IsKeyPressed(key);
        }

        for (int i = 0; i != this._pressedCharacters.Count; i++)
            io.AddInputCharacter(this._pressedCharacters[i]);

        this._pressedCharacters.Clear();

        io.KeyCtrl  = keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight);
        io.KeyAlt   = keyboard.IsKeyPressed(Key.AltLeft)     || keyboard.IsKeyPressed(Key.AltRight);
        io.KeyShift = keyboard.IsKeyPressed(Key.ShiftLeft)   || keyboard.IsKeyPressed(Key.ShiftRight);
        io.KeySuper = keyboard.IsKeyPressed(Key.SuperLeft)   || keyboard.IsKeyPressed(Key.SuperRight);
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

        InputElementDesc[] inputElementDesc = new InputElementDesc[] {
            new("POSITION", 0, Format.R32G32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new("TEXCOORD", 0, Format.R32G32_Float, 8, 0, InputClassification.PerVertexData, 0),
            new("COLOR", 0, Format.R8G8B8A8_UNorm, 16, 0, InputClassification.PerVertexData, 0),
        };

        this._inputLayout = this._backend.Device.CreateInputLayout(inputElementDesc, vertexShaderData);
        //this._inputLayout.DebugName = "ImGui InputLayout";

        BufferDesc constantBufferDesc = new() {
            ByteWidth      = 16 * sizeof(float),
            Usage          = ResourceUsage.Dynamic,
            BindFlags      = BindFlags.ConstantBuffer,
            CPUAccessFlags = CpuAccessFlags.Write
        };

        this._constantBuffer = this._backend.Device.CreateBuffer(constantBufferDesc);
        //this._constantBuffer.DebugName = "ImGui ConstantBuffer";

        BlendDesc blendDesc = new() {
            AlphaToCoverageEnable = false,
            RenderTarget = new RenderTargetBlendDesc[] {
                new() {
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

        this._blendState = this._backend.Device.CreateBlendState(blendDesc);
        //this._blendState.DebugName = "ImGui BlendState";

        RasterizerDesc rasterizerDesc = new() {
            FillMode        = FillMode.Solid,
            CullMode        = CullMode.None,
            ScissorEnable   = true,
            DepthClipEnable = true
        };

        this._rasterizerState = this._backend.Device.CreateRasterizerState(rasterizerDesc);
        //this._rasterizerState.DebugName = "ImGui RasterizerState";

        DepthStencilOperationDesc depthStencilOperationDesc = new(
            StencilOperation.Keep,
            StencilOperation.Keep,
            StencilOperation.Keep,
            ComparisonFunction.Always
        );

        DepthStencilDesc depthStencilDesc = new() {
            DepthEnable    = false,
            DepthWriteMask = DepthWriteMask.All,
            DepthFunc      = ComparisonFunction.Always,
            StencilEnable  = false,
            FrontFace      = depthStencilOperationDesc,
            BackFace       = depthStencilOperationDesc
        };

        this._depthStencilState = this._backend.Device.CreateDepthStencilState(depthStencilDesc);
        //this._depthStencilState.DebugName = "ImGui DepthStencilState";

        CreateFontsTexture();
    }

    private unsafe void CreateFontsTexture() {
        ImGuiIOPtr io = ImGui.GetIO();

        byte* pixels;
        int   width, height;

        io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height);

        Texture2DDesc texture2DDesc = new() {
            Width     = width,
            Height    = height,
            MipLevels = 1,
            ArraySize = 1,
            Format    = Format.R8G8B8A8_UNorm,
            SampleDesc = new SampleDesc {
                Count   = 1,
                Quality = 0
            },
            Usage          = ResourceUsage.Default,
            BindFlags      = BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.None
        };

        SubresourceData subresourceData = new() {
            DataPointer = (IntPtr)pixels,
            RowPitch    = width * 4,
            SlicePitch  = 0
        };

        ID3D11Texture2D fontTexture = this._backend.Device.CreateTexture2D(
            texture2DDesc,
            new[] {
                subresourceData
            }
        );
        //fontTexture.DebugName = "ImGui Font Texture";

        ShaderResourceViewDesc shaderResourceViewDesc = new() {
            Format        = Format.R8G8B8A8_UNorm,
            ViewDimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new Texture2DShaderResourceView {
                MipLevels       = 1,
                MostDetailedMip = 0
            }
        };

        this._fontTextureView =
            this._backend.Device.CreateShaderResourceView(fontTexture, shaderResourceViewDesc);
        //this._fontTextureView.DebugName = "ImGui Font Texture Atlas";

        fontTexture.Dispose();

        io.Fonts.TexID = RegisterTexture(this._fontTextureView);

        SamplerDesc samplerDesc = new() {
            Filter             = Filter.MinMagMipLinear,
            AddressU           = TextureAddressMode.Wrap,
            AddressV           = TextureAddressMode.Wrap,
            AddressW           = TextureAddressMode.Wrap,
            MipLODBias         = 0f,
            ComparisonFunction = ComparisonFunction.Always,
            MinLOD             = 0f,
            MaxLOD             = 0f
        };

        this._fontSampler = this._backend.Device.CreateSamplerState(samplerDesc);
        //this._fontSampler.DebugName = "ImGui Font Sampler";
    }

    IntPtr RegisterTexture(ID3D11ShaderResourceView? texture) {
        IntPtr imGuiId = texture!.NativePointer;
        _textureResources.Add(imGuiId, texture);

        return imGuiId;
    }

    private void ReleaseAndNullify <pT>(ref pT? o) where pT : ComObject {
        o?.Dispose();
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
        catch (NullReferenceException) { /* Apperantly thing?.Dispose can still throw a NullRefException? */
        }
    }

    private          bool  _isDisposed = false;
    private readonly Array _keys       = Enum.GetValues(typeof(Key));

    public void Dispose() {
        if (this._isDisposed)
            return;

        this._isDisposed = true;

        // if (this._backend.Device == null)
        // return;

        this.InvalidateDeviceObjects();
    }
}
#endif