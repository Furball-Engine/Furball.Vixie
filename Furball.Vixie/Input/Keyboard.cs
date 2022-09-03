using System.Collections.Generic;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;

namespace Furball.Vixie.Input; 

public class Keyboard {
    public static KeyboardState GetState(int keyboard = 0) {
        return Global.GameInstance.InputContext.Keyboards[keyboard].CaptureState();
    }

    public static IReadOnlyList<IKeyboard> GetKeyboards() {
        return Global.GameInstance.InputContext.Keyboards;
    }

    public static IKeyboard GetKeyboard(int keyboard = 0) {
        return Global.GameInstance.InputContext.Keyboards[keyboard];
    }
}