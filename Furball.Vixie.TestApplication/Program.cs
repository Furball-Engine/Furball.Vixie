using Furball.Vixie.TestApplication;
using Silk.NET.Windowing;

var options = WindowOptions.Default;
options.WindowBorder           = WindowBorder.Fixed;
options.TransparentFramebuffer = true;

new TestGame(options).Run();