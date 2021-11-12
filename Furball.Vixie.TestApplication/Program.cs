using Furball.Vixie.TestApplication;
using Silk.NET.Windowing;

var options = WindowOptions.Default;
options.VSync = false;
//options.WindowBorder = WindowBorder.Fixed;

new TestGame(options).Run(options);