using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UniSense;
using UnityEngine;

public static class DualSenseInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        InputSystem.RegisterLayout<DualSenseGamepadHID>(
            matches: new InputDeviceMatcher()
                .WithInterface("HID")
                .WithProduct("DualSense")
        );
    }
}
