using System;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Furball.Vixie.Backends.Vulkan; 

public class Shader : IDisposable {
    private Vk     _vk;
    private Device _device;

    private ShaderModule     _shaderModule;
    private string           _entrypoint;
    private ShaderStageFlags _shaderStage;

    public unsafe Shader(VulkanBackend backend, byte[] shaderByteCode, ShaderStageFlags shaderStage, string entrypoint) {
        this._vk          = backend.GetVk();
        this._device      = backend.GetDevice();
        this._entrypoint  = entrypoint;
        this._shaderStage = shaderStage;

        fixed (void* shaderByteCodePtr = shaderByteCode) {
            ShaderModuleCreateInfo shaderModuleCreateInfo = new ShaderModuleCreateInfo {
                SType    = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)shaderByteCode.Length,
                PCode    = (uint*) shaderByteCodePtr
            };

            Result creationResult = this._vk.CreateShaderModule(this._device, shaderModuleCreateInfo, null, out this._shaderModule);

            if (creationResult != Result.Success)
                throw new Exception("Shader Module creation failed!");
        }
    }

    public unsafe ShaderStagePipelineCreateInfo GetPipelineCreateInfo() {
        ShaderStagePipelineCreateInfo info = new() {
            Info = new PipelineShaderStageCreateInfo {
                SType  = StructureType.PipelineShaderStageCreateInfo,
                Stage  = this._shaderStage,
                Module = this._shaderModule,
                PName  = (byte*)SilkMarshal.StringToPtr(this._entrypoint) //this needs to be manually freed
            }
        };
        
        return info;
    }

    public unsafe void Dispose() {
        this._vk.DestroyShaderModule(this._device, this._shaderModule, null);
    }
}

public class ShaderStagePipelineCreateInfo {
    public PipelineShaderStageCreateInfo Info;

    //We need to free the PName when the destructor gets hit
    unsafe ~ShaderStagePipelineCreateInfo() {
        SilkMarshal.Free((nint)this.Info.PName);
    }
}