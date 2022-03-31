using System;
using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.Graphics.Backends.Veldrid;
using Furball.Vixie.TestApplication;
using Silk.NET.Windowing;

var options = WindowOptions.Default;

options.VSync = false;

GraphicsBackend.PrefferedBackends = Backend.Veldrid;

new TestGame(options).Run(options);