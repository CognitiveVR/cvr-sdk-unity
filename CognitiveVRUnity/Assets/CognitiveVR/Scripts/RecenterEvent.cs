using UnityEngine;
using System.Collections;

/// <summary>
/// sends recenter hmd transaction
/// </summary>

namespace CognitiveVR.Components
{
    public class RecenterEvent : CognitiveVRAnalyticsComponent
    {
#if CVR_OCULUS
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);
            if (OVRManager.display != null)
                OVRManager.display.RecenteredPose += RecenterEventTracker_RecenteredPose;
        }

        private void RecenterEventTracker_RecenteredPose()
        {
            new CustomEvent("cvr.recenter").Send();
        }
#endif

        public static bool GetWarning()
        {
#if CVR_OCULUS
            return false;
#else
            return true;
#endif
        }

        public static string GetDescription()
        {
            return "Sends transaction when the HMD recenters\nRequires Oculus Utilities";
        }

        void OnDestroy()
        {
#if CVR_OCULUS
            OVRManager.display.RecenteredPose -= RecenterEventTracker_RecenteredPose;
#endif
        }
    }
}