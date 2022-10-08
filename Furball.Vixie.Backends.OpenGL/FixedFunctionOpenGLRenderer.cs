using System.Numerics;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL.Legacy;
using SixLabors.ImageSharp.PixelFormats;

//Disable obsolete warning :^)
#pragma warning disable CS0618

namespace Furball.Vixie.Backends.OpenGL;

public class FixedFunctionOpenGLRenderer : Renderer {
    private readonly OpenGLBackend _backend;
    private readonly GL            _gl;

    private readonly uint _list;

    public FixedFunctionOpenGLRenderer(OpenGLBackend backend) {
        this._backend = backend;

        this._gl = backend.GetLegacyGl();

        unsafe {
            uint stride = (uint)sizeof(Vertex);

            this._gl.VertexPointer(2, VertexPointerType.Float, stride, (void*)0);
            this._gl.ColorPointer(4, GLEnum.Float, stride, (void*)sizeof(Vector2));
            this._gl.TexCoordPointer(2, TexCoordPointerType.Float, stride, (void*)(sizeof(Vector2) + sizeof(Rgba32)));

            this._gl.EnableClientState(EnableCap.VertexArray);
            this._gl.EnableClientState(EnableCap.ColorArray);
            this._gl.EnableClientState(EnableCap.TextureCoordArray);
        }

        this._list = this._gl.GenLists(1);
    }

    public override void Begin() {
        Guard.Todo("Implement FFP Begin");
    }
    public override void End() {
        Guard.Todo("Implement FFP End");

        //Start a new display list
        this._gl.NewList(this._list, ListMode.Compile);

        //TODO: Convert all the commands to OpenGL commands

        //End the display list
        this._gl.EndList();
    }
    public override MappedData Reserve(ushort vertexCount, uint indexCount) {
        Guard.Todo("Implement FFP Reserve");

        return new MappedData();
    }
    public override long GetTextureId(VixieTexture tex) {
        Guard.Todo("Implement FFP GetTextureId");

        return -1;
    }
    public override void Draw() {
        Guard.Todo("Implement FFP Draw");
    }
    protected override void DisposeInternal() {
        this._gl.DeleteLists(this._list, 1);
    }
}