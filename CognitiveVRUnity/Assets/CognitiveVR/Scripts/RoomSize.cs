using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Adds room size from SteamVR chaperone to device info
/// </summary>

namespace CognitiveVR.Components
{
    public class RoomSize : CognitiveVRAnalyticsComponent
    {
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);

#if CVR_STEAMVR || CVR_STEAMVR2
            float roomX = 0;
            float roomY = 0;
            if (Valve.VR.OpenVR.Chaperone == null || !Valve.VR.OpenVR.Chaperone.GetPlayAreaSize(ref roomX, ref roomY))
            {
                Core.UpdateSessionState(new Dictionary<string, object>() { { "cvr.vr.roomsize", "0 x 0" }, { "cvr.vr.roomscale", false } });
                //Instrumentation.updateDeviceState(new Dictionary<string, object>() { { "cvr.vr.roomsize", "0 x 0" }, { "cvr.vr.roomscale", false } });
            }
            else
            {
                bool seated = Mathf.Approximately(roomX, 1f) && Mathf.Approximately(roomY, 1f);
                Core.UpdateSessionState(new Dictionary<string, object>()
                {
                    { "cvr.vr.roomsize", string.Format("{0:0.0} x {1:0.0}", roomX, roomY) },
                    { "cvr.vr.roomscale", !seated }
                });
                //Instrumentation.updateDeviceState(new Dictionary<string, object>(){{ "cvr.vr.roomsize", string.Format("{0:0.0} x {1:0.0}", roomX, roomY) },{ "cvr.vr.roomscale", !seated }});
            }
#elif CVR_OCULUS

            //(x = width, y = height, z = depth)
            Vector3 dimensions = OVRManager.boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea);
            Core.UpdateSessionState(new Dictionary<string, object>()
            {
                { "cvr.vr.roomsize", string.Format("{0:0.0} x {1:0.0}", dimensions.x, dimensions.z) }
            });
            //Instrumentation.updateDeviceState(new Dictionary<string, object>(){{ "cvr.vr.roomsize", string.Format("{0:0.0} x {1:0.0}", dimensions.x, dimensions.z) }});
#endif
        }

        public static bool GetWarning()
        {
#if (!CVR_OCULUS && !CVR_STEAMVR) || UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }

        public static string GetDescription()
        {
            return "Include Room Size in Device Info from SteamVR Chaperone or Oculus Guardian";
        }
    }
}