using Furball.Vixie;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Veldrid;
using Furball.Vixie.TestApplication;
using Silk.NET.Windowing;

var options = WindowOptions.Default;

options.VSync = false;

GraphicsBackend.PrefferedBackends = Backend.Direct3D11;

VeldridBackend.PrefferedBackend = Veldrid.GraphicsBackend.OpenGL;

//Thread.Sleep(7500);

new TestGame(options).Run(options);