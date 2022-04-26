using Android.App;
using Android.OS;
using Furball.Vixie.Backends.Shared.Backends;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl.Android;

namespace Furball.Vixie.TestApplication.Android
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : SilkActivity
    {
        protected override void OnRun() {
            var options = ViewOptions.Default;

            options.VSync = false;

            GraphicsBackend.PrefferedBackends = Backend.OpenGL41 | Backend.OpenGLES32 | Backend.OpenGLES30;
            
            new TestGame().RunAndroid(options);
        }
    }
}