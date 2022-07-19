using System;

namespace Furball.Vixie.Helpers.Helpers; 

public static class MathHelpers {
    //Taken from https://stackoverflow.com/a/2683487
    public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T> {
        if (val.CompareTo(min) < 0) return min;
        if (val.CompareTo(max) > 0) return max;
            
        return val;
    }
}