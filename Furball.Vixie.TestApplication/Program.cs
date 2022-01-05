using Furball.Vixie.TestApplication;
using Silk.NET.Windowing;

var options = WindowOptions.Default;
options.API   = new GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 0));
options.VSync = false;
//options.WindowBorder = WindowBorder.Fixed;

new TestGame(options).Run(options);