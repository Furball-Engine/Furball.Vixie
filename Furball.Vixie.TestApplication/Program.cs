using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.TestApplication;
using Silk.NET.Windowing;

var options = WindowOptions.Default;
options.VSync           = false;

new TestGame(options).Run(options, Backend.OpenGLES);

//TODO: also cleanup everything i made during crunch, this is probably some of the worst refactoring uve ever seen
