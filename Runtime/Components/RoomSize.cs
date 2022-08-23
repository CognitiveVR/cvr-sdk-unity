using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Adds room size from SteamVR chaperone to device info
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Room Size")]
    public class RoomSize : Cognitive3DAnalyticsComponent
    {
        public override void Cognitive3D_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            base.Cognitive3D_Init(initError);

            Vector3 roomsize = new Vector3();
            if (GameplayReferences.GetRoomSize(ref roomsize))
            {
                Core.SetSessionProperty("c3d.roomsize", roomsize.x * roomsize.z * 100);
                Core.SetSessionProperty("c3d.roomsizeDescription", string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} x {1:0.0}", roomsize.x * 100, roomsize.z * 100));
            }
        }

        public override bool GetWarning()
        {
            return !GameplayReferences.SDKSupportsRoomSize;
        }

        public override string GetDescription()
        {
            if (GameplayReferences.SDKSupportsRoomSize)
            {
                return "Include Room Size as a Session Properties";
            }
            else
            {
                return "Current platform does not support this component";
            }
        }
    }
}