using Furball.Vixie;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Veldrid;
using Furball.Vixie.TestApplication;
using Silk.NET.Windowing;

var options = WindowOptions.Default;

options.VSync = false;

GraphicsBackend.PrefferedBackends = Backend.Veldrid;

VeldridBackend.PrefferedBackend = Veldrid.GraphicsBackend.OpenGL;

new TestGame(options).Run(options);