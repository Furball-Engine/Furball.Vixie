using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Furball.Vixie.Helpers;
using Kettu;
using Silk.NET.OpenGLES;

namespace Furball.Vixie.Graphics.Backends.OpenGL.Abstractions {
    /// <summary>
    /// A Shader, a Program run on the GPU
    /// </summary>
    public class ShaderGL : IDisposable {
        /// <summary>
        /// Currently Bound Shader
        /// </summary>
        internal static ShaderGL CurrentlyBound;
        /// <summary>
        /// Getter to check whether this Shader is bound
        /// </summary>
        public bool Bound => CurrentlyBound == this;
        /// <summary>
        /// OpenGL api, used to not have to do Global.Gl.function everytime, saves time and makes code shorter
        /// </summary>
        private GL gl;
        /// <summary>
        /// Program ID, used by OpenGL to distingluish different Programs
        /// </summary>
        internal uint ProgramId;
        /// <summary>
        /// List of intermediate Shaders that can be deleted later.
        /// </summary>
        private List<uint> _shaders;
        /// <summary>
        /// A Dictionary for caching uniform locations, so we don't have to re-get the locations for uniforms everytime a Uniform is set
        /// </summary>
        private Dictionary<string, int> _uniformLocationCache;

        /// <summary>
        /// Creates a unlinked Shader with no source code
        /// </summary>
        public ShaderGL(OpenGLESBackend backend) {
            OpenGLHelper.CheckThread();
            
            this.gl = backend.GetGlApi();

            this._shaders              = new List<uint>();
            this._uniformLocationCache = new Dictionary<string, int>();

            this.ProgramId = this.gl.CreateProgram();
            OpenGLHelper.CheckError();
        }

        /// <summary>
        /// Attaches and Compiles a Shader Source
        /// </summary>
        /// <param name="type">What type of Shader is it?</param>
        /// <param name="source">Shader source code</param>
        /// <returns>Self, used for Chaining methods</returns>
        /// <exception cref="Exception">Shader Compilation Failure</exception>
        public ShaderGL AttachShader(ShaderType type, string source) {
            OpenGLHelper.CheckThread();
            
            uint shaderId = this.gl.CreateShader(type);

            this.gl.ShaderSource(shaderId, source);
            this.gl.CompileShader(shaderId);
            OpenGLHelper.CheckError();

            string infoLog = this.gl.GetShaderInfoLog(shaderId);

            if (!string.IsNullOrEmpty(infoLog))
                throw new Exception($"Failed to Compile shader of type {type}, Error Message: {infoLog}");

            this.gl.AttachShader(this.ProgramId, shaderId);
            OpenGLHelper.CheckError();

            this._shaders.Add(shaderId);

            return this;
        }
        /// <summary>
        /// Links the Shader together and deletes the intermediate Shaders
        /// </summary>
        /// <returns>Self, used for Chaining methods</returns>
        /// <exception cref="Exception"></exception>
        public ShaderGL Link() {
            OpenGLHelper.CheckThread();
            
            //Link Program and get Error incase something failed
            this.gl.LinkProgram(this.ProgramId);
            this.gl.GetProgram(this.ProgramId, ProgramPropertyARB.LinkStatus, out int linkStatus);
            OpenGLHelper.CheckError();

            if (linkStatus == 0)
                throw new Exception($"Failed to Link Program, Error Message: { this.gl.GetProgramInfoLog(this.ProgramId) }");

            //Delete Intermediate Shaders
            for(int i = 0; i != this._shaders.Count; i++)
                this.gl.DeleteShader(this._shaders[i]);
            OpenGLHelper.CheckError();

            return this;
        }
        /// <summary>
        /// Selects this Shader
        /// </summary>
        public ShaderGL Bind() {
            OpenGLHelper.CheckThread();
            
            if (this.Locked)
                return null;

            this.gl.UseProgram(this.ProgramId);
            OpenGLHelper.CheckError();

            CurrentlyBound = this;

            return this;
        }

        /// <summary>
        /// Indicates whether Object is Locked or not,
        /// This is done internally to not be able to switch Shaders while a Batch is happening
        /// or really anything that would possibly get screwed over by switching Shaders
        /// </summary>
        internal bool Locked = false;

        /// <summary>
        /// Binds and sets a Lock so that the Shader cannot be unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal ShaderGL LockingBind() {
            this.Bind();
            this.Lock();

            return this;
        }

        /// <summary>
        /// Locks the Shader so that other Shaders cannot be bound/unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal ShaderGL Lock() {
            this.Locked = true;

            return this;
        }

        /// <summary>
        /// Unlocks the Shader, so that other Shaders can be bound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal ShaderGL Unlock() {
            this.Locked = false;

            return this;
        }
        /// <summary>
        /// Uninds and unlocks the Shader so that other Shaders can be bound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal ShaderGL UnlockingUnbind() {
            this.Unlock();
            this.Unbind();

            return this;
        }

        /// <summary>
        /// Gets the location of a specific uniform
        /// </summary>
        /// <param name="uniformName">The name of the uniform</param>
        /// <returns>The location</returns>
        internal int GetUniformLocation(string uniformName) {
            OpenGLHelper.CheckThread();
            
            //If cache missed, get from OpenGL and store in cache
            if (!this._uniformLocationCache.TryGetValue(uniformName, out int location)) {
                //Get the location from the program
                location = this.gl.GetUniformLocation(this.ProgramId, uniformName);
                OpenGLHelper.CheckError();
                
                if(location != -1)
                    this._uniformLocationCache.Add(uniformName, location);
            }

            if (location == -1) {
                Logger.Log($"[OpenGL Warning] Uniform Location for {uniformName} seems to not exist. It may have been optimized out or you simply misspelled the Uniform name", LoggerLevelDebugMessageCallback.InstanceHigh);
#if DEBUG
                //Break here 
                Debugger.Break();
#endif
            }

            return location;
        }

        public unsafe ShaderGL SetUniform(string uniformName, Matrix4x4 matrix) {
            OpenGLHelper.CheckThread();
            
            this.gl.UniformMatrix4(this.GetUniformLocation(uniformName), 1, false, (float*) &matrix);
            OpenGLHelper.CheckError();
            
            //Return this for chaining
            return this;
        }
        
        public ShaderGL SetUniform(string uniformName, float f) {
            OpenGLHelper.CheckThread();
            
            this.gl.Uniform1(this.GetUniformLocation(uniformName), f);
            OpenGLHelper.CheckError();
            
            //Return this for chaining
            return this;
        }
        
        public ShaderGL SetUniform(string uniformName, float f, float f2) {
            OpenGLHelper.CheckThread();
            
            this.gl.Uniform2(this.GetUniformLocation(uniformName), f, f2);
            OpenGLHelper.CheckError();
            
            //Return this for chaining
            return this;
        }
        
        public ShaderGL SetUniform(string uniformName, int i) {
            OpenGLHelper.CheckThread();
            
            this.gl.Uniform1(this.GetUniformLocation(uniformName), i);
            OpenGLHelper.CheckError();
            
            //Return this for chaining
            return this;
        }
        /// <summary>
        /// Unbinds all Shaders
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        public ShaderGL Unbind() {
            OpenGLHelper.CheckThread();
            
            if (this.Locked)
                return null;

            this.gl.UseProgram(0);
            OpenGLHelper.CheckError();

            CurrentlyBound = null;

            return this;
        }

        /// <summary>
        /// Cleans up the Shader
        /// </summary>
        public void Dispose() {
            OpenGLHelper.CheckThread();
            
            if (this.Bound)
                this.UnlockingUnbind();

            try {
                this.gl.DeleteProgram(this.ProgramId);
            }
            catch {

            }
            OpenGLHelper.CheckError();
        }
        
        /// <summary>
        /// Binds a uniform to a specific texture unit
        /// </summary>
        /// <param name="uniform"></param>
        /// <param name="unit"></param>
        public void BindUniformToTexUnit(string uniform, int unit) {
            OpenGLHelper.CheckThread();

            int location = this.GetUniformLocation(uniform);

            this.gl.Uniform1(location, unit);
            
            OpenGLHelper.CheckError();
        }
    }
}
