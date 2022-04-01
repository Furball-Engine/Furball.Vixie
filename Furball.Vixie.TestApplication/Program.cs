using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.TestApplication;
using Silk.NET.Windowing;

var options = WindowOptions.Default;

options.VSync = false;

GraphicsBackend.PrefferedBackends = Backend.OpenGLES | Backend.OpenGL41;

new TestGame(options).Run(options);