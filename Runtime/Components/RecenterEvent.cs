using UnityEngine;
using System.Collections;

/// <summary>
/// Sends a Custom Event when the user recenters their hmd
/// </summary>

//TODO add support for other recenter events, if there is a general way to do this

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Recenter Event")]
    public class RecenterEvent : AnalyticsComponentBase
    {
#if C3D_OCULUS
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            if (OVRManager.display != null)
            {
                OVRManager.display.RecenteredPose += RecenterEventTracker_RecenteredPose;
                Cognitive3D_Manager.OnPostSessionEnd += Cognitive3D_Manager_OnPostSessionEnd;
            }
        }

        private void RecenterEventTracker_RecenteredPose()
        {
            CustomEvent.SendCustomEvent("cvr.recenter");
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
            return "Sends a Custom Event when the HMD recenters";
#else
            return "Current platform does not support this component\nRequires Oculus Utilities";
#endif
        }

#if C3D_OCULUS
        void OnDestroy()
        {
            Cognitive3D_Manager_OnPostSessionEnd();
        }

        private void Cognitive3D_Manager_OnPostSessionEnd()
        {
            OVRManager.display.RecenteredPose -= RecenterEventTracker_RecenteredPose;
            Cognitive3D_Manager.OnPostSessionEnd -= Cognitive3D_Manager_OnPostSessionEnd;
        }
#endif
    }
}