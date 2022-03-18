using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.TestApplication;
using Silk.NET.Windowing;

var options = WindowOptions.Default;
options.VSync           = false;

new TestGame(options).Run(options, Backend.Direct3D11);