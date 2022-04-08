using System.Runtime.CompilerServices;

namespace Furball.Vixie.Helpers.Helpers {
    public static class UnsafeHelpers {
        public static uint SizeInBytes<T>(this T[] array) where T : struct
        {
            return (uint)(array.Length * Unsafe.SizeOf<T>());
        }
    }
}
