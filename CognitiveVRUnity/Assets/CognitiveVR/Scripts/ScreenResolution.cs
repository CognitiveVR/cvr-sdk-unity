using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Adds screen resolution to device info
/// </summary>

namespace CognitiveVR.Components
{
    public class ScreenResolution : CognitiveVRAnalyticsComponent
    {
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);

            Core.UpdateSessionState(new Dictionary<string, object>() { { "cvr.vr.screenresolution", Screen.height + " x " + Screen.width } });
            //CognitiveVR.Instrumentation.updateDeviceState(new Dictionary<string, object>() { { "cvr.vr.screenresolution", Screen.height + " x " + Screen.width } });
        }

        public static bool GetWarning()
        {
#if CVR_OCULUS && UNITY_ANDROID
            return false;
#else
            return true;
#endif
        }

        public static string GetDescription()
        {
            return "Include Screen Resolution in Device Info. Probably only useful for mobile";
        }
    }
}
