using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Furball.Vixie.Helpers;
using Kettu;
using Silk.NET.OpenGLES;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle=System.Drawing.Rectangle;

namespace Furball.Vixie.Graphics {
    public class Texture : IDisposable {
        /// <summary>
        /// All the Currently Bound Textures
        /// </summary>
        internal static Dictionary<TextureUnit, uint> BoundTextures = new() {
            { TextureUnit.Texture0,  0 },
            { TextureUnit.Texture1,  0 },
            { TextureUnit.Texture2,  0 },
            { TextureUnit.Texture3,  0 },
            { TextureUnit.Texture4,  0 },
            { TextureUnit.Texture5,  0 },
            { TextureUnit.Texture6,  0 },
            { TextureUnit.Texture7,  0 },
            { TextureUnit.Texture8,  0 },
            { TextureUnit.Texture9,  0 },
            { TextureUnit.Texture10, 0 },
            { TextureUnit.Texture11, 0 },
            { TextureUnit.Texture12, 0 },
            { TextureUnit.Texture13, 0 },
            { TextureUnit.Texture14, 0 },
            { TextureUnit.Texture15, 0 },
            { TextureUnit.Texture16, 0 },
            { TextureUnit.Texture17, 0 },
            { TextureUnit.Texture18, 0 },
            { TextureUnit.Texture19, 0 },
            { TextureUnit.Texture20, 0 },
            { TextureUnit.Texture21, 0 },
            { TextureUnit.Texture22, 0 },
            { TextureUnit.Texture23, 0 },
            { TextureUnit.Texture24, 0 },
            { TextureUnit.Texture25, 0 },
            { TextureUnit.Texture26, 0 },
            { TextureUnit.Texture27, 0 },
            { TextureUnit.Texture28, 0 },
            { TextureUnit.Texture29, 0 },
            { TextureUnit.Texture30, 0 },
            { TextureUnit.Texture31, 0 },
        };

        public bool Bound {
            get {
                uint texFound;

                if (BoundTextures.TryGetValue(this.BoundAt, out texFound)) {
                    return texFound == this.TextureId;
                }

                return false;
            }
        }

        internal TextureUnit BoundAt;

        /// <summary>
        /// OpenGL API, used to not write Global.Gl everytime
        /// </summary>
        private GL            gl;
        /// <summary>
        /// Unique ID which identifies this Texture
        /// </summary>
        internal uint          TextureId;
        /// <summary>
        /// Local Image, possibly useful to Sample on the CPU Side if necessary
        /// </summary>
        private Image<Rgba32> _localBuffer;
        /// <summary>
        /// Size of the Texture
        /// </summary>
        public Vector2 Size { get; private set; }

        public int Width  => (int)this.Size.X;
        public int Height => (int)this.Size.Y;

        /// <summary>
        /// Creates a Texture from a File
        /// </summary>
        /// <param name="filepath">Path to an Image</param>
        public unsafe Texture(string filepath) {
            this.gl = Global.Gl;

            Image<Rgba32> image = (Image<Rgba32>)Image.Load(filepath);

            this._localBuffer = image;

            int width = image.Width;
            int height = image.Height;

            this.Load(image);

            this.Size = new Vector2(width, height);
        }
        /// <summary>
        /// Creates a Texture from a byte array which contains Image Data
        /// </summary>
        /// <param name="imageData">Image Data</param>
        public unsafe Texture(byte[] imageData, bool qoi = false) {
            this.gl = Global.Gl;

            Image<Rgba32> image;
            if(qoi) {
                double start = Stopwatch.GetTimestamp();
                
                (Rgba32[] pixels, QoiLoader.QoiHeader header) data  = QoiLoader.Load(imageData);

                Logger.Log($"Loading QOI image took {(Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency * 1000}ms", LoggerLevelImageLoader.Instance);
                
                image = Image.LoadPixelData(data.pixels, (int)data.header.Width, (int)data.header.Height);
            } else {
                double start = Stopwatch.GetTimestamp();

                image = Image.Load<Rgba32>(imageData);
                
                Logger.Log($"Loading PNG image took {(Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency * 1000}ms", LoggerLevelImageLoader.Instance);
            }

            this._localBuffer = image;

            int width = image.Width;
            int height = image.Height;

            this.Load(image);

            this.Size = new Vector2(width, height);
        }
        /// <summary>
        /// Creates a Texture with a single White Pixel
        /// </summary>
        public unsafe Texture() {
            OpenGLHelper.CheckThread();
            
            this.gl = Global.Gl;

            this.TextureId = this.gl.GenTexture();
            //Bind as we will be working on the Texture
            this.gl.BindTexture(TextureTarget.Texture2D, this.TextureId);
            //Apply Linear filtering, and make Image wrap around and repeat
            this.gl.TexParameter(GLEnum.Texture2D,        TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
            this.gl.TexParameter(GLEnum.Texture2D,        TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            this.gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapS,                   (int) GLEnum.Repeat);
            this.gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapT,                   (int) GLEnum.Repeat);
            //White color
            uint color = 0xffffffff;
            //Upload Image Data
            this.gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba8, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, &color);
            //Unbind as we have finished
            this.gl.BindTexture(TextureTarget.Texture2D, 0);
            OpenGLHelper.CheckError();

            this.Size = new Vector2(1, 1);
        }
        /// <summary>
        /// Creates a Empty texture given a width and height
        /// </summary>
        /// <param name="width">Desired Width</param>
        /// <param name="height">Desired Height</param>
        public unsafe Texture(uint width, uint height) {
            OpenGLHelper.CheckThread();
            
            this.gl = Global.Gl;

            this.TextureId = this.gl.GenTexture();
            //Bind as we will be working on the Texture
            this.gl.BindTexture(TextureTarget.Texture2D, this.TextureId);
            //Apply Linear filtering, and make Image wrap around and repeat
            this.gl.TexParameter(GLEnum.Texture2D,        TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
            this.gl.TexParameter(GLEnum.Texture2D,        TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            this.gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapS,                   (int) GLEnum.Repeat);
            this.gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapT,                   (int) GLEnum.Repeat);
            //Upload Image Data
            this.gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba8, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            //Unbind as we have finished
            this.gl.BindTexture(TextureTarget.Texture2D, 0);
            OpenGLHelper.CheckError();

            this.Size = new Vector2(width, height);
        }
        /// <summary>
        /// Creates a Texture from a Stream which Contains Image Data
        /// </summary>
        /// <param name="stream">Image Data Stream</param>
        public unsafe Texture(Stream stream) {
            this.gl = Global.Gl;

            Image<Rgba32> image = Image.Load<Rgba32>(stream);

            this._localBuffer = image;

            int width = image.Width;
            int height = image.Height;

            this.Load(image);

            this.Size = new Vector2(width, height);
        }

        private unsafe void Load(Image<Rgba32> image) {
            this.Load(null, image.Width, image.Height);
            this.Bind();
            image.ProcessPixelRows(accessor =>
            {
                for (int i = 0; i < accessor.Height; i++) {
                    fixed(void* ptr = &accessor.GetRowSpan(i).GetPinnableReference())
                        this.gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, i, (uint)accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
                }
            });
            this.Unbind();
        }
        
        /// <summary>
        /// Creates a Texture class using a already Generated OpenGL Texture
        /// </summary>
        /// <param name="textureId">OpenGL Texture ID</param>
        /// <param name="width">Width of the Texture</param>
        /// <param name="height">Height of the Texture</param>
        internal Texture(uint textureId, uint width, uint height) {
            this.gl = Global.Gl;

            this.TextureId = textureId;
            this.Size       = new Vector2(width, height);
        }

        /// <summary>
        /// Generates the Texture on the GPU, Sets Parameters and Uploads the Image Data
        /// </summary>
        /// <param name="data">Image Data</param>
        /// <param name="width">Width of Image</param>
        /// <param name="height">Height of Imgae</param>
        private unsafe void Load(void* data, int width, int height) {
            this.TextureId = this.gl.GenTexture();
            //Bind as we will be working on the Texture
            this.gl.BindTexture(TextureTarget.Texture2D, this.TextureId);
            //Apply Linear filtering, and make Image wrap around and repeat
            this.gl.TexParameter(GLEnum.Texture2D,        TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
            this.gl.TexParameter(GLEnum.Texture2D,        TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            this.gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapS,                   (int) GLEnum.Repeat);
            this.gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapT,                   (int) GLEnum.Repeat);
            //Upload Image Data
            this.gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            //Unbind as we have finished
            this.gl.BindTexture(TextureTarget.Texture2D, 0);
            OpenGLHelper.CheckError();
        }
        /// <summary>
        /// Sets the Data of the Texture Directly
        /// </summary>
        /// <param name="level">Level of the texture</param>
        /// <param name="data">Data to put there</param>
        /// <typeparam name="pDataType">Type of the Data</typeparam>
        /// <returns>Self, used for chaining methods</returns>
        public unsafe Texture SetData<pDataType>(int level, pDataType[] data) where pDataType : unmanaged {
            this.LockingBind();

            fixed(void* d = data)
                this.gl.TexImage2D(TextureTarget.Texture2D, level, InternalFormat.Rgba, (uint) this.Size.X, (uint) this.Size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, d);

            this.gl.Finish();
            OpenGLHelper.CheckError();

            this.UnlockingUnbind();

            return this;
        }
        /// <summary>
        /// Sets the Data of the Texture Directly
        /// </summary>
        /// <param name="level">Level of the Texture</param>
        /// <param name="rect">Rectangle of Data to Edit</param>
        /// <param name="data">Data to put there</param>
        /// <typeparam name="pDataType">Type of Data to put</typeparam>
        /// <returns>Self, used for chaining methods</returns>
        public unsafe Texture SetData<pDataType>(int level, Rectangle rect, pDataType[] data) where pDataType : unmanaged {
            this.LockingBind();

            fixed(void* d = data)
                this.gl.TexSubImage2D(TextureTarget.Texture2D, level, rect.X, rect.Y, (uint) rect.Width, (uint) rect.Height, PixelFormat.Rgba, PixelType.UnsignedByte, d);
            OpenGLHelper.CheckError();

            this.UnlockingUnbind();

            return this;
        }

        /// <summary>
        /// Binds the Texture to a certain Texture Slot
        /// </summary>
        /// <param name="textureSlot">Desired Texture Slot</param>
        /// <returns>Self, used for chaining methods</returns>
        public Texture Bind(TextureUnit textureSlot = TextureUnit.Texture0) {
            if (this.Locked)
                return null;

            this.gl.ActiveTexture(textureSlot);
            this.gl.BindTexture(TextureTarget.Texture2D, this.TextureId);
            OpenGLHelper.CheckError();

            BoundTextures[textureSlot] = this.TextureId;
            this.BoundAt               = textureSlot;

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
        internal Texture LockingBind() {
            this.Bind();
            this.Lock();

            return this;
        }
        /// <summary>
        /// Locks the Texture so that other Textures cannot be bound/unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal Texture Lock() {
            this.Locked = true;

            return this;
        }
        /// <summary>
        /// Unlocks the Texture, so that other Textures can be bound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal Texture Unlock() {
            this.Locked = false;

            return this;
        }
        /// <summary>
        /// Uninds and unlocks the Texture so that other Textures can be bound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal Texture UnlockingUnbind() {
            this.Unlock();
            this.Unbind();

            return this;
        }

        /// <summary>
        /// Unbinds the Texture
        /// </summary>
        /// <returns>Self, used for chaining methods</returns>
        public Texture Unbind() {
            if (this.Locked)
                return null;

            this.gl.ActiveTexture(this.BoundAt);
            this.gl.BindTexture(TextureTarget.Texture2D, 0);
            OpenGLHelper.CheckError();

            BoundTextures[this.BoundAt] = 0;

            return this;
        }
        /// <summary>
        /// Gets the OpenGL texture ID
        /// </summary>
        /// <returns>Texture ID</returns>
        internal uint GetTextureId() => this.TextureId;

        /// <summary>
        /// Disposes the Texture and the Local Image Buffer
        /// </summary>
        public void Dispose() {
            if (this.Bound)
                this.UnlockingUnbind();

            try {
                this.gl.DeleteTexture(this.TextureId);
                this._localBuffer.Dispose();
            }
            catch {

            }
            OpenGLHelper.CheckError();
        }
    }
}
