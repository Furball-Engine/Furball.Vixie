using System;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Helpers.Helpers;
using Silk.NET.Core.Native;
using Silk.NET.OpenCL;
using SixLabors.ImageSharp.PixelFormats;

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

    private readonly nint _radiusBuffer;
    private readonly nint _sourceImage;
    private readonly nint _destImage;

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

        int radius = this.KernelRadius;
        this._radiusBuffer = this._cl.CreateBuffer(
            this._context,
            MemFlags.ReadOnly | MemFlags.CopyHostPtr,
            sizeof(int),
            &radius,
            &err
        );
        ThrowIfError(err);

        ImageFormat imgFormat = new ImageFormat(ChannelOrder.Rgba, ChannelType.UnsignedInt8);

        //TODO: pull this in UpdateTexture() so it can be updated
        //TODO: we should read directly from a GL texture if possible
        Rgba32[] data = sourceTex.GetData();
        fixed (void* ptr = data)
            this._sourceImage = this._cl.CreateImage2D(
                this._context,
                MemFlags.ReadOnly | MemFlags.CopyHostPtr,
                &imgFormat,
                (nuint)this._sourceTex.Width,
                (nuint)this._sourceTex.Height,
                0,
                ptr,
                &err
            );
        ThrowIfError(err);

        //TODO: we should write directly to a GL texture if possible
        this._destImage = this._cl.CreateImage2D(
            this._context,
            MemFlags.WriteOnly | MemFlags.HostReadOnly,
            &imgFormat,
            (nuint)this._sourceTex.Width,
            (nuint)this._sourceTex.Height,
            0,
            null,
            &err
        );
        ThrowIfError(err);
    }

    private void PrintDeviceInfo() {
        this._cl.GetDeviceInfo(this._device, DeviceInfo.Name, 0, null, out nuint size);
        byte* name = stackalloc byte[(int)size];
        this._cl.GetDeviceInfo(this._device, DeviceInfo.Name, size, name, null);
        Console.WriteLine($"Using OpenCL device {SilkMarshal.PtrToString((nint)name)}");
        
        this._cl.GetDeviceInfo(this._device, DeviceInfo.MaxReadImageArgs, 0, null, out size);
        int* maxReadImageArgs = stackalloc int[(int)size];
        this._cl.GetDeviceInfo(this._device, DeviceInfo.MaxReadImageArgs, size, maxReadImageArgs, null);
        Console.WriteLine($"Max read image args: {maxReadImageArgs[0]}");
    }

    private static void ThrowIfError(int code) {
        if (code != 0)
            throw new Exception($"OpenCL Error: {(ErrorCodes)code}");
        // Console.WriteLine($"OpenCL Error: {(ErrorCodes)code}");
    }

    private static void NotifyFunc(byte* errorInfo, void* privateInfo, nuint cb, void* userdata) {
        Console.WriteLine($"OpenCL Error: {SilkMarshal.PtrToString((nint)errorInfo)}");
    }

    public override void UpdateTexture() {
        ThrowIfError(this._cl.SetKernelArg(this._kernel, 0, sizeof(int), in this._radiusBuffer));
        ThrowIfError(this._cl.SetKernelArg(this._kernel, 1, (nuint)sizeof(nint), in this._sourceImage));
        ThrowIfError(this._cl.SetKernelArg(this._kernel, 2, (nuint)sizeof(nint), in this._destImage));

        nuint* globalWorkSize = stackalloc nuint[2];
        globalWorkSize[0] = (nuint)this._sourceTex.Width;
        globalWorkSize[1] = (nuint)this._sourceTex.Height;

        this._cl.Finish(this._commandQueue);
        
        ThrowIfError(this._cl.EnqueueNdrangeKernel(
                         this._commandQueue,
                         this._kernel,
                         2,
                         null,
                         globalWorkSize,
                         null,
                         0,
                         null,
                         null
                     ));

        ThrowIfError(this._cl.Finish(this._commandQueue));

        nuint* origin = stackalloc nuint[3];
        origin[0] = 0;
        origin[1] = 0;
        origin[2] = 0;
        
        nuint* region = stackalloc nuint[3];
        region[0] = (nuint)this.Texture.Width;
        region[1] = (nuint)this.Texture.Height;
        region[2] = 1;

        Rgba32[] results = new Rgba32[this.Texture.Width * this.Texture.Height];
        fixed (void* ptr = results)
            ThrowIfError(this._cl.EnqueueReadImage(
                             this._commandQueue,
                             this._destImage,
                             true,
                             origin,
                             region,
                             0,
                             0,
                             ptr,
                             0,
                             null,
                             null
                         ));

        ThrowIfError(this._cl.Finish(this._commandQueue));

        this.Texture.SetData<Rgba32>(results);
    }

    public override VixieTexture Texture {
        get;
    }
    public override void Dispose() {
        this.Texture.Dispose();
        this._cl.Dispose();
    }
}