using Furball.Vixie;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Veldrid;
using Furball.Vixie.TestApplication;
using Silk.NET.Windowing;

// GraphicsBackend.PrefferedBackends = Backend.OpenGL41 | Backend.OpenGLES32;

// VeldridBackend.PrefferedBackend = Veldrid.GraphicsBackend.Vulkan;

new TestGame().Run(Backend.Vulkan);