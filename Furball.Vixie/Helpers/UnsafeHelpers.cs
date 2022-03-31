using System.Runtime.CompilerServices;

namespace Furball.Vixie.Helpers {
    public static class UnsafeHelpers {
        internal static uint SizeInBytes<T>(this T[] array) where T : struct
        {
            return (uint)(array.Length * Unsafe.SizeOf<T>());
        }
    }
}
