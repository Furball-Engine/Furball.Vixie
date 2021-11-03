using System;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Gl {

    public enum UniformType {
        GlFloat,
        GlInt,
        GlUint,
        GlMat4f,
    }

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
        /// A Dictionary for caching uniform locations, so we don't have to re-get the locations for uniforms everytime a Uniform is set
        /// </summary>
        private Dictionary<string, int> _uniformLocationCache;

        /// <summary>
        /// Creates a unlinked Shader with no source code
        /// </summary>
        public Shader() {
            this.gl = Global.Gl;

            this._shaders              = new List<uint>();
            this._uniformLocationCache = new Dictionary<string, int>();

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
            //Link Program and get Error incase something failed
            gl.LinkProgram(this._programId);
            gl.GetProgram(this._programId, ProgramPropertyARB.LinkStatus, out int linkStatus);

            if (linkStatus == 0)
                throw new Exception($"Failed to Link Program, Error Message: { gl.GetProgramInfoLog(this._programId) }");

            //Delete Intermediate Shaders
            for(int i = 0; i != this._shaders.Count; i++)
                gl.DeleteShader(this._shaders[i]);

            return this;
        }
        /// <summary>
        /// Selects this Shader
        /// </summary>
        public Shader Bind() {
            if (this._locked)
                return null;

            gl.UseProgram(this._programId);

            return this;
        }

        private bool _locked = false;

        /// <summary>
        /// Binds and sets a Lock so that the Shader cannot be unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal Shader LockingBind() {
            this.Bind();
            this.Lock();

            return this;
        }

        /// <summary>
        /// Locks the Shader so that other Shaders cannot be bound/unbound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal Shader Lock() {
            this._locked = true;

            return this;
        }

        /// <summary>
        /// Unlocks the Shader, so that other Shaders can be bound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal Shader Unlock() {
            this._locked = false;

            return this;
        }
        /// <summary>
        /// Uninds and unlocks the Shader so that other Shaders can be bound/rebound
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        internal Shader UnlockingUnbind() {
            this.Unlock();
            this.Unbind();

            return this;
        }

        public unsafe Shader SetUniform(string uniformName, UniformType type, params object[] args) {
            //Get location from cache
            int location = this._uniformLocationCache.GetValueOrDefault(uniformName, -2);

            //If cache missed, get from OpenGL and store in cache
            if (location == -2) {
                int foundLocation = gl.GetUniformLocation(this._programId, uniformName);

                location = foundLocation;
                this._uniformLocationCache.Add(uniformName, foundLocation);
            }

            if(location == -1)
                Console.WriteLine($"[OpenGL Warning] Uniform Location for {uniformName} seems to not exist. It may have been optimized out or you simply misspelled the Uniform name");

            switch (type) {
                case UniformType.GlFloat: {
                    switch (args.Length) {
                        case 1: {
                            float arg1 = (float) args[0];

                            gl.Uniform1(location, arg1);

                            break;
                        }
                        case 2: {
                            float arg1 = (float) args[0];
                            float arg2 = (float) args[1];

                            gl.Uniform2(location, arg1, arg2);

                            break;
                        }
                        case 3: {
                            float arg1 = (float) args[0];
                            float arg2 = (float) args[1];
                            float arg3 = (float) args[2];

                            gl.Uniform3(location, arg1, arg2, arg3);

                            break;
                        }
                        case 4: {
                            float arg1 = (float) args[0];
                            float arg2 = (float) args[1];
                            float arg3 = (float) args[2];
                            float arg4 = (float) args[3];

                            gl.Uniform4(location, arg1, arg2, arg3, arg4);

                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException("args", $"You cannot have a vec{args.Length} as a uniform parameter!");
                    }
                    break;
                }
                case UniformType.GlInt: {
                    switch (args.Length) {
                        case 1: {
                            int arg1 = (int) args[0];

                            gl.Uniform1(location, arg1);

                            break;
                        }
                        case 2: {
                            int arg1 = (int) args[0];
                            int arg2 = (int) args[1];

                            gl.Uniform2(location, arg1, arg2);

                            break;
                        }
                        case 3: {
                            int arg1 = (int) args[0];
                            int arg2 = (int) args[1];
                            int arg3 = (int) args[2];

                            gl.Uniform3(location, arg1, arg2, arg3);

                            break;
                        }
                        case 4: {
                            int arg1 = (int) args[0];
                            int arg2 = (int) args[1];
                            int arg3 = (int) args[2];
                            int arg4 = (int) args[3];

                            gl.Uniform4(location, arg1, arg2, arg3, arg4);

                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException(nameof(args), $"You cannot have a int{args.Length} as a uniform parameter!");
                    }
                    break;
                }
                case UniformType.GlUint: {
                    switch (args.Length) {
                        case 1: {
                            uint arg1 = (uint) args[0];

                            gl.Uniform1(location, arg1);

                            break;
                        }
                        case 2: {
                            uint arg1 = (uint) args[0];
                            uint arg2 = (uint) args[1];

                            gl.Uniform2(location, arg1, arg2);

                            break;
                        }
                        case 3: {
                            uint arg1 = (uint) args[0];
                            uint arg2 = (uint) args[1];
                            uint arg3 = (uint) args[2];

                            gl.Uniform3(location, arg1, arg2, arg3);

                            break;
                        }
                        case 4: {
                            uint arg1 = (uint) args[0];
                            uint arg2 = (uint) args[1];
                            uint arg3 = (uint) args[2];
                            uint arg4 = (uint) args[3];

                            gl.Uniform4(location, arg1, arg2, arg3, arg4);

                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException("args", $"You cannot have a uint{args.Length} as a uniform parameter!");
                    }
                    break;
                }
                case UniformType.GlMat4f: {
                    Matrix4x4 matrix = (Matrix4x4) args[0];

                    gl.UniformMatrix4(location, 1, false, (float*) &matrix);

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return this;
        }
        /// <summary>
        /// Unbinds all Shaders
        /// </summary>
        /// <returns>Self, used for chaining Methods</returns>
        public Shader Unbind() {
            if (this._locked)
                return null;

            gl.UseProgram(0);

            return this;
        }

        /// <summary>
        /// Cleans up the Shader
        /// </summary>
        public void Dispose() {
            gl.DeleteProgram(this._programId);
        }
    }
}
