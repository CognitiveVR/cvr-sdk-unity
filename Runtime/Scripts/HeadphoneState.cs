using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Check if the user has headphones connected
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Headphone State")]
    public class HeadphoneState : AnalyticsComponentBase
    {
        public override void Cognitive3D_Init()
        {
            base.Cognitive3D_Init();

#if C3D_OCULUS
            //TODO add oculus audio changed events
            Core.SetSessionProperty("c3d.headphonespresent", OVRPlugin.headphonesPresent);
#elif C3D_STEAMVR
            //TODO could check SteamVR_Ears if using speaker?
#endif

        }

        public override bool GetWarning()
        {
#if C3D_OCULUS && UNITY_ANDROID
            return false;
#else
            return true;
#endif
        }

        public override string GetDescription()
        {
#if C3D_OCULUS && UNITY_ANDROID
            return "Check if the user has headphones connected";
#else
            return "Currently only works with Oculus Utilities on Android";
#endif
        }
    }
}