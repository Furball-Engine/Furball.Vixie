using System;
using System.Drawing;
using System.Numerics;

namespace Furball.Vixie.Graphics.Renderers {
    public interface ILineRenderer : IDisposable {
        public bool IsBegun { get; set; }
        /// <summary>
        /// Begins the Renderer, used for initializing things
        /// </summary>
        void Begin();
        void Draw(Vector2 begin, Vector2 end, float thickness, Color color);
        /// <summary>
        /// Ends the Rendering, use this to finish drawing or do something at the very end
        /// </summary>
        void End();
    }
}
