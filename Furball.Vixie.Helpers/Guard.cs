using System;
using System.Diagnostics;

namespace Furball.Vixie.Helpers; 

public static class Guard {
    //NOTE: the reason `EnsureNonNull` and `EnsureNull` are not marked as `Conditional` is to let us put method calls
    //inline in the `obj` parameter, which is much cleaner and prevents the calls from being poofed on release builds
    
    /// <summary>
    /// Ensures an object is not null
    /// </summary>
    /// <param name="obj">Object to check</param>
    /// <param name="valueExpression">The expression </param>
    /// <exception cref="ArgumentNullException">When the object is null</exception>
    public static void EnsureNonNull(object? obj, string valueExpression = "") {
#if DEBUG
        _ = obj ?? throw new ArgumentNullException(nameof (obj), valueExpression);
#endif
    }
    
    /// <summary>
    /// Ensures an object is null
    /// </summary>
    /// <param name="obj">Object to check</param>
    /// <param name="msg">The message to display in the exception</param>
    /// <exception cref="ArgumentException">When the object is non-null</exception>
    public static void EnsureNull(object? obj, string msg) {
#if DEBUG
        if(obj != null)
            throw new ArgumentException(msg);
#endif
    }

    /// <summary>
    /// Asserts a value is true
    /// </summary>
    /// <param name="value">The value to assert</param>
    /// <param name="valueExpression">The expression that evaluated the bool</param>
    /// <exception cref="ArgumentException">When the value is false</exception>
    public static void Assert(bool value, string valueExpression = "") {
#if DEBUG
        if (!value)
            throw new ArgumentException(valueExpression, nameof (value));
#endif
    }
    
    [Conditional("DEBUG")]
    public static void Fail(string message) {
        throw new Exception(message);
    }
}