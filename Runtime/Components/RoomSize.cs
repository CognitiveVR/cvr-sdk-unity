﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Adds room size from SteamVR chaperone to device info
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Room Size")]
    public class RoomSize : AnalyticsComponentBase
    {
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();

            Vector3 roomsize = new Vector3();
            if (GameplayReferences.GetRoomSize(ref roomsize))
            {
#if XRPF
                if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsSpatialDataAllowed)
#endif
                {
                    Cognitive3D_Manager.SetSessionProperty("c3d.roomsize", roomsize.x * roomsize.z * 100);
                    Cognitive3D_Manager.SetSessionProperty("c3d.roomsizeDescription", string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} x {1:0.0}", roomsize.x * 100, roomsize.z * 100));
                }
            }
            else
            {
                Cognitive3D_Manager.SetSessionProperty("c3d.roomsize", "Invalid");
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