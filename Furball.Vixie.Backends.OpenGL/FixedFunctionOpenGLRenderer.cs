using System;
using System.Collections.Generic;
using Furball.Vixie.Backends.OpenGL.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL.Legacy;

//Disable obsolete warning :^)
#pragma warning disable CS0612
#pragma warning disable CS0618

namespace Furball.Vixie.Backends.OpenGL;

public class FixedFunctionOpenGlVixieRenderer : VixieRenderer {
    private readonly OpenGLBackend _backend;
    private readonly GL            _gl;

    private readonly uint _list;

    private bool   _isBegun;
    
    private List<MappedData> _mappedDataList = new List<MappedData>(1);
    private CullFace         _cullFace;

    public FixedFunctionOpenGlVixieRenderer(OpenGLBackend backend) {
        this._backend = backend;

        this._gl = backend.GetLegacyGl();

        this._list = this._gl.GenLists(1);
        this._backend.CheckError("Failed to generate list");
    }

    public override void Begin(CullFace cullFace = CullFace.CCW) {
        this._isBegun  = true;
        this._cullFace = cullFace;
    }
    public override unsafe void End() {
        Guard.Assert(this._isBegun, "Renderer is not begun!");

        //Start a new display list
        this._gl.NewList(this._list, ListMode.Compile);
        this._gl.Begin(PrimitiveType.Triangles);
        
        long lastMappedTex = -1;
        foreach (MappedData mappedData in this._mappedDataList) {
            for (int i = 0; i < mappedData.IndexCount; i++) {
                ushort index = mappedData.IndexPtr[i];

                Vertex vertex = mappedData.VertexPtr[index];

                //Map the new texture if it changed, but only on the start of a triangle
                if (vertex.TexId != lastMappedTex && (i & 3) == 0) {
                    this._gl.End();
                    this._gl.BindTexture(TextureTarget.Texture2D, (uint)vertex.TexId);
                    this._gl.Begin(PrimitiveType.Triangles);
                
                    lastMappedTex = vertex.TexId;
                }
                
                this._gl.Color4(vertex.Color.Rf, vertex.Color.Gf, vertex.Color.Bf, vertex.Color.Af);
                this._gl.TexCoord2((float*)&vertex.TextureCoordinate);
                this._gl.Vertex2((float*)&vertex.Position);
            }
        }
        this._backend.CheckError("rendering");

        this._gl.End();
        //End the display list
        this._gl.EndList();
        this._backend.CheckError("end list");
        
        foreach (MappedData mappedData in this._mappedDataList) {
            SilkMarshal.Free((IntPtr)mappedData.VertexPtr);
            SilkMarshal.Free((IntPtr)mappedData.IndexPtr);
        }
        this._backend.CheckError("freeing mapped data");
        
        this._mappedDataList.Clear();

        this._isBegun = false;
    }

    public override unsafe MappedData Reserve(ushort vertexCount, uint indexCount, VixieTexture tex) {
        Vertex* vertexPtr = (Vertex*)SilkMarshal.Allocate(sizeof(Vertex) * vertexCount);
        ushort* indexPtr  = (ushort*)SilkMarshal.Allocate((int)(sizeof(ushort) * indexCount));

        MappedData data = new MappedData(vertexPtr, indexPtr, vertexCount, indexCount, 0, this.GetTextureId(tex));

        this._mappedDataList.Add(data);
        
        return data;
    }
    private long GetTextureId(VixieTexture tex) {
        if (tex is not VixieTextureGl glTex)
            throw new Exception($"You can only pass textures of type {typeof(VixieTextureGl)} into this function!");

        return glTex.TextureId;
    }
    public override void Draw() {
        //Assert we arent still collection data
        Guard.Assert(!this._isBegun);

        this._backend.SetCullMode(this._cullFace);
        
        //Call the list to draw to the screen
        this._gl.CallList(this._list);
        this._backend.CheckError("call list");
    }
    protected override void DisposeInternal() {
        this._gl.DeleteLists(this._list, 1);
    }
}