using System;
using System.Collections.Generic;
using System.IO;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Microsoft.Extensions.DependencyModel;
using Silk.NET.Core.Loader;
using Silk.NET.Direct3D9;
using Silk.NET.Input;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;

namespace Furball.Vixie.Backends.Direct3D9; 

public unsafe class Direct3D9Backend : IGraphicsBackend {
    internal D3D9 D3D9Api;
    
    public override void Initialize(IView view, IInputContext inputContext) {
        this.D3D9Api = D3D9.GetApi();

        IDirect3D9* d3d = this.D3D9Api.Direct3DCreate9(D3D9.SdkVersion);

        uint u = d3d->GetAdapterCount();

        AdapterIdentifier9 adapterIdentifier9 = new AdapterIdentifier9();
        d3d->GetAdapterIdentifier(0, 0, &adapterIdentifier9);
        
        ;
    }
    public override void Cleanup() {
        throw new System.NotImplementedException();
    }
    public override void HandleFramebufferResize(int width, int height) {
        throw new System.NotImplementedException();
    }
    public override IQuadRenderer CreateTextureRenderer() {
        throw new System.NotImplementedException();
    }
    public override int QueryMaxTextureUnits() {
        throw new System.NotImplementedException();
    }
    public override void Clear() {
        throw new System.NotImplementedException();
    }
    public override void TakeScreenshot() {
        throw new System.NotImplementedException();
    }
    public override Rectangle ScissorRect {
        get;
        set;
    }
    public override void SetFullScissorRect() {
        throw new System.NotImplementedException();
    }
    public override TextureRenderTarget CreateRenderTarget(uint width, uint height) {
        throw new System.NotImplementedException();
    }
    public override Texture CreateTexture(byte[] imageData, bool qoi = false) {
        throw new System.NotImplementedException();
    }
    public override Texture CreateTexture(Stream stream) {
        throw new System.NotImplementedException();
    }
    public override Texture CreateTexture(uint width, uint height) {
        throw new System.NotImplementedException();
    }
    public override Texture CreateTexture(string filepath) {
        throw new System.NotImplementedException();
    }
    public override Texture CreateWhitePixelTexture() {
        throw new System.NotImplementedException();
    }
    public override void ImGuiUpdate(double deltaTime) {
        throw new System.NotImplementedException();
    }
    public override void ImGuiDraw(double deltaTime) {
        throw new System.NotImplementedException();
    }
}