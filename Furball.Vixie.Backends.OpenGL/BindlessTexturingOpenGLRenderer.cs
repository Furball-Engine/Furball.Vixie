using Furball.Vixie.Backends.OpenGL.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;

namespace Furball.Vixie.Backends.OpenGL;

// ReSharper disable once InconsistentNaming
public class BindlessTexturingOpenGLRenderer : VixieRenderer {
    private readonly OpenGLBackend _backend;

    private bool _begun;

    private class BufferData {
        public VertexArrayObjectGl Vao;
        public BufferObjectGl      Vtx;
        public BufferObjectGl      Idx;
    }
    
    public BindlessTexturingOpenGLRenderer(OpenGLBackend backend) {
        this._backend = backend;
    }

    public override void Begin() {
        Guard.Assert(!this._begun, "!this._begun");
        this._begun = true;
    }

    public override void End() {
        Guard.Assert(this._begun, "this._begun");
        this._begun = false;
    }

    private void ReserveTexture(VixieTextureGl tex) {
        // this._backend.BindlessTexturingExtension.
    }
    
    public override MappedData Reserve(ushort vertexCount, uint indexCount, VixieTexture tex) {
        throw new System.NotImplementedException();
    }

    public override void Draw() {
        throw new System.NotImplementedException();
    }

    protected override void DisposeInternal() {
        throw new System.NotImplementedException();
    }
}