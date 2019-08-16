using UnityEngine;
using System.Collections;
#if CVR_STEAMVR || CVR_STEAMVR2
using Valve.VR;
#endif

/// <summary>
/// sends transactions when SteamVR Chaperone is visible
/// </summary>

namespace CognitiveVR.Components
{
    [AddComponentMenu("Cognitive3D/Components/Boundary Event")]
    public class BoundaryEvent : CognitiveVRAnalyticsComponent
    {
#if CVR_STEAMVR
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            base.CognitiveVR_Init(initError);
            //CognitiveVR_Manager.PoseEvent += CognitiveVR_Manager_PoseEventHandler;

            SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsHidden).AddListener(OnChaperoneChanged);
            SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsShown).AddListener(OnChaperoneChanged);

            if (Valve.VR.OpenVR.Chaperone.AreBoundsVisible())
            {
                new CustomEvent("cvr.boundary").Send();
                Util.logDebug("chaperone visible");
            }
        }

        private void OnChaperoneChanged(VREvent_t arg0)
        {
            if (Valve.VR.OpenVR.Chaperone.AreBoundsVisible())
            {
                new CustomEvent("cvr.boundary").SetProperty("visible", true).Send();
                Util.logDebug("chaperone visible");
            }
            else
            {
                new CustomEvent("cvr.boundary").SetProperty("visible", false).Send();
                Util.logDebug("chaperone hidden");
            }
        }

        void OnDestroy()
        {
            //CognitiveVR_Manager.PoseEvent -= CognitiveVR_Manager_PoseEventHandler;
           SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsHidden).RemoveListener(OnChaperoneChanged);
           SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsShown).RemoveListener(OnChaperoneChanged);
        }
#endif


#if CVR_STEAMVR2
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            base.CognitiveVR_Init(initError);
            //CognitiveVR_Manager.PoseEvent += CognitiveVR_Manager_PoseEventHandler;
            Valve.VR.SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsHidden).AddListener(OnChaperoneChanged);
            Valve.VR.SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsShown).AddListener(OnChaperoneChanged);

            if (Valve.VR.OpenVR.Chaperone.AreBoundsVisible())
            {
                new CustomEvent("cvr.boundary").Send();
                Util.logDebug("chaperone visible INITIAL STATE");
            }
        }

        private void OnChaperoneChanged(Valve.VR.VREvent_t arg0)
        {
            if (Valve.VR.OpenVR.Chaperone.AreBoundsVisible())
            {
                new CustomEvent("cvr.boundary").SetProperty("visible", true).Send();
                Util.logDebug("chaperone visible");
            }
            else
            {
                new CustomEvent("cvr.boundary").SetProperty("visible", false).Send();
                Util.logDebug("chaperone hidden");
            }
        }

        void OnDestroy()
        {
            Valve.VR.SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsHidden).RemoveListener(OnChaperoneChanged);
            Valve.VR.SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsShown).RemoveListener(OnChaperoneChanged);
        }
#endif



#if CVR_OCULUS
        bool BoundsVisible;
        void Update()
        {
            if (OVRManager.boundary.GetVisible() && !BoundsVisible)
            {
                new CustomEvent("cvr.boundary").SetProperty("visible", true).Send();
                BoundsVisible = true;

            }
            if (!OVRManager.boundary.GetVisible() && BoundsVisible)
            {
                new CustomEvent("cvr.boundary").SetProperty("visible", false).Send();
                BoundsVisible = false;
            }
        }
#endif

        public override string GetDescription()
        {
#if CVR_STEAMVR || CVR_STEAMVR2
            return "Sends transaction when SteamVR Chaperone becomes visible and becomes hidden";
#elif CVR_OCULUS
            return "Sends transaction when Oculus Guardian becomes visible and becomes hidden";
#else
            return "Current platform does not support this component";
#endif
        }

        public override bool GetWarning()
        {
#if CVR_STEAMVR || CVR_STEAMVR2 || CVR_OCULUS
            return false;
#else
            return true;
#endif
        }

    }
}