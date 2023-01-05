﻿using UnityEngine;
using System.Collections;
#if C3D_STEAMVR || C3D_STEAMVR2
using Valve.VR;
#endif

/// <summary>
/// Send a Custom Event when SteamVR Chaperone is visible
/// </summary>

//TODO add picovr sdk Pvr_UnitySDKAPI.BoundarySystem.UPvr_BoundaryGetVisible();
//TODO investigate openxr support for boundary visibility events

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Boundary Event")]
    public class BoundaryEvent : AnalyticsComponentBase
    {
#if C3D_STEAMVR2
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            //Cognitive3D_Manager.PoseEvent += Cognitive3D_Manager_PoseEventHandler;

            Valve.VR.SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsHidden).AddListener(OnChaperoneChanged);
            Valve.VR.SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsShown).AddListener(OnChaperoneChanged);

            if (Valve.VR.OpenVR.Chaperone.AreBoundsVisible())
            {
                new CustomEvent("cvr.boundary").Send();
            }
        }

        private void OnChaperoneChanged(Valve.VR.VREvent_t arg0)
        {
            if (Valve.VR.OpenVR.Chaperone.AreBoundsVisible())
            {
                new CustomEvent("cvr.boundary").SetProperty("visible", true).Send();
            }
            else
            {
                new CustomEvent("cvr.boundary").SetProperty("visible", false).Send();
            }
        }

        void OnDestroy()
        {
            Valve.VR.SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsHidden).RemoveListener(OnChaperoneChanged);
            Valve.VR.SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsShown).RemoveListener(OnChaperoneChanged);
        }
#endif

        public override string GetDescription()
        {
#if C3D_STEAMVR2
            return "Sends an event when Boundary becomes visible and becomes hidden";
#else
            return "Current platform does not support this component";
#endif
        }

        public override bool GetWarning()
        {
#if C3D_STEAMVR2
            return false;
#else
            return true;
#endif
        }

    }
}