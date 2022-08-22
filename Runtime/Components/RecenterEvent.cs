using UnityEngine;
using System.Collections;

/// <summary>
/// sends recenter hmd transaction
/// </summary>

namespace CognitiveVR.Components
{
    [AddComponentMenu("Cognitive3D/Components/Recenter Event")]
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

        public override bool GetWarning()
        {
#if CVR_OCULUS
            return false;
#else
            return true;
#endif
        }

        public override string GetDescription()
        {
#if CVR_OCULUS
            return "Sends transaction when the HMD recenters";
#else
            return "Current platform does not support this component\nRequires Oculus Utilities";
#endif

        }

        void OnDestroy()
        {
#if CVR_OCULUS
            OVRManager.display.RecenteredPose -= RecenterEventTracker_RecenteredPose;
#endif
        }
    }
}