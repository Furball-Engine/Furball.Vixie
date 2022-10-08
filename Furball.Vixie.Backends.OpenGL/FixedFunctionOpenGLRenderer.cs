using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Furball.Vixie.Backends.OpenGL.Abstractions;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.FontStashSharp;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL.Legacy;

//Disable obsolete warning :^)
#pragma warning disable CS0612
#pragma warning disable CS0618

namespace Furball.Vixie.Backends.OpenGL;

public class FixedFunctionOpenGLRenderer : Renderer {
    private readonly OpenGLBackend _backend;
    private readonly GL            _gl;

    private readonly uint _list;

    private bool   _isBegun;
    
    private List<MappedData> _mappedDataList = new List<MappedData>(1);

    public FixedFunctionOpenGLRenderer(OpenGLBackend backend) {
        this._backend = backend;

        this._gl = backend.GetLegacyGl();

        this._list = this._gl.GenLists(1);
        this._backend.CheckError("Failed to generate list");

        this.FontRenderer = new VixieFontStashRenderer(backend, this);
    }

    public override void Begin() {
        this._isBegun = true;
    }
    public override unsafe void End() {
        Guard.Assert(this._isBegun, "Renderer is not begun!");

        //Start a new display list
        this._gl.NewList(this._list, ListMode.Compile);
        this._gl.Begin(PrimitiveType.Triangles);

        // uint stride = (uint)sizeof(Vertex);

        this._gl.VertexPointer(2, VertexPointerType.Float, 0, (void*)0);
        this._gl.ColorPointer(4, GLEnum.Float, 0, (void*)0);
        this._gl.TexCoordPointer(2, TexCoordPointerType.Float, 0, (void*)0);
        this._backend.CheckError("pointer defs");
        
        this._gl.EnableClientState(EnableCap.VertexArray);
        this._gl.EnableClientState(EnableCap.ColorArray);
        this._gl.EnableClientState(EnableCap.TextureCoordArray);
        this._backend.CheckError("enable client state");

        long lastMappedTex = -1;
        foreach (MappedData mappedData in this._mappedDataList) {
            for (int i = 0; i < mappedData.IndexCount; i++) {
                ushort index = mappedData.IndexPtr[i];

                Vertex vertex = mappedData.VertexPtr[index];

                //Map the new texture if it changed
                if (vertex.TexId != lastMappedTex) {
                    this._gl.End();
                    this._gl.BindTexture(TextureTarget.Texture2D, (uint)vertex.TexId);
                    this._gl.Begin(PrimitiveType.Triangles);

                    lastMappedTex = vertex.TexId;
                }
                
                this._gl.Vertex2((float*)&vertex.Position);
                this._gl.Color4((float*)&vertex.Color);
                this._gl.TexCoord2((float*)&vertex.TextureCoordinate);
            }
        }
        this._backend.CheckError("rendering");

        this._gl.End();
        //End the display list
        this._gl.EndList();
        this._backend.CheckError("end list");
        
        foreach (MappedData mappedData in this._mappedDataList) {
            Marshal.FreeHGlobal((IntPtr)mappedData.VertexPtr);
            Marshal.FreeHGlobal((IntPtr)mappedData.IndexPtr);
        }
        this._backend.CheckError("freeing mapped data");
        
        this._mappedDataList.Clear();

        this._isBegun = false;
    }

    public override unsafe MappedData Reserve(ushort vertexCount, uint indexCount) {
        Vertex* vertexPtr = (Vertex*)Marshal.AllocHGlobal(sizeof(Vertex) * vertexCount);
        ushort* indexPtr  = (ushort*)Marshal.AllocHGlobal((IntPtr)(sizeof(ushort) * indexCount));

        MappedData data = new MappedData(vertexPtr, indexPtr, vertexCount, indexCount, 0);

        this._mappedDataList.Add(data);
        
        return data;
    }
    public override long GetTextureId(VixieTexture tex) {
        if (tex is not VixieTextureGl glTex)
            throw new Exception($"You can only pass textures of type {typeof(VixieTextureGl)} into this function!");

        return glTex.TextureId;
    }
    public override void Draw() {
        //Assert we arent still collection data
        Guard.Assert(!this._isBegun);
        
        //Call the list to draw to the screen
        this._gl.CallList(this._list);
        this._backend.CheckError("call list");
    }
    protected override void DisposeInternal() {
        this._gl.DeleteLists(this._list, 1);
    }
}