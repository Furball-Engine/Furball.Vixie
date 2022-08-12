using System;
using System.IO;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Kettu;
using SharpDX.Direct3D9;
using SharpDX.Mathematics.Interop;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using Texture = Furball.Vixie.Backends.Shared.Texture;

namespace Furball.Vixie.Backends.Direct3D9;

public class Direct3D9Backend : IGraphicsBackend {
    private          Direct3D      _direct3D;
    private          Device        _device;
    private          SwapChain     _swapChain;
    private readonly RawColorBGRA  _clearColor = new(0, 0, 0, 255);
    private          Vector2D<int> _currentViewport;

    private ImGuiController _imgui;

    public static int DeviceOverride = 0;

    private bool TryCreateDevice(int deviceId, IntPtr hwnd, PresentParameters presentParameters, out Device device) {
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

    private bool TryCreateDevice(DeviceType type, int deviceId, IntPtr hwnd, PresentParameters presentParameters,
                                 out Device device) {
        Capabilities caps = this._direct3D.GetDeviceCaps(deviceId, type);

        Logger.Log($"Trying to create Device [{deviceId}] as {type.ToString()}", LoggerLevelD3D9.InstanceInfo);

        //Check for all the device features we require
        if ((caps.TextureCaps & TextureCaps.Pow2)               != 0 ||
            (caps.TextureCaps & TextureCaps.NonPow2Conditional) != 0 ||
            (caps.TextureCaps & TextureCaps.SquareOnly)         != 0 ||
            caps.VertexShaderVersion.Major                      < 2  ||
            caps.PixelShaderVersion.Major                       < 2
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


        device = new Device(this._direct3D, deviceId, type, hwnd, createFlags, presentParameters);

        if (device == null)
            return false;

        return true;
    }

    private void PrintAdapterInfo() {
        for (int i = 0; i != this._direct3D.AdapterCount; i++) {
            AdapterDetails details = this._direct3D.GetAdapterIdentifier(i);

            BackendInfoSection deviceInformation = new($"Device #{i}");

            deviceInformation.Contents.Add(("Driver", details.Driver));
            deviceInformation.Contents.Add(("Adapter Description", details.Description));
            deviceInformation.Contents.Add(("Device Name", details.DeviceName));
            deviceInformation.Contents.Add(("Driver Version", details.DriverVersion.ToString()));

            this.InfoSections.Add(deviceInformation);
        }
    }

    public override void Initialize(IView view, IInputContext inputContext) {
        this._direct3D = new Direct3D();

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

        this._device.Reset(presentParameters);

        this._currentViewport = new Vector2D<int>(width, height);
    }

    public override IQuadRenderer CreateTextureRenderer() => new QuadRendererD3D9();

    public override int QueryMaxTextureUnits() {
        throw new NotImplementedException();
    }

    public override void Clear() {
        this._device.Clear(ClearFlags.Target, this._clearColor, 0, 0);
    }

    public override void TakeScreenshot() {
        throw new NotImplementedException();
    }

    private Rectangle _lastScissor;

    public override Rectangle ScissorRect {
        get => this._lastScissor;
        set {
            this._lastScissor = value;

            this._device.ScissorRect = new RawRectangle(value.Left, value.Top, value.Right, value.Bottom);
        }
    }

    public override void SetFullScissorRect() {
        this._device.ScissorRect = new RawRectangle(0, 0, this._currentViewport.X, this._currentViewport.Y);
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

    public override void EndScene() {
        this._device.EndScene();
    }

    public override void Present() {
        this._device.Present();
    }
}