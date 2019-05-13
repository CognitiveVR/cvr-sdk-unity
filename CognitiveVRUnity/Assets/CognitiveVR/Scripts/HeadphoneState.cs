using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Check if the user has headphones connected
/// </summary>

namespace CognitiveVR.Components
{
    [AddComponentMenu("Cognitive3D/Components/Headphone State")]
    public class HeadphoneState : CognitiveVRAnalyticsComponent
    {
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            base.CognitiveVR_Init(initError);

#if CVR_OCULUS
            //TODO add oculus audio changed events
            Core.SetSessionProperty("c3d.headphonespresent", OVRPlugin.headphonesPresent);
#elif CVR_STEAMVR
            //TODO could check SteamVR_Ears if using speaker?
#endif

        }

        public override bool GetWarning()
        {
#if CVR_OCULUS && UNITY_ANDROID
            return false;
#else
            return true;
#endif
        }

        public override string GetDescription()
        {
#if CVR_OCULUS && UNITY_ANDROID
            return "Check if the user has headphones connected";
#else
            return "Currently only works with Oculus Utilities on Android";
#endif
        }
    }
}