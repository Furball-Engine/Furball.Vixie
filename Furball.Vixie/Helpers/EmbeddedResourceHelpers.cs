using System.IO;
using System.Reflection;
using System.Text;

namespace Furball.Vixie.Helpers {
    public static class EmbeddedResourceHelpers {
        public static MemoryStream GetResource(string path) {
            Assembly assembly = Assembly.GetCallingAssembly();
            string actualName = assembly.GetName().Name + "." + path.Replace("/", ".");

            MemoryStream stream = new MemoryStream();
            Stream resStream = assembly.GetManifestResourceStream(actualName);

            if (resStream == null)
                return null;

            resStream.CopyTo(stream);

            return stream;
        }

        public static string GetStringResource(string path) {
            Assembly assembly = Assembly.GetCallingAssembly();
            string actualName = assembly.GetName().Name + "." + path.Replace("/", ".");

            MemoryStream stream = new MemoryStream();
            Stream resStream = assembly.GetManifestResourceStream(actualName);

            if (resStream == null)
                return null;

            resStream.CopyTo(stream);

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
