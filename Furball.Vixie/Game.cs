using System;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Furball.Vixie {
    public abstract class Game : IDisposable {
        private   IWindow _gameWindow;
        protected GL      gl;

        public Game(WindowOptions options) {
            this._gameWindow = Window.Create(options);

            this._gameWindow.Update += this.Update;
            this._gameWindow.Render += this.Draw;
            this._gameWindow.Load   += this.RendererInitialize;
        }

        public void Run() {
            this._gameWindow.Run();
        }

        internal void RendererInitialize() {
            Global.Gl = GL.GetApi(this._gameWindow);
            this.gl   = Global.Gl;

            //TODO: input stuffs

            this.Initialize();
        }

        protected abstract void Initialize();
        protected abstract void Update(double obj);
        protected abstract void Draw(double obj);
        public abstract void Dispose();
    }
}
