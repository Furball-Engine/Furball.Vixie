using System;
using System.Collections.Generic;
using System.IO;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Helpers;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle=System.Drawing.Rectangle;

namespace Furball.Vixie.Backends.OpenGL.Abstractions; 

internal sealed class VixieTextureGl : VixieTexture {
    private readonly OpenGLBackend _backend;

    private readonly bool _mipmaps = false;

    public void SetFlip() {
        this.InternalFlip = true;
    }
    
    /// <summary>
    /// Unique ID which identifies this Texture
    /// </summary>
    public uint TextureId {
        get;
        private set;
    }

    /// <summary>
    /// Creates a Texture from a byte array which contains Image Data
    /// </summary>
    /// <param name="backend"></param>
    /// <param name="imageData">Image Data</param>
    /// <param name="parameters"></param>
    public VixieTextureGl(OpenGLBackend backend, byte[] imageData, TextureParameters parameters) {
        this._backend = backend;
        this._backend.GlCheckThread();
        this._mipmaps = parameters.RequestMipmaps;

        Image<Rgba32> image;

        bool qoi = imageData.Length > 3 && imageData[0] == 'q' && imageData[1] == 'o' && imageData[2] == 'i' &&
                   imageData[3]     == 'f';

        if(qoi) {
            (Rgba32[] pixels, QoiLoader.QoiHeader header) data = QoiLoader.Load(imageData);

            image = Image.LoadPixelData(data.pixels, (int)data.header.Width, (int)data.header.Height);
        } else {
            image = Image.Load<Rgba32>(imageData);
        }

        int width  = image.Width;
        int height = image.Height;

        this.Load(image);

        this.Size = new Vector2D<int>(width, height);

        this.FilterType = parameters.FilterType;
    }
    /// <summary>
    /// Creates a Texture with a single White Pixel
    /// </summary>
    public unsafe VixieTextureGl(OpenGLBackend backend) {
        this._backend = backend;
        this._backend.GlCheckThread();
            
        this.TextureId = this._backend.GenTexture();
        this._backend.CheckError("gen white pixel texture");
        //Bind as we will be working on the Texture
        this._backend.BindTexture(TextureTarget.Texture2D, this.TextureId);
        this._backend.CheckError("bind genned texture");
        //Apply Linear filtering, and make Image wrap around and repeat
        this._backend.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
        this._backend.CheckError("set texminfilter");
        this._backend.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
        this._backend.CheckError("set texmagfilter");
        this._backend.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,     (int) GLEnum.Repeat);
        this._backend.CheckError("set texture wrap s");
        this._backend.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,     (int)GLEnum.Repeat);
        this._backend.CheckError("set texture wrap t");
        //White color
        uint color = 0xffffffff;
        //Upload Image Data
        this._backend.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, &color);
        this._backend.CheckError("fill white pixel with a single uint colour");
        //Unbind as we have finished
        this._backend.BindTexture(TextureTarget.Texture2D, 0);
        this._backend.CheckError("create white pixel texture");

        this.Size = new Vector2D<int>(1, 1);
    }
    /// <summary>
    /// Creates a Empty texture given a width and height
    /// </summary>
    /// <param name="backend"></param>
    /// <param name="width">Desired Width</param>
    /// <param name="height">Desired Height</param>
    /// <param name="parameters"></param>
    public unsafe VixieTextureGl(OpenGLBackend backend, uint width, uint height, TextureParameters parameters) {
        this._backend = backend;
        this._backend.GlCheckThread();
        this._mipmaps = parameters.RequestMipmaps;

        this.TextureId = this._backend.GenTexture();
        //Bind as we will be working on the Texture
        this._backend.BindTexture(TextureTarget.Texture2D, this.TextureId);
        this._backend.CheckError("bind");
        //Apply Linear filtering, and make Image wrap around and repeat
        this.FilterType = parameters.FilterType;
        this._backend.CheckError("filtertype");
        this._backend.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        this._backend.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        //Upload Image Data
        this._backend.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
        //Unbind as we have finished
        this._backend.BindTexture(TextureTarget.Texture2D, 0);
        this._backend.CheckError("create blank texture with width + height");

        this.Size = new Vector2D<int>((int)width, (int)height);
    }
    /// <summary>
    /// Creates a Texture from a Stream which Contains Image Data
    /// </summary>
    /// <param name="backend"></param>
    /// <param name="stream">Image Data Stream</param>
    /// <param name="parameters"></param>
    public VixieTextureGl(OpenGLBackend backend, Stream stream, TextureParameters parameters) {
        this._backend = backend;
        this._backend.GlCheckThread();
        this._mipmaps = parameters.RequestMipmaps;

        Image<Rgba32> image = Image.Load<Rgba32>(stream);

        int width  = image.Width;
        int height = image.Height;

        this.Load(image);

        this.Size = new Vector2D<int>(width, height);

        this.FilterType = parameters.FilterType;
        
        this._backend.CheckError("create texture");
    }

    private void GenMipmaps() {
        if (!this._mipmaps)
            return;

        ((IGlBasedBackend)this._backend).GenerateMipmaps(this);
    }
    
    ~VixieTextureGl() {
        DisposeQueue.Enqueue(this);
    }

    private unsafe void Load(Image<Rgba32> image) {
        this._backend.GlCheckThread();
        this.Load(null, image.Width, image.Height);
        this.Bind();
        image.ProcessPixelRows(accessor =>
        {
            for (int i = 0; i < accessor.Height; i++) {
                fixed(void* ptr = accessor.GetRowSpan(i))
                    this._backend.TexSubImage2D(TextureTarget.Texture2D, 0, 0, i, (uint)accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        });
        this.GenMipmaps();
        this.Unbind();
        this._backend.CheckError("fill texture");
    }

    /// <summary>
    /// Creates a Texture class using a already Generated OpenGL Texture
    /// </summary>
    /// <param name="backend">The OpenGL backend</param>
    /// <param name="textureId">OpenGL Texture ID</param>
    /// <param name="width">Width of the Texture</param>
    /// <param name="height">Height of the Texture</param>
    internal VixieTextureGl(OpenGLBackend backend, uint textureId, uint width, uint height) {
        this._backend = backend;
        this._backend.GlCheckThread();
        this.TextureId = textureId;
        this.Size     = new Vector2D<int>((int)width, (int)height);
    }

    /// <summary>
    /// Generates the Texture on the GPU, Sets Parameters and Uploads the Image Data
    /// </summary>
    /// <param name="data">Image Data</param>
    /// <param name="width">Width of Image</param>
    /// <param name="height">Height of Imgae</param>
    private unsafe void Load(void* data, int width, int height) {
        this._backend.GlCheckThread();
        this.TextureId = this._backend.GenTexture();
        //Bind as we will be working on the Texture
        this._backend.BindTexture(TextureTarget.Texture2D, this.TextureId);
        //Apply Linear filtering, and make Image wrap around and repeat
        this._backend.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
        this._backend.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
        this._backend.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,     (int) GLEnum.Repeat);
        this._backend.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,                   (int) GLEnum.Repeat);
        //Upload Image Data
        this._backend.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
        //Unbind as we have finished
        this._backend.BindTexture(TextureTarget.Texture2D, 0);
        this._backend.CheckError("create tex width+height with data");

        this.GenMipmaps();
    }
    
    public override bool Mipmaps => this._mipmaps;

    /// <summary>
    /// Sets the Data of the Texture Directly
    /// </summary>
    /// <param name="data">Data to put there</param>
    /// <typeparam name="pDataType">Type of the Data</typeparam>
    /// <returns>Self, used for chaining methods</returns>
    public override unsafe VixieTexture SetData <pDataType>(ReadOnlySpan<pDataType> data) {
        this._backend.GlCheckThread();
        this.LockingBind();

        if (sizeof(pDataType) * data.Length < sizeof(Rgba32) * this.Size.X * this.Size.Y)
            throw new Exception("Data is too small!");
            
        fixed(void* d = data)
            this._backend.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint) this.Size.X, (uint) this.Size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, d);
        this._backend.CheckError("set texture data");

        this.GenMipmaps();
        this.UnlockingUnbind();

        return this;
    }
    /// <summary>
    /// Sets the Data of the Texture Directly
    /// </summary>
    /// <param name="data">Data to put there</param>
    /// <param name="rect">Rectangle of Data to Edit</param>
    /// <typeparam name="pDataType">Type of Data to put</typeparam>
    /// <returns>Self, used for chaining methods</returns>
    public override unsafe VixieTexture SetData <pDataType>(ReadOnlySpan<pDataType> data, Rectangle rect) {
        this._backend.GlCheckThread();
        this.LockingBind();

        fixed(void* d = data)
            this._backend.TexSubImage2D(TextureTarget.Texture2D, 0, rect.X, rect.Y, (uint) rect.Width, (uint) rect.Height, PixelFormat.Rgba, PixelType.UnsignedByte, d);
        this._backend.CheckError("set texture data with rect");

        this.GenMipmaps();
        this.UnlockingUnbind();

        return this;
    }
    
    public override unsafe Rgba32[] GetData() {
        Rgba32[] arr = new Rgba32[this.Width * this.Height];

        this.Bind();
        
        fixed (void* ptr = arr) {
            if (this._backend.CreationBackend == Backend.OpenGLES) {
                this._backend.gl.GetInteger(GetPName.DrawFramebufferBinding, out int currFbo);
                
                uint fbo = this._backend.GenFramebuffer(); 
                this._backend.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
                this._backend.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment
                                                    .ColorAttachment0, this.TextureId, 0);

                this._backend.gl.ReadPixels(0, 0, (uint)this.Width, (uint)this.Height, PixelFormat.Rgba, PixelType
                .UnsignedByte, ptr);

                this._backend.gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)currFbo);
                this._backend.gl.DeleteFramebuffers(1, &fbo);
            } else {
                this._backend.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        }
        this._backend.CheckError("get tex image");
        
        return arr;
    }

    /// <summary>
    /// Binds the Texture to a certain Texture Slot
    /// </summary>
    /// <param name="textureSlot">Desired Texture Slot</param>
    /// <returns>Self, used for chaining methods</returns>
    public VixieTextureGl Bind(TextureUnit textureSlot = TextureUnit.Texture0) {
        this._backend.GlCheckThread();
        if (this.Locked)
            return null;

        this._backend.CheckError("prebind");
        this._backend.ActiveTexture(textureSlot);
        this._backend.CheckError("active texture");
        this._backend.BindTexture(TextureTarget.Texture2D, this.TextureId);
        this._backend.CheckError("bind tex with tex slot");

        return this;
    }

    /// <summary>
    /// Indicates whether Object is Locked or not,
    /// This is done internally to not be able to switch Textures while a Batch is happening
    /// or really anything that would possibly get screwed over by switching Textures
    /// </summary>
    internal bool Locked = false;

    /// <summary>
    /// Binds and sets a Lock so that the Texture cannot be unbound/rebound
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    internal VixieTextureGl LockingBind() {
        this._backend.GlCheckThread();
        this.Bind();
        this.Lock();

        return this;
    }
    /// <summary>
    /// Locks the Texture so that other Textures cannot be bound/unbound/rebound
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    internal VixieTextureGl Lock() {
        this._backend.GlCheckThread();
        this.Locked = true;

        return this;
    }
    /// <summary>
    /// Unlocks the Texture, so that other Textures can be bound
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    internal VixieTextureGl Unlock() {
        this._backend.GlCheckThread();
        this.Locked = false;

        return this;
    }
    /// <summary>
    /// Uninds and unlocks the Texture so that other Textures can be bound/rebound
    /// </summary>
    /// <returns>Self, used for chaining Methods</returns>
    internal VixieTextureGl UnlockingUnbind() {
        this._backend.GlCheckThread();
        this.Unlock();
        this.Unbind();

        return this;
    }

    /// <summary>
    /// Unbinds the Texture
    /// </summary>
    /// <returns>Self, used for chaining methods</returns>
    public VixieTextureGl Unbind() {
        this._backend.GlCheckThread();
        if (this.Locked)
            return null;

        this._backend.BindTexture(TextureTarget.Texture2D, 0);
        this._backend.CheckError("unbind texture");

        return this;
    }

    private TextureFilterType _filterType = TextureFilterType.Smooth;
    public override TextureFilterType FilterType {
        get => this._filterType;
        set {
            TextureMagFilter magFilter = value == TextureFilterType.Smooth ? TextureMagFilter.Linear : TextureMagFilter.Nearest;
            TextureMinFilter minFilter =
                value == TextureFilterType.Smooth ? TextureMinFilter.Linear : TextureMinFilter.Nearest;

            if (this._mipmaps)
                minFilter = value == TextureFilterType.Smooth ? TextureMinFilter.LinearMipmapLinear
                                : TextureMinFilter.NearestMipmapLinear;
            
            this.Bind();
            
            this._backend.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            this._backend.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);

            this._filterType = value;
        }
    }
    
    private bool  _isDisposed = false;
    public  ulong BindlessHandle;

    /// <summary>
    /// Disposes the Texture and the Local Image Buffer
    /// </summary>
    public override void Dispose() {
        this._backend.GlCheckThread();

        if (this._isDisposed)
            return;

        this._isDisposed = true;

        if (this.BindlessHandle != 0) {
            this._backend.BindlessTexturingExtension.MakeTextureHandleNonResident(this.BindlessHandle);
        }
        
        this._backend.DeleteTexture(this.TextureId);
        this._backend.CheckError("dispose texture");
        GC.SuppressFinalize(this);
    }
}