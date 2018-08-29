using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Check if the user has headphones connected
/// </summary>

namespace CognitiveVR.Components
{
    public class HeadphoneState : CognitiveVRAnalyticsComponent
    {
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);

#if CVR_OCULUS
            //TODO add oculus audio changed events
            Core.UpdateSessionState(new Dictionary<string, object>() { { "cvr.vr.headphonespresent", OVRPlugin.headphonesPresent } });
#elif CVR_STEAMVR
            //TODO could check SteamVR_Ears if using speaker?
#endif

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
            return "Check if the user has headphones connected.\nCurrently only works with Oculus Utilities on Android";
        }
    }
}