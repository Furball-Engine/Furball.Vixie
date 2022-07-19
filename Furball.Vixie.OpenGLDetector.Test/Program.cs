using System;
using System.Diagnostics;
using Furball.Vixie.OpenGLDetector;
using Silk.NET.Windowing;

double start = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

(APIVersion GL, APIVersion GLES) ver = OpenGLDetector.GetLatestSupported();

Console.WriteLine($"Latest OpenGL: {ver.GL.MajorVersion}.{ver.GL.MinorVersion}");
Console.WriteLine($"Latest OpenGLES: {ver.GLES.MajorVersion}.{ver.GLES.MinorVersion}");

double end = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
Console.WriteLine($"That took {end - start} seconds!");