﻿using System;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Helpers.Helpers;
using Silk.NET.Core.Native;
using Silk.NET.OpenCL;
using SixLabors.ImageSharp.PixelFormats;

namespace Furball.Vixie.Backends.Shared.TextureEffects.Blur;

// ReSharper disable once InconsistentNaming
public sealed unsafe class OpenCLBoxBlurTextureEffect : BoxBlurTextureEffect {
    private readonly GraphicsBackend _backend;
    private readonly CL              _cl;
    private VixieTexture    _sourceTex;

    private nint _device;
    private nint _platform;

    private readonly nint _context;
    private readonly nint _commandQueue;
    private readonly nint _program;
    private readonly nint _kernel;

    private readonly nint _radiusBuffer;
    private nint? _sourceImage;
    private nint? _destImage;
    private VixieTexture? _texture;

    public OpenCLBoxBlurTextureEffect(GraphicsBackend backend, VixieTexture sourceTex) : base(sourceTex) {
        this._backend   = backend;

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

        this.SetSourceTexture(sourceTex);
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
    }

    private static void NotifyFunc(byte* errorInfo, void* privateInfo, nuint cb, void* userdata) {
        Console.WriteLine($"OpenCL Error: {SilkMarshal.PtrToString((nint)errorInfo)}");
    }

    public override void UpdateTexture() {
        Rgba32[] data = this._sourceTex.GetData();

        nuint* origin = stackalloc nuint[3];
        nuint* region = stackalloc nuint[3];
        origin[0] = 0;
        origin[1] = 0;
        origin[2] = 0;
        region[0] = (nuint)this._sourceTex.Width;
        region[1] = (nuint)this._sourceTex.Height;
        region[2] = 1;

        fixed (void* ptr = data)
            this._cl.EnqueueWriteImage(
                this._commandQueue,
                this._sourceImage.Value,
                false,
                origin,
                region,
                0,
                0,
                ptr,
                0,
                null,
                null
            );

        nint src = this._sourceImage.Value;
        nint dst = this._destImage.Value;
        
        ThrowIfError(this._cl.SetKernelArg(this._kernel, 0, sizeof(int), in this.KernelRadius));
        ThrowIfError(this._cl.SetKernelArg(this._kernel, 1, (nuint)sizeof(nint), in src));
        ThrowIfError(this._cl.SetKernelArg(this._kernel, 2, (nuint)sizeof(nint), in dst));

        nuint* globalWorkSize = stackalloc nuint[2];
        globalWorkSize[0] = (nuint)this._sourceTex.Width;
        globalWorkSize[1] = (nuint)this._sourceTex.Height;

        void DoPass(bool last) {
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

            if (last)
                return;

            this._cl.EnqueueCopyImage(
                this._commandQueue,
                this._destImage.Value,
                this._sourceImage.Value,
                origin,
                origin,
                region,
                0,
                null,
                null
            );
        }

        for (int i = 0; i < this.Passes - 1; i++) {
            DoPass(false);
        }
        DoPass(true);

        ThrowIfError(this._cl.Finish(this._commandQueue));

        Rgba32[] results = new Rgba32[this.Texture.Width * this.Texture.Height];
        fixed (void* ptr = results)
            ThrowIfError(this._cl.EnqueueReadImage(
                             this._commandQueue,
                             this._destImage.Value,
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

    public override void SetSourceTexture(VixieTexture tex) {
        this._sourceTex = tex;

        if (this._texture != null && tex.Size == this._texture.Size) return;
        
        this._texture?.Dispose();
        this._texture = this._backend.CreateEmptyTexture((uint)tex.Width, (uint)tex.Height);
        
        ImageFormat imgFormat = new ImageFormat(ChannelOrder.Rgba, ChannelType.UnsignedInt8);

        int err;

        if (this._sourceImage.HasValue)
            this._cl.ReleaseMemObject(this._sourceImage.Value);
        if (this._destImage.HasValue)
            this._cl.ReleaseMemObject(this._destImage.Value);
        
        this._sourceImage = this._cl.CreateImage2D(
            this._context,
            MemFlags.ReadOnly | MemFlags.HostWriteOnly,
            &imgFormat,
            (nuint)this._sourceTex.Width,
            (nuint)this._sourceTex.Height,
            0,
            null,
            &err
        );
        ThrowIfError(err);

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

    public override VixieTexture Texture => _texture;

    public override void Dispose() {
        this.Texture.Dispose();
        this._cl.Dispose();
    }
}