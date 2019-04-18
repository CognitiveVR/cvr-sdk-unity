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
                //Core.SetSessionProperty("c3d.roomsize", 0);
                //Core.SetSessionProperty("c3d.roomsizeDescription", "0 x 0");
                //Core.SetSessionProperty("c3d.roomscale", false);
            }
            else
            {
                bool seated = Mathf.Approximately(roomX, 1f) && Mathf.Approximately(roomY, 1f);
                Core.SetSessionProperty("c3d.roomsize", roomX * roomY*100);
                Core.SetSessionProperty("c3d.roomsizeDescription", string.Format("{0:0.0} x {1:0.0}", roomX*100, roomY*100));
                Core.SetSessionProperty("c3d.roomscale", !seated);
            }
#elif CVR_OCULUS

            //(x = width, y = height, z = depth)
            if (OVRManager.boundary.GetConfigured())
            {
                Vector3 dimensions = OVRManager.boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea);
                Core.SetSessionProperty("c3d.roomsizeDescription", string.Format("{0:0.0} x {1:0.0}", dimensions.x * 100, dimensions.z * 100));
                Core.SetSessionProperty("c3d.roomsize", dimensions.x * dimensions.z * 100);
                Core.SetSessionProperty("c3d.roomscale", OVRManager.boundary.GetConfigured());
            }
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