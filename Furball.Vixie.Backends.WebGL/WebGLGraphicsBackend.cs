﻿using System;
using System.IO;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Shared.Renderers;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Uno.Foundation;
using Uno.Foundation.Interop;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace Furball.Vixie.Backends.WebGL; 

// ReSharper disable once InconsistentNaming
public class WebGLGraphicsBackend : GraphicsBackend {
    public override void Initialize(IView view, IInputContext inputContext) {
#if USE_IMGUI
        throw new NotImplementedException();
#endif
        
        WebAssemblyRuntime.InvokeJS(@"gl = canvas.getContext('webgl');

gl.enable(gl.BLEND);
gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);

gl.enable(gl.SCISSOR_TEST);

gl.enable(gl.CULL_FACE);
gl.cullFace(gl.BACK);

//log the version of webgl
console.log('WebGL Version: ' + gl.getParameter(gl.VERSION));
console.log('WebGL Shading Language Version: ' + gl.getParameter(gl.SHADING_LANGUAGE_VERSION));
console.log('WebGL Vendor: ' + gl.getParameter(gl.VENDOR));
console.log('WebGL Renderer: ' + gl.getParameter(gl.RENDERER));
console.log('WebGL Extensions: ' + gl.getSupportedExtensions());

console.log('Initialized WebGL!');");
    }
    
    private  bool          _screenshotQueued;
    internal Vector2D<int> CurrentViewport;
    private  Rectangle     _lastScissor;

    public override void Cleanup() {
        // throw new NotImplementedException();
    }
    
    public override void HandleFramebufferResize(int width, int height) {
        this.CurrentViewport.X = width;
        this.CurrentViewport.Y = height;
        
        //Set the viewport
        WebAssemblyRuntime.InvokeJS($@"gl.viewport(0, 0, {width}, {height});");

        this.ScissorRect = new Rectangle(0, 0, width, height);
    }
    
    public override VixieRenderer CreateRenderer() {
        return new WebGLRenderer();
    }
    
    public override int QueryMaxTextureUnits() {
        // throw new NotImplementedException();

        return Convert.ToInt32(WebAssemblyRuntime.InvokeJS("gl.getParameter(gl.MAX_TEXTURE_IMAGE_UNITS)"));
    }
    
    public override void Clear() {
        WebAssemblyRuntime.InvokeJS(@"gl.clearColor(0.0, 0.0, 0.0, 1.0);
gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);");
    }
    public override void TakeScreenshot() {
        // throw new NotImplementedException();
    }
    public override Rectangle ScissorRect {
        get => this._lastScissor;
        set {
            this._lastScissor = value;
            WebAssemblyRuntime.InvokeJS($"gl.scissor({value.X}, {value.Y}, {value.Width}, {value.Height});");
        }
    }
    public override void SetFullScissorRect() {
        this.ScissorRect = new Rectangle(0, 0, this.CurrentViewport.X, this.CurrentViewport.Y);
    }
    public override ulong GetVramUsage() {
        // throw new NotImplementedException();
        return 0;
    }
    public override ulong GetTotalVram() {
        // throw new NotImplementedException();
        return 0;
    }
    public override VixieTextureRenderTarget CreateRenderTarget(uint width, uint height) {
        return new WebGLRenderTarget(width, height);
    }
    public override VixieTexture CreateTextureFromByteArray(byte[]            imageData,
                                                            TextureParameters parameters = default(TextureParameters)) {
        return new WebGLTexture(imageData, parameters);
    }
    public override VixieTexture CreateTextureFromStream(Stream            stream,
                                                         TextureParameters parameters = default(TextureParameters)) {
        return new WebGLTexture(stream, parameters);
    }
    public override VixieTexture CreateEmptyTexture(uint              width, uint height,
                                                    TextureParameters parameters = default(TextureParameters)) {
        return new WebGLTexture(width, height, parameters);
    }
    public override VixieTexture CreateWhitePixelTexture() {
        return new WebGLTexture();
    }
#if USE_IMGUI
    public override void ImGuiUpdate(double deltaTime) {
        throw new NotImplementedException();
    }
    public override void ImGuiDraw(double deltaTime) {
        throw new NotImplementedException();
    }
#endif
}