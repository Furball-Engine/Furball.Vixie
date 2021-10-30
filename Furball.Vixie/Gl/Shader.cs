using System;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Gl {
    public class Shader : IDisposable {
        private GL gl;

        private uint       _programId;
        private List<uint> _shaders;

        public Shader() {
            this.gl       = Global.Gl;
            this._shaders = new List<uint>();

            this._programId = gl.CreateProgram();
        }

        public Shader AttachShader(ShaderType type, string source) {
            uint shaderId = gl.CreateShader(type);

            gl.ShaderSource(shaderId, source);
            gl.CompileShader(shaderId);

            string infoLog = gl.GetShaderInfoLog(shaderId);

            if (!string.IsNullOrEmpty(infoLog))
                throw new Exception($"Failed to Compile shader of type {type}, Error Message: {infoLog}");

            gl.AttachShader(this._programId, shaderId);

            this._shaders.Add(shaderId);

            return this;
        }

        public Shader Link() {
            gl.LinkProgram(this._programId);
            gl.GetProgram(this._programId, ProgramPropertyARB.LinkStatus, out int linkStatus);

            if (linkStatus == 0)
                throw new Exception($"Failed to Link Program, Error Message: { gl.GetProgramInfoLog(this._programId) }");

            for(int i = 0; i != this._shaders.Count; i++)
                gl.DeleteShader(this._shaders[i]);

            return this;
        }

        public void Bind() {
            gl.UseProgram(this._programId);
        }

        public void Dispose() {
            gl.DeleteProgram(this._programId);
        }
    }
}
