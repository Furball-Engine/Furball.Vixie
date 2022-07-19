using System.IO;
using System.Reflection;
using System.Text;

namespace Furball.Vixie.Helpers.Helpers; 

public static class ResourceHelpers {
    /// <summary>
    /// Gets a Embedded Resource Stream
    /// </summary>
    /// <param name="path">Path to Resource</param>
    /// <param name="vixieResource">Is it a resource from Vixie?</param>
    /// <returns>Stream with the resource</returns>
    public static MemoryStream GetResource(string path, bool vixieResource = false) {
        Assembly assembly   = vixieResource ? Assembly.GetExecutingAssembly() : Assembly.GetCallingAssembly();
        string   actualName = assembly.GetName().Name + "." + path.Replace("/", ".");

        MemoryStream stream    = new MemoryStream();
        Stream       resStream = assembly.GetManifestResourceStream(actualName);

        if (resStream == null)
            return null;

        resStream.CopyTo(stream);

        return stream;
    }
    /// <summary>
    /// Gets a String Resource
    /// </summary>
    /// <param name="path">Path to Resource</param>
    /// <param name="vixieResource">Is it a resource from Vixie?</param>
    /// <returns>String with the resource</returns>
    public static string GetStringResource(string path, bool vixieResource = false) {
        Assembly assembly   = vixieResource ? Assembly.GetExecutingAssembly() : Assembly.GetCallingAssembly();
        string   actualName = assembly.GetName().Name + "." + path.Replace("/", ".");

        MemoryStream stream    = new MemoryStream();
        Stream       resStream = assembly.GetManifestResourceStream(actualName);

        if (resStream == null)
            return null;

        resStream.CopyTo(stream);

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Gets a Byte Array Resource
    /// </summary>
    /// <param name="path">Path to Resource</param>
    /// <param name="vixieResource">Is it a resource from Vixie?</param>
    /// <returns>String with the resource</returns>
    public static byte[] GetByteResource(string path, bool vixieResource = false) {
        Assembly assembly   = vixieResource ? Assembly.GetExecutingAssembly() : Assembly.GetCallingAssembly();
        string   actualName = assembly.GetName().Name + "." + path.Replace("/", ".");

        MemoryStream stream    = new MemoryStream();
        Stream       resStream = assembly.GetManifestResourceStream(actualName);

        if (resStream == null)
            return null;

        resStream.CopyTo(stream);

        return stream.ToArray();
    }
}