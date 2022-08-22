using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using FontStashSharp;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Direct3D9;
using Color = Furball.Vixie.Backends.Shared.Color;
using Texture = Furball.Vixie.Backends.Shared.Texture;

namespace Furball.Vixie.Backends.Direct3D9; 

public unsafe class QuadRendererD3D9 : IQuadRenderer {
    private readonly IDirect3DDevice9 _device;

    private const int BATCH_COUNT = 128;
    
    private Vertex[] _vertexArray = new Vertex[BATCH_COUNT * 4];
    private ushort[] _indexArray  = new ushort[BATCH_COUNT * 6];

    private int _batchedQuads;
    
    private readonly IDirect3DIndexBuffer9  _indexBuffer;
    private readonly IDirect3DVertexBuffer9 _vertexBuffer;
    
    [StructLayout(LayoutKind.Sequential)]
    struct Vertex {
        public static VertexFormat Format = VertexFormat.PositionRhw | VertexFormat.Diffuse;
        
        Vector4 Position;
        Rgba32  Color;

        public Vertex(Vector4 position, Rgba32 color) {
            this.Position = position;
            this.Color    = color;
        }
    }
    
    public QuadRendererD3D9(IDirect3DDevice9 device) {
        this._device = device;
        
        this._vertexBuffer = device.CreateVertexBuffer(sizeof(Vertex) * this._vertexArray.Length, Usage.None, Vertex.Format, Pool.Managed);
        this._indexBuffer  = device.CreateIndexBuffer(10, Usage.None, true, Pool.Managed);
    }
    
    public void Dispose() {
        this._indexBuffer.Dispose();
        this._vertexBuffer.Dispose();
    }
    public bool IsBegun {
        get;
        set;
    }
    public void Begin() {
        this._device.VertexFormat = Vertex.Format;
        
        this._device.SetStreamSource(0, this._vertexBuffer, 0, sizeof(Vertex));
        this._device.Indices = this._indexBuffer;
    }

    private void Flush() {
        if (this._batchedQuads == 0)
            return;
        
        var dataStream = this._vertexBuffer.LockToPointer(0, sizeof(Vertex) * this._vertexArray.Length);

        fixed(void* ptr = this._vertexArray)
            Buffer.MemoryCopy(ptr, (void*)dataStream, sizeof(Vertex) * this._vertexArray.Length, sizeof(Vertex) * this._vertexArray.Length);

        this._vertexBuffer.Unlock();
        
        dataStream = this._indexBuffer.LockToPointer(0, sizeof(ushort) * this._indexArray.Length);

        fixed(void* ptr = this._indexArray)
            Buffer.MemoryCopy(ptr, (void*) dataStream, sizeof(ushort) * this._indexArray.Length, sizeof(ushort) * this._indexArray.Length);

        this._indexBuffer.Unlock();

        this._device.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, this._batchedQuads * 6, 0, this._batchedQuads * 2);

        this._batchedQuads = 0;
    }
    
    public void End() {
        this.Flush();
    }
    
    public void Draw(Texture     texture,                    Vector2 position, Vector2 scale, float rotation, Color colorOverride,
                     TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        throw new System.NotImplementedException();
    }
    
    public void Draw(Texture     texture,                    Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect,
                     TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        throw new System.NotImplementedException();
    }
    
    public void Draw(Texture texture, Vector2 position, float rotation = 0, TextureFlip flip = TextureFlip.None,
                     Vector2 rotOrigin = default) {
        throw new System.NotImplementedException();
    }
    
    public void Draw(Texture texture, Vector2 position, Vector2 scale, float rotation = 0, TextureFlip flip = TextureFlip.None,
                     Vector2 rotOrigin = default) {
        throw new System.NotImplementedException();
    }

    public void Draw(Texture     texture,                    Vector2 position, Vector2 scale, Color colorOverride, float rotation = 0,
                     TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        throw new System.NotImplementedException();
    }
    
    public void DrawString(DynamicSpriteFont font,         string  text, Vector2 position, Color color, float rotation = 0,
                           Vector2?          scale = null, Vector2 origin = default) {
        throw new System.NotImplementedException();
    }
    
    public void DrawString(DynamicSpriteFont font,         string  text, Vector2 position, System.Drawing.Color color, float rotation = 0,
                           Vector2?          scale = null, Vector2 origin = default) {
        throw new System.NotImplementedException();
    }
    
    public void DrawString(DynamicSpriteFont font,         string  text, Vector2 position, System.Drawing.Color[] colors, float rotation = 0,
                           Vector2?          scale = null, Vector2 origin = default) {
        throw new System.NotImplementedException();
    }
}