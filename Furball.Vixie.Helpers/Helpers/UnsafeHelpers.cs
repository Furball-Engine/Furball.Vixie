namespace Furball.Vixie.Helpers.Helpers {
    public static class UnsafeHelpers {
        public static unsafe uint SizeInBytes<T>(this T[] array) where T : unmanaged
        {
            return (uint)(array.Length * sizeof(T));
        }
    }
}
