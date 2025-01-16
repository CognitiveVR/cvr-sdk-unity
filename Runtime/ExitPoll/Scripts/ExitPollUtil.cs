using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Cognitive3D
{
	internal static class ExitPollUtil
    {
        internal static InputFeatureUsage<bool> GetButtonFeature(ExitPollManager.PointerInputButton button)
        {
            switch (button)
            {
                case ExitPollManager.PointerInputButton.Trigger:
                    return CommonUsages.triggerButton;
                case ExitPollManager.PointerInputButton.Grip:
                    return CommonUsages.gripButton;
                case ExitPollManager.PointerInputButton.PrimaryButton:
                    return CommonUsages.primaryButton;
                case ExitPollManager.PointerInputButton.SecondaryButton:
                    return CommonUsages.secondaryButton;
                case ExitPollManager.PointerInputButton.Primary2DAxisClick:
                    return CommonUsages.primary2DAxisClick;
                default:
                    return CommonUsages.triggerButton; // Default to trigger button if no match
            }
        }

        internal static bool GetButtonState(XRNode hand, InputFeatureUsage<bool> button)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
            bool isPressed = false;

            if (device.isValid)
            {
                device.TryGetFeatureValue(button, out isPressed);
            }

            return isPressed;
        }
    }
}
