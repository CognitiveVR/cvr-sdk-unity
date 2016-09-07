using UnityEngine;
using System.Collections;

/// <summary>
/// sends recenter hmd transaction
/// </summary>

namespace CognitiveVR
{
    public class RecenterEventTracker : CognitiveVRAnalyticsComponent
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
            Instrumentation.Transaction("Recenter").beginAndEnd();
        }
#endif

        public static string GetDescription()
        {
            return "Sends transaction when the HMD recenters\nRequires Oculus Utilities";
        }
    }
}