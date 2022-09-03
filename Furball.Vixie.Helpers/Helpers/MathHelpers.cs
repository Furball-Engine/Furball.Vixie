using System;
using System.Diagnostics.Contracts;

namespace Furball.Vixie.Helpers.Helpers; 

public static class MathHelpers {
    //Taken from https://stackoverflow.com/a/2683487
    [Pure]
    public static pT Clamp<pT>(this pT val, pT min, pT max) where pT : IComparable<pT> {
        if (val.CompareTo(min) < 0) return min;
        if (val.CompareTo(max) > 0) return max;
            
        return val;
    }
}