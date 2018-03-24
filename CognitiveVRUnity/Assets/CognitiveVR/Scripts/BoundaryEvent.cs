using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when SteamVR Chaperone is visible
/// </summary>

namespace CognitiveVR.Components
{
    public class BoundaryEvent : CognitiveVRAnalyticsComponent
    {
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);

#if CVR_STEAMVR
            CognitiveVR_Manager.PoseEvent += CognitiveVR_Manager_PoseEventHandler;


            if (Valve.VR.OpenVR.Chaperone.AreBoundsVisible())
            {
                chaperoneGUID = Util.GetUniqueId();
                new CustomEvent("cvr.boundary", chaperoneGUID).Send();
                Util.logDebug("chaperone visible");
            }
#endif
        }

#if CVR_STEAMVR
        string chaperoneGUID;
        void CognitiveVR_Manager_PoseEventHandler(Valve.VR.EVREventType evrevent)
        {
            if (evrevent == Valve.VR.EVREventType.VREvent_ChaperoneDataHasChanged)
            {
                if (Valve.VR.OpenVR.Chaperone.AreBoundsVisible())
                {
                    chaperoneGUID = Util.GetUniqueId();
                    new CustomEvent("cvr.boundary").SetProperty("visible", true).Send();
                    Util.logDebug("chaperone visible");
                }
                else
                {
                    new CustomEvent("cvr.boundary").SetProperty("visible", false).Send();
                }
            }
        }
#endif

#if CVR_OCULUS
        string transactionID;
        void Update()
        {
            if (OVRManager.boundary.GetVisible() && string.IsNullOrEmpty(transactionID))
            {
                transactionID = Util.GetUniqueId();
                new CustomEvent("cvr.boundary").SetProperty("visible", true).Send();

            }
            if (!OVRManager.boundary.GetVisible() && !string.IsNullOrEmpty(transactionID))
            {
                new CustomEvent("cvr.boundary").SetProperty("visible", false).Send();
                transactionID = string.Empty;
            }
        }
#endif

        public static string GetDescription()
        {
            return "Sends transaction when SteamVR Chaperone or Oculus Guardian becomes visible and becomes hidden";
        }

        public static bool GetWarning()
        {
#if (!CVR_OCULUS && !CVR_STEAMVR) || UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }

        void OnDestroy()
        {
#if CVR_STEAMVR
            CognitiveVR_Manager.PoseEvent -= CognitiveVR_Manager_PoseEventHandler;
#endif
        }
    }
}