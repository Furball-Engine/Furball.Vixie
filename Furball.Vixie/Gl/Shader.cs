using System;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Gl {
    /// <summary>
    /// A Shader, a Program run on the GPU
    /// </summary>
    public class Shader : IDisposable {
        /// <summary>
        /// OpenGL api, used to not have to do Global.Gl.function everytime, saves time and makes code shorter
        /// </summary>
        private GL gl;

        /// <summary>
        /// Program ID, used by OpenGL to distingluish different Programs
        /// </summary>
        private uint       _programId;
        /// <summary>
        /// List of intermediate Shaders that can be deleted later.
        /// </summary>
        private List<uint> _shaders;

        /// <summary>
        /// Creates a unlinked Shader with no source code
        /// </summary>
        public Shader() {
            this.gl       = Global.Gl;
            this._shaders = new List<uint>();

            this._programId = gl.CreateProgram();
        }
        /// <summary>
        /// Attaches and Compiles a Shader Source
        /// </summary>
        /// <param name="type">What type of Shader is it?</param>
        /// <param name="source">Shader source code</param>
        /// <returns>Self, used for Chaining methods</returns>
        /// <exception cref="Exception">Shader Compilation Failure</exception>
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
        /// <summary>
        /// Links the Shader together and deletes the intermediate Shaders
        /// </summary>
        /// <returns>Self, used for Chaining methods</returns>
        /// <exception cref="Exception"></exception>
        public Shader Link() {
            gl.LinkProgram(this._programId);
            gl.GetProgram(this._programId, ProgramPropertyARB.LinkStatus, out int linkStatus);

            if (linkStatus == 0)
                throw new Exception($"Failed to Link Program, Error Message: { gl.GetProgramInfoLog(this._programId) }");

            for(int i = 0; i != this._shaders.Count; i++)
                gl.DeleteShader(this._shaders[i]);

            return this;
        }
        /// <summary>
        /// Selects this Shader
        /// </summary>
        public void Bind() {
            gl.UseProgram(this._programId);
        }
        /// <summary>
        /// Cleans up the Shader
        /// </summary>
        public void Dispose() {
            gl.DeleteProgram(this._programId);
        }
    }
}
