using System.Numerics;
using Furball.Vixie.Graphics.Renderers;

namespace Furball.Vixie.Graphics.Backends.Veldrid {
    public class LineRendererVeldrid : ILineRenderer {
        private readonly VeldridBackend _backend;
        
        public LineRendererVeldrid(VeldridBackend backend) {
            this._backend = backend;
        }
        
        public void Dispose() {
            throw new System.NotImplementedException();
        }
        public bool IsBegun {
            get;
            set;
        }
        public void Begin() {
            throw new System.NotImplementedException();
        }
        public void Draw(Vector2 begin, Vector2 end, float thickness, Color color) {
            throw new System.NotImplementedException();
        }
        public void End() {
            throw new System.NotImplementedException();
        }
    }
}
