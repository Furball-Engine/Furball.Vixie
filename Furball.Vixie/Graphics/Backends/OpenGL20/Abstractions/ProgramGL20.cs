using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using Silk.NET.OpenGL.Legacy;

namespace Furball.Vixie.Graphics.Backends.OpenGL20.Abstractions {
    public class ProgramGL20 : IDisposable {
        private readonly OpenGL20Backend _backend;
        private readonly GL              gl;

        internal uint Program;
        internal uint VertexShader;
        internal uint FragmentShader;
        
        public ProgramGL20(OpenGL20Backend backend, string vertexSource, string fragmentSource) {
            this._backend = backend;

            this.gl = this._backend.GetOpenGL();

            this.VertexShader = this.gl.CreateShader(ShaderType.VertexShader);
            this.gl.ShaderSource(this.VertexShader, vertexSource);
            this.gl.CompileShader(this.VertexShader);
            this._backend.CheckError();
            
            
            string infoLog = this.gl.GetShaderInfoLog(this.VertexShader);

            if (!string.IsNullOrEmpty(infoLog))
                throw new Exception($"Failed to Compile shader of type VertexShader, Error Message: {infoLog}");
            
            this.FragmentShader = this.gl.CreateShader(ShaderType.FragmentShader);
            this.gl.ShaderSource(this.FragmentShader, fragmentSource);
            this.gl.CompileShader(this.FragmentShader);
            this._backend.CheckError();

            infoLog = this.gl.GetShaderInfoLog(this.FragmentShader);

            if (!string.IsNullOrEmpty(infoLog))
                throw new Exception($"Failed to Compile shader of type FragmentShader, Error Message: {infoLog}");

            this.Program = this.gl.CreateProgram();
            
            this.gl.AttachShader(this.Program, this.VertexShader);
            this.gl.AttachShader(this.Program, this.FragmentShader);
            this._backend.CheckError();
            
            this.gl.LinkProgram(this.Program);
            this._backend.CheckError();
            
            this.gl.GetProgram(this.Program, ProgramPropertyARB.LinkStatus, out int linkStatus);
            
            if (linkStatus == 0)
                throw new Exception($"Failed to Link Program, Error Message: { this.gl.GetProgramInfoLog(this.Program) }");
        }

        public void Bind() {
            this.gl.UseProgram(this.Program);
            this._backend.CheckError();
        }

        private Dictionary<string, int> _uniforms = new();

        public int GetUniformLocation(string uniform) {
            this.Bind();

            if (this._uniforms.TryGetValue(uniform, out int location))
                return location;
            
            location = this.gl.GetUniformLocation(this.Program, uniform);
            this._backend.CheckError();

            if (location == -1)
                throw new Exception($"Unable to find uniform {uniform}!");

            this._uniforms[uniform] = location;
            
            return location;
        }
        public void Unbind() {
            this.gl.UseProgram(0);
        }

        private bool isDisposed = false;
        public void Dispose() {
            if (this.isDisposed) return;
            
            this.gl.DeleteShader(this.VertexShader);
            this.gl.DeleteShader(this.FragmentShader);
            this.gl.DeleteProgram(this.Program);

            this.isDisposed = true;
        }
        ~ProgramGL20() {
            DisposeQueue.Enqueue(this);
        }
    }
}
