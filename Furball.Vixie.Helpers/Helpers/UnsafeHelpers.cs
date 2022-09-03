namespace Furball.Vixie.Helpers.Helpers; 

public static class UnsafeHelpers {
    public static unsafe uint SizeInBytes<pT>(this pT[] array) where pT : unmanaged
    {
        return (uint)(array.Length * sizeof(pT));
    }
}