using System;
using System.IO;
using System.Reflection;

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

        MemoryStream stream    = new();
        Stream?      resStream = assembly.GetManifestResourceStream(actualName);
        
        Guard.EnsureNonNull(resStream, "resStream");

        resStream!.CopyTo(stream);

        return stream;
    }
    /// <summary>
    /// Gets a String Resource
    /// </summary>
    /// <param name="path">Path to Resource</param>
    /// <param name="type">A type from the assembly to grab from</param>
    /// <returns>String with the resource</returns>
    public static string GetStringResource(string path, Type type) {
        Assembly assembly = Assembly.GetAssembly(type);
        string   actualName = assembly.GetName().Name + "." + path.Replace("/", ".");

        using Stream? resStream = assembly.GetManifestResourceStream(actualName);
        
        Guard.EnsureNonNull(resStream, "resStream");

        using StreamReader reader = new(resStream!);

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Gets a Byte Array Resource
    /// </summary>
    /// <param name="path">Path to Resource</param>
    /// <param name="type">A type from the assembly to grab from</param>
    /// <returns>String with the resource</returns>
    public static byte[] GetByteResource(string path, Type type) {
        Assembly assembly   = Assembly.GetAssembly(type);
        string   actualName = assembly.GetName().Name + "." + path.Replace("/", ".");

        using Stream? resStream = assembly.GetManifestResourceStream(actualName);
        
        Guard.EnsureNonNull(resStream, "resStream");

        using BinaryReader reader = new(resStream!);

        return reader.ReadBytes((int)reader.BaseStream.Length);
    }
}