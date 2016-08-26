using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Check if the user has headphones connected
/// </summary>

namespace CognitiveVR
{
    public class HeadphoneTracker : CognitiveVRAnalyticsComponent
    {
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);

#if CVR_OCULUS
            Instrumentation.updateDeviceState(new Dictionary<string, object>() { { "cvr.vr.headphonespresent", OVRPlugin.headphonesPresent } });
#elif CVR_STEAMVR
            //TODO could check SteamVR_Ears if using speaker?
#endif

        }

        public static string GetDescription()
        {
            return "Check if the user has headphones connected.\nCurrently only works with Oculus Utilities";
        }
    }
}