using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using Veldrid;
using Veldrid.SPIRV;

namespace Furball.Vixie.Backends.Veldrid {
    public class LineRendererVeldrid : ILineRenderer {
        private readonly VeldridBackend _backend;
        
        private Pipeline _pipeline;
        
        [StructLayout(LayoutKind.Sequential)]
        private struct InstanceData {
            public Vector2 InstancePosition;
            public Vector2 InstanceSize;
            public Color   InstanceColor;
            public float   InstanceRotation;
        }
        
        
        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex {
            public Vector2 VertexPosition;
        }

        private readonly DeviceBuffer _projectionBuffer;
        private readonly ResourceSet  _projectionBufferResourceSet;
        
        private readonly DeviceBuffer _instanceVertexBuffer;
        private readonly DeviceBuffer _vertexBuffer;//TODO: maybe this can be static?
        private readonly DeviceBuffer _indexBuffer; //TODO: maybe this can be static?

        private static ushort[] _Indicies = new ushort[] {
            //Tri 1
            0, 1, 2,
            //Tri 2
            2, 3, 0
        };

        private static Vertex[] _Vertices = new Vertex[] {
            //Bottom left
            new() {
                VertexPosition    = new(0, 1),
            },
            //Bottom right
            new() {
                VertexPosition    = new(1, 1),
            },
            //Top right
            new() {
                VertexPosition    = new(1, 0),
            },
            //Top left
            new() {
                VertexPosition    = new(0, 0),
            }
        };

        
        public unsafe LineRendererVeldrid(VeldridBackend backend) {
            this._backend = backend;
            this._backend.CheckThread();

            string vertexSource   = ResourceHelpers.GetStringResource("Shaders/LineRenderer/VertexShader.glsl");
            string fragmentSource = ResourceHelpers.GetStringResource("Shaders/LineRenderer/FragmentShader.glsl");

            ShaderDescription vertexShaderDescription   = new ShaderDescription(ShaderStages.Vertex,   Encoding.UTF8.GetBytes(vertexSource),   "main");
            ShaderDescription fragmentShaderDescription = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentSource), "main");

            Shader[] shaders = this._backend.ResourceFactory.CreateFromSpirv(vertexShaderDescription, fragmentShaderDescription);

            VertexLayoutDescription vtxLayout = new VertexLayoutDescription(new[] {
                new VertexElementDescription("VertexPosition",    VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate), 
            }) {
                InstanceStepRate = 0
            };

            VertexLayoutDescription instanceVtxLayout = new VertexLayoutDescription(new[] {
                new VertexElementDescription("InstancePosition",            VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate), 
                new VertexElementDescription("InstanceSize",                VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("InstanceColor",               VertexElementFormat.Float4, VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("InstanceRotation",            VertexElementFormat.Float1, VertexElementSemantic.TextureCoordinate),
            }) {
                InstanceStepRate = 1
            };

            #region create projection buffer
            BufferDescription projBufDesc = new BufferDescription((uint)sizeof(Matrix4x4), BufferUsage.UniformBuffer);
            this._projectionBuffer = this._backend.ResourceFactory.CreateBuffer(projBufDesc);
            
            ResourceSetDescription projBufResourceSetDesc = new() {
                BoundResources = new[] {
                    this._projectionBuffer
                },
                Layout = this._backend.ResourceFactory.CreateResourceLayout(new(new[] {
                    new ResourceLayoutElementDescription("ProjectionMatrixUniform", ResourceKind.UniformBuffer, ShaderStages.Vertex)
                }))
            };
            this._projectionBufferResourceSet = this._backend.ResourceFactory.CreateResourceSet(projBufResourceSetDesc);
            #endregion

            GraphicsPipelineDescription pipelineDescription = new() {
                ShaderSet = new ShaderSetDescription {
                    Shaders = shaders,
                    VertexLayouts = new[] {
                        vtxLayout, instanceVtxLayout
                    }
                },
                Outputs           = backend.RenderFramebuffer.OutputDescription,
                BlendState        = BlendStateDescription.SingleAlphaBlend,
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ResourceLayouts = new[] {
                    projBufResourceSetDesc.Layout,
                },
                RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, true)
            };

            this._pipeline = backend.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);

            #region Create render buffers
            BufferDescription vtxBufferDesc         = new BufferDescription((uint)sizeof(Vertex)       * 4,             BufferUsage.VertexBuffer);
            BufferDescription instanceVtxBufferDesc = new BufferDescription((uint)sizeof(InstanceData) * NUM_INSTANCES, BufferUsage.VertexBuffer);

            BufferDescription indexBufferDesc = new BufferDescription((uint)sizeof(ushort) * 6, BufferUsage.IndexBuffer);
            
            this._vertexBuffer = this._backend.ResourceFactory.CreateBuffer(vtxBufferDesc);
            this._instanceVertexBuffer = this._backend.ResourceFactory.CreateBuffer(instanceVtxBufferDesc);

            this._indexBuffer = this._backend.ResourceFactory.CreateBuffer(indexBufferDesc);
            
            //Fill our vertex and index buffer
            this._backend.GraphicsDevice.UpdateBuffer(this._vertexBuffer, 0, _Vertices);
            this._backend.GraphicsDevice.UpdateBuffer(this._indexBuffer,  0, _Indicies);
            #endregion
        }

        private bool _isDisposed = false;
        public void Dispose() {
            this._backend.CheckThread();
            if (this._isDisposed) return;
            
            this._isDisposed = true;

            this._pipeline.Dispose();
            this._indexBuffer.Dispose();
            this._projectionBuffer.Dispose();
            this._vertexBuffer.Dispose();
            this._instanceVertexBuffer.Dispose();
            this._projectionBufferResourceSet.Dispose();
        }

        ~LineRendererVeldrid() {
            DisposeQueue.Enqueue(this);
        }
        
        public bool IsBegun {
            get;
            set;
        }
        
        public void Begin() {
            this._backend.CheckThread();
            this.IsBegun = true;
            
            this._backend.CommandList.SetPipeline(this._pipeline);

            //Update the UBO with the projection matrix
            this._backend.CommandList.UpdateBuffer(this._projectionBuffer, 0, this._backend.ProjectionMatrix);
            this._backend.CommandList.SetGraphicsResourceSet(0, this._projectionBufferResourceSet);
            
            //Set the index buffer
            this._backend.CommandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt16);
            //Set the main vertex buffer
            this._backend.CommandList.SetVertexBuffer(0, this._vertexBuffer);
            //Set the vertex buffer that contains our instance data
            this._backend.CommandList.SetVertexBuffer(1, this._instanceVertexBuffer);
        }
        
        public void Draw(Vector2 begin, Vector2 end, float thickness, Color color) {
            this._backend.CheckThread();
            if (!this.IsBegun)
                throw new Exception("Begin() has not been called!");

            if (this._instances >= NUM_INSTANCES) {
                this.Flush();
            }
            
            this._instanceData[this._instances].InstancePosition = begin;
            this._instanceData[this._instances].InstanceSize.X   = (end - begin).Length();
            this._instanceData[this._instances].InstanceSize.Y   = thickness;
            this._instanceData[this._instances].InstanceColor    = color;
            this._instanceData[this._instances].InstanceRotation = (float)Math.Atan2(end.Y - begin.Y, end.X - begin.X);

            this._instances++;
        }
        
        private          uint           _instances    = 0;
        private readonly InstanceData[] _instanceData = new InstanceData[NUM_INSTANCES];

        private const int NUM_INSTANCES = 1024;

        private unsafe void Flush() {
            this._backend.CheckThread();
            if (this._instances == 0) return;

            //Update the vertex buffer with just the data we use
            fixed (void* ptr = this._instanceData)
                this._backend.CommandList.UpdateBuffer(this._instanceVertexBuffer, 0, (IntPtr)ptr, (uint)(sizeof(InstanceData) * this._instances));

            //Draw the data to the screen
            this._backend.CommandList.DrawIndexed(6, this._instances, 0, 0, 0);

            this._instances = 0;
        }
        
        public void End() {
            this._backend.CheckThread();
            this.Flush();
            this.IsBegun = false;
        }
    }
}
