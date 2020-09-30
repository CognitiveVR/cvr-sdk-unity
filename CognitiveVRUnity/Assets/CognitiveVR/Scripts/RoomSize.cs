using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Adds room size from SteamVR chaperone to device info
/// </summary>

namespace CognitiveVR.Components
{
    [AddComponentMenu("Cognitive3D/Components/Room Size")]
    public class RoomSize : CognitiveVRAnalyticsComponent
    {
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
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
                Core.SetSessionProperty("c3d.roomsizeDescription", string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:0.0} x {1:0.0}", roomX*100, roomY*100));
                Core.SetSessionProperty("c3d.roomscale", !seated);
            }
#elif CVR_OCULUS

            //(x = width, y = height, z = depth)
            if (OVRManager.boundary == null) { return; }
            if (OVRManager.boundary.GetConfigured())
            {
                Vector3 dimensions = OVRManager.boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea);
                Core.SetSessionProperty("c3d.roomsizeDescription", string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:0.0} x {1:0.0}", dimensions.x * 100, dimensions.z * 100));
                Core.SetSessionProperty("c3d.roomsize", dimensions.x * dimensions.z * 100);
                Core.SetSessionProperty("c3d.roomscale", OVRManager.boundary.GetConfigured());
            }
#endif
            //TODO pico. unclear how to get boundaries from api
        }

        public override bool GetWarning()
        {
#if CVR_STEAMVR || CVR_STEAMVR || CVR_OCULUS || CVR_STEAMVR2
            return false;
#else
            return true;
#endif
        }

        public override string GetDescription()
        {
#if CVR_STEAMVR || CVR_STEAMVR2
            return "Include Room Size in Session Properties from SteamVR Chaperone";
#elif CVR_OCULUS
            return "Include Room Size in Session Properties from Oculus Guardian";
#else
            return "Current platform does not support this component";
#endif
        }
    }
}