using UnityEngine;
using UnityEngine.InputSystem;
using UniSense;
using UniSense.LowLevel;

public class DualSenseTriggerRumble : MonoBehaviour
{
    private DualSenseGamepadHID ds;

    void Start()
    {
        // ค้นหา DualSense Controller ตัวแรกที่เชื่อมต่อ
        ds = DualSenseGamepadHID.FindFirst();

        if (ds == null)
        {
            Debug.LogError("No DualSense controller found!");
        }
        else
        {
            Debug.Log("DualSense controller found!");
        }
    }

    void Update()
    {
        if (ds == null) return;

        // ปรับความแรงของ Trigger (0.0f ถึง 1.0f)
        //float leftIntensity = 0.7f;
        //float rightIntensity = 0.3f;

        // ตั้งค่า Trigger ฝั่งซ้าย (L2)
        var leftTriggerState = new DualSenseTriggerState
        {
            EffectType = DualSenseTriggerEffectType.EffectEx,
            EffectEx = new DualSenseEffectExProperties
            {
                StartPosition = 0,
                KeepEffect = true,
                BeginForce = 0,
                MiddleForce = 160,
                EndForce = 255,
                Frequency = 10
            }
        };

        var rightTriggerState = new DualSenseTriggerState
        {
            EffectType = DualSenseTriggerEffectType.EffectEx,
            EffectEx = new DualSenseEffectExProperties
            {
                StartPosition = 0,
                KeepEffect = true,
                BeginForce = 0,
                MiddleForce = 160,
                EndForce = 255,
                Frequency = 10
            }
        };

        ds.SetGamepadState(new DualSenseGamepadState
        {
            LeftTrigger = leftTriggerState,
            RightTrigger = rightTriggerState
        });
    }

    private void OnDisable()
    {
        if (ds != null)
        {
            // Reset triggers to no resistance
            var resetState = new DualSenseGamepadState
            {
                LeftTrigger = new DualSenseTriggerState
                {
                    EffectType = DualSenseTriggerEffectType.NoResistance
                },
                RightTrigger = new DualSenseTriggerState
                {
                    EffectType = DualSenseTriggerEffectType.NoResistance
                }
            };

            ds.SetGamepadState(resetState);

            // Also reset any other haptics
            ds.ResetHaptics();
        }
    }
}
