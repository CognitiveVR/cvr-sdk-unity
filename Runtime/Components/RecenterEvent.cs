using UnityEngine;
using System.Collections;

/// <summary>
/// sends recenter hmd transaction
/// </summary>

//TODO add support for other recenter events, if there is a general way to do this

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Recenter Event")]
    public class RecenterEvent : AnalyticsComponentBase
    {
#if C3D_OCULUS
        public override void Cognitive3D_Init()
        {
            base.Cognitive3D_Init();
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
#if C3D_OCULUS
            return false;
#else
            return true;
#endif
        }

        public override string GetDescription()
        {
#if C3D_OCULUS
            return "Sends transaction when the HMD recenters";
#else
            return "Current platform does not support this component\nRequires Oculus Utilities";
#endif

        }

        void OnDestroy()
        {
#if C3D_OCULUS
            OVRManager.display.RecenteredPose -= RecenterEventTracker_RecenteredPose;
#endif
        }
    }
}