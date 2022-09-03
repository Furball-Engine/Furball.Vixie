using System.Collections.Generic;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;

namespace Furball.Vixie.Input; 

public class Mouse {
    public static MouseState GetState(int mouse = 0) {
        return Global.GameInstance.InputContext.Mice[mouse].CaptureState();
    }

    public static IReadOnlyList<IMouse> GetMice() {
        return Global.GameInstance.InputContext.Mice;
    }

    public static IMouse GetMouse(int mouse = 0) {
        return Global.GameInstance.InputContext.Mice[mouse];
    }
}