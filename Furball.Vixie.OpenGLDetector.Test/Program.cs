using System;
using System.Diagnostics;

namespace Furball.Vixie.OpenGLDetector.Test {
    class Program {
        static void Main() {
            var start = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
            var ver   = OpenGLDetector.GetLatestSupported();
            Console.WriteLine($"Latest OpenGL: {ver.GL.MajorVersion}.{ver.GL.MinorVersion}");
            Console.WriteLine($"Latest OpenGLES: {ver.GLES.MajorVersion}.{ver.GLES.MinorVersion}");
            var end = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
            Console.WriteLine($"That took {end - start} seconds!");
        }
    }
}
