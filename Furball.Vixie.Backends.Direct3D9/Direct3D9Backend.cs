using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Kettu;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Direct3D9;
using Vortice.Mathematics;
using Color=Vortice.Mathematics.Color;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using Texture = Furball.Vixie.Backends.Shared.Texture;

namespace Furball.Vixie.Backends.Direct3D9;

public class Direct3D9Backend : IGraphicsBackend {
    private          IDirect3D9 _direct3D;
    private          IDirect3DDevice9           _device;
    private          IDirect3DSwapChain9        _swapChain;
    private readonly Color     _clearColor = new(0, 0, 0, 255);
    private          Vector2D<int>    _currentViewport;

    private ImGuiController _imgui;

    public static int DeviceOverride = 0;

    private bool TryCreateDevice(int deviceId, IntPtr hwnd, PresentParameters presentParameters, out IDirect3DDevice9 device) {
#if DEBUG
        if (this.TryCreateDevice(DeviceType.Reference, deviceId, hwnd, presentParameters, out device))
            return true;
#endif
        
        if (this.TryCreateDevice(DeviceType.Hardware, deviceId, hwnd, presentParameters, out device))
            return true;

        if (this.TryCreateDevice(DeviceType.Software, deviceId, hwnd, presentParameters, out device))
            return true;

        return false;
    }

    private unsafe bool TryCreateDevice(DeviceType type, int deviceId, IntPtr hwnd, PresentParameters presentParameters, out IDirect3DDevice9 device) {
        Capabilities caps = this._direct3D.GetDeviceCaps(deviceId, type);

        //Get to the 49th element int he D3DCAPS struct, this is where VertexShaderVersion lies
        //all this is done cuz vortice has both shader versions as internal :))))))
        int* capPtr = (int*) &caps + 49;
        int vertexShaderVersionMajor = (*capPtr >> 8) & 0xFF;

        //2 fields later is the Pixel Shader Version
        capPtr += 2;
        int pixelShaderVersionMajor = (*capPtr >> 8) & 0xFF;

        Logger.Log($"Trying to create Device [{deviceId}] as {type.ToString()}", LoggerLevelD3D9.InstanceInfo);

        //Check for all the device features we require
        if ((caps.TextureCaps & TextureCaps.Pow2)               != 0 ||
            (caps.TextureCaps & TextureCaps.NonPow2Conditional) != 0 ||
            (caps.TextureCaps & TextureCaps.SquareOnly)         != 0 ||
            vertexShaderVersionMajor                            < 2  ||
            pixelShaderVersionMajor                             < 2
            ) {
            device = null;

            Logger.Log($"Creating Device [{deviceId}] as {type.ToString()} failed!", LoggerLevelD3D9.InstanceError);
            Logger.Log(
                "The device either doesn't support NPOT at all times, or non-square textures, or a Vertex or Pixel shader version of at least 2.0",
                LoggerLevelD3D9.InstanceError);

            return false;
        }

        CreateFlags createFlags = (caps.DeviceCaps & DeviceCaps.HWTransformAndLight) != 0
            ? CreateFlags.HardwareVertexProcessing
            : CreateFlags.SoftwareVertexProcessing;

        device = this._direct3D.CreateDevice(deviceId, type, hwnd, createFlags, presentParameters);

        if (device == null)
            return false;

        return true;
    }

    private void PrintAdapterInfo() {
        for (int i = 0; i != this._direct3D.AdapterCount; i++) {
            AdapterIdentifier details = this._direct3D.GetAdapterIdentifier(i);

            BackendInfoSection deviceInformation = new($"Device #{i}");

            deviceInformation.Contents.Add(("Driver", details.Driver));
            deviceInformation.Contents.Add(("Adapter Description", details.Description));
            deviceInformation.Contents.Add(("Device Name", details.DeviceName));
            deviceInformation.Contents.Add(("Driver Version", details.DriverVersion.ToString()));

            this.InfoSections.Add(deviceInformation);
        }
    }

    public override unsafe void Initialize(IView view, IInputContext inputContext) {
        this._direct3D = D3D9.Direct3DCreate9();

        PresentParameters presentParameters = new() {
            BackBufferCount  = 1,
            BackBufferWidth  = view.FramebufferSize.X,
            BackBufferHeight = view.FramebufferSize.Y,
            BackBufferFormat = Format.X8R8G8B8,
            Windowed         = true,
            SwapEffect       = SwapEffect.Copy
        };

        this.PrintAdapterInfo();

        this.InfoSections.ForEach(x => x.Log(LoggerLevelD3D9.InstanceInfo));

        IntPtr windowHandle = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? view.Native!.Win32!.Value.Hwnd
            : view.Handle;

        if (DeviceOverride != 0)
            this.TryCreateDevice(DeviceOverride, windowHandle, presentParameters, out this._device);

        int i = 0;
        while (i != this._direct3D.AdapterCount && this._device == null) {
            if (this.TryCreateDevice(i, windowHandle, presentParameters, out this._device))
                break;

            i++;
        }

        if (this._device == null)
            throw new Exception("No suitable Direct3D9 Device found which matches Vixie's requirements!");

        this._currentViewport = new Vector2D<int>(view.FramebufferSize.X, view.FramebufferSize.Y);

        this._imgui = new ImGuiController(view, inputContext);
    }

    public override void Cleanup() {
        this._device.Dispose();
        this._direct3D.Dispose();
    }

    public override void HandleFramebufferResize(int width, int height) {
        PresentParameters presentParameters = new() {
            BackBufferCount  = 1,
            BackBufferWidth  = width,
            BackBufferHeight = height,
            BackBufferFormat = Format.X8R8G8B8,
            Windowed         = true,
            SwapEffect       = SwapEffect.Copy
        };

        this._device.Reset(ref presentParameters);

        this._currentViewport = new Vector2D<int>(width, height);
    }

    public override IQuadRenderer CreateTextureRenderer() => new QuadRendererD3D9(this._device);

    public override int QueryMaxTextureUnits() {
        throw new NotImplementedException();
    }

    public override void Clear() {
        this._device.Clear(ClearFlags.Target, this._clearColor, 1, 0);
    }

    public override void TakeScreenshot() {
        throw new NotImplementedException();
    }

    private Rectangle _lastScissor;

    public override Rectangle ScissorRect {
        get => this._lastScissor;
        set {
            this._lastScissor = value;

            this._device.ScissorRect = new Vortice.Direct3D9.Rect(value.Left, value.Top, value.Right, value.Bottom);
        }
    }

    public override void SetFullScissorRect() {
        this.ScissorRect = new Rectangle(0, 0, this._currentViewport.X, this._currentViewport.Y);
    }

    public override TextureRenderTarget CreateRenderTarget(uint width, uint height) {
        return new RenderTargetD3D9(width, height);
    }

    public override Texture CreateTextureFromByteArray(byte[] imageData, TextureParameters parameters = default) {
        return new TextureD3D9(imageData, parameters);
    }
    
    public override Texture CreateTextureFromStream(Stream stream, TextureParameters parameters = default) {
        return new TextureD3D9(stream, parameters);
    }
    
    public override Texture CreateEmptyTexture(uint width, uint height, TextureParameters parameters = default) {
        return new TextureD3D9(width, height, parameters);
    }

    public override Texture CreateWhitePixelTexture() {
        return new TextureD3D9();
    }

    public override void ImGuiUpdate(double deltaTime) {
        this._imgui.Update((float)deltaTime);
    }

    public override void ImGuiDraw(double deltaTime) {
        this._imgui.Render();
    }

    public override void BeginScene() {
        this._device.BeginScene();
    }

    public override unsafe void EndScene() {
        this._device.EndScene();
    }

    public override void Present() {
        this._device.Present();
    }
}