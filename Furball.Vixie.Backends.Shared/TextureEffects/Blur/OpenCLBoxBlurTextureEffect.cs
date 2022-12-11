using System;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Helpers.Helpers;
using Silk.NET.Core.Native;
using Silk.NET.OpenCL;

namespace Furball.Vixie.Backends.Shared.TextureEffects.Blur;

// ReSharper disable once InconsistentNaming
public unsafe class OpenCLBoxBlurTextureEffect : BoxBlurTextureEffect {
    private readonly GraphicsBackend _backend;
    private readonly CL              _cl;
    private readonly VixieTexture    _sourceTex;

    private nint _device;
    private nint _platform;

    private readonly nint _context;
    private readonly nint _commandQueue;
    private readonly nint _program;
    private readonly nint _kernel;

    public OpenCLBoxBlurTextureEffect(GraphicsBackend backend, VixieTexture sourceTex) : base(sourceTex) {
        this._backend   = backend;
        this._sourceTex = sourceTex;

        this.Texture = backend.CreateEmptyTexture((uint)sourceTex.Width, (uint)sourceTex.Height);

        this._cl = CL.GetApi();

        int err;

        ThrowIfError(this._cl.GetPlatformIDs(1, out this._platform, null));
        ThrowIfError(this._cl.GetDeviceIDs(this._platform, DeviceType.Gpu, 1, out this._device, null));

        ContextProperties* props = stackalloc ContextProperties[3];
        props[0] = ContextProperties.Platform;
        props[1] = (ContextProperties)this._platform;
        props[2] = 0;

        this._context = this._cl.CreateContext((nint*)props, 1, in this._device, NotifyFunc, null, &err);
        ThrowIfError(err);

        this.PrintDeviceInfo();

        this._commandQueue =
            this._cl.CreateCommandQueue(this._context, this._device, CommandQueueProperties.None, &err);
        ThrowIfError(err);

        string kernelCode = ResourceHelpers.GetStringResource
        ("TextureEffects/Blur/BoxBlur.cl",
         typeof(OpenCLBoxBlurTextureEffect));

        this._program =
            this._cl.CreateProgramWithSource(
                this._context,
                1,
                new[] { kernelCode },
                null,
                &err
            );
        ThrowIfError(err);

        try {
            ThrowIfError(
                this._cl.BuildProgram(this._program, 0, null, (byte*)null, null, null));
        }
        catch {
            nuint logSize = 0;
            ThrowIfError(this._cl.GetProgramBuildInfo(
                             this._program,
                             this._device,
                             ProgramBuildInfo.BuildLog,
                             0,
                             null,
                             &logSize
                         ));
            nint logStr = SilkMarshal.AllocateString((int)logSize);
            ThrowIfError(this._cl.GetProgramBuildInfo(
                             this._program,
                             this._device,
                             ProgramBuildInfo.BuildLog,
                             logSize,
                             (byte*)logStr,
                             null
                         ));
            throw new Exception($"OpenCL Kernel Build error! {SilkMarshal.PtrToString(logStr)}");
        }

        this._kernel = this._cl.CreateKernel(this._program, "box_blur", &err);
        ThrowIfError(err);
    }
    
    private void PrintDeviceInfo() {
        this._cl.GetDeviceInfo(this._device, DeviceInfo.Name, 0, null, out nuint size);
        byte* name = stackalloc byte[(int)size];
        this._cl.GetDeviceInfo(this._device, DeviceInfo.Name, size, name, null);
        Console.WriteLine($"Using OpenCL device {SilkMarshal.PtrToString((nint)name)}");
    }

    private static void ThrowIfError(int code) {
        if (code != 0)
            throw new Exception($"OpenCL Error: {(ErrorCodes)code}");
    }

    private static void NotifyFunc(byte* errorInfo, void* privateInfo, nuint cb, void* userdata) {
        Console.WriteLine($"OpenCL Error: {SilkMarshal.PtrToString((nint)errorInfo)}");
    }

    public override void UpdateTexture() {}

    public override VixieTexture Texture {
        get;
    }
    public override void Dispose() {
        this.Texture.Dispose();
        this._cl.Dispose();
    }
}