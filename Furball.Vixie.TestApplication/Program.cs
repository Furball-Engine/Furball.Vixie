using Furball.Vixie.Graphics.Backends;
using Furball.Vixie.TestApplication;
using Silk.NET.Windowing;

var options = WindowOptions.Default;
options.VSync           = false;
//options.WindowBorder = WindowBorder.Fixed;

new TestGame(options).Run(options, Backend.OpenGLES);

//TODO: do the auto dispose thingy, the second this thing starts its gonna leak like fucking 50gb of memory within 5 seconds
//TODO: also cleanup everything i made during crunch, this is probably some of the worst refactoring uve ever seen
