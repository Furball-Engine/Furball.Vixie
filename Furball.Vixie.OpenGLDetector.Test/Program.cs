using System;
using System.Diagnostics;

namespace Furball.Vixie.OpenGLDetector.Test {
    class Program {
        static void Main() {
            var start = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
            var ver   = OpenGLDetector.GetLatestSupported();
            Console.WriteLine($"Latest OpenGL: {ver.gl.MajorVersion}.{ver.gl.MinorVersion}");
            Console.WriteLine($"Latest OpenGLES: {ver.gles.MajorVersion}.{ver.gles.MinorVersion}");
            var end = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
            Console.WriteLine($"That took {end - start} seconds!");
        }
    }
}
