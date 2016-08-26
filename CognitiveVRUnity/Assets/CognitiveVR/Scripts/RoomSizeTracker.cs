using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Adds room size from SteamVR chaperone to device info
/// </summary>

namespace CognitiveVR
{
    public class RoomSizeTracker : CognitiveVRAnalyticsComponent
    {
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);

#if CVR_STEAMVR
            float roomX = 0;
            float roomY = 0;
            if (Valve.VR.OpenVR.Chaperone == null || !Valve.VR.OpenVR.Chaperone.GetPlayAreaSize(ref roomX, ref roomY))
            {
                Instrumentation.updateDeviceState(new Dictionary<string, object>() { { "cvr.vr.roomsize", "0 x 0" }, { "cvr.vr.roomscale", false } });
            }
            else
            {
                bool seated = Mathf.Approximately(roomX, 1f) && roomX == roomY;
                Instrumentation.updateDeviceState(new Dictionary<string, object>()
                {
                    { "cvr.vr.roomsize", string.Format("{0:0.0} x {1:0.0}", roomX, roomY) },
                    { "cvr.vr.roomscale", !seated }
                });
            }
#elif CVR_OCULUS

            if (OVRManager.tracker.isPresent)
            {
                Instrumentation.updateDeviceState(new Dictionary<string, object>() {
                    { "cvr.vr.frustrumFOV", OVRManager.tracker.GetFrustum().fov },
                    { "cvr.vr.frustrumNear", OVRManager.tracker.GetFrustum().nearZ },
                    { "cvr.vr.frustrumFar", OVRManager.tracker.GetFrustum().farZ }});
            }
            else
            {
                Util.logDebug("OVRManager tracker is not present!");
            }
#endif
        }

        public static string GetDescription()
        {
            return "Include SteamVR Chaperone Room Size in Device Info\nOn Oculus, sends the tracker FOV, NearZ and FarZ";
        }
    }
}