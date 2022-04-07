using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.TestApplication;
using Silk.NET.Windowing;

var options = WindowOptions.Default;

options.VSync = false;

GraphicsBackend.PrefferedBackends = Backend.OpenGL41 | Backend.OpenGLES;

new TestGame(options).Run(options);