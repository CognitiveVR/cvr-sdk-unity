using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when SteamVR Chaperone is visible
/// </summary>

namespace CognitiveVR
{
    public class BoundaryEventTracker : CognitiveVRAnalyticsComponent
    {
        string chaperoneGUID;
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);

#if CVR_STEAMVR
            CognitiveVR_Manager.OnPoseEvent += CognitiveVR_Manager_PoseEventHandler;


            if (Valve.VR.OpenVR.Chaperone.AreBoundsVisible())
            {
                chaperoneGUID = System.Guid.NewGuid().ToString();
                Instrumentation.Transaction("cvr.boundary.visible", chaperoneGUID).begin();
                Util.logDebug("chaperone visible");
            }
#endif
        }

#if CVR_STEAMVR
        void CognitiveVR_Manager_PoseEventHandler(Valve.VR.EVREventType evrevent)
        {
            if (evrevent == Valve.VR.EVREventType.VREvent_ChaperoneDataHasChanged)
            {
                if (Valve.VR.OpenVR.Chaperone.AreBoundsVisible())
                {
                    chaperoneGUID = System.Guid.NewGuid().ToString();
                    Instrumentation.Transaction("cvr.boundary.visible", chaperoneGUID).begin();
                    Util.logDebug("chaperone visible");
                }
                else
                {
                    Instrumentation.Transaction("cvr.boundary.visible", chaperoneGUID).end();
                }
            }
        }
#endif

#if CVR_OCULUS
        string transactionID;
        void Update()
        {
            Debug.Log(OVRManager.boundary.GetVisible());

            if (OVRManager.boundary.GetVisible() && string.IsNullOrEmpty(transactionID))
            {
                transactionID = System.Guid.NewGuid().ToString();
                Instrumentation.Transaction("cvr.boundary.visible", transactionID).begin();

            }
            if (!OVRManager.boundary.GetVisible() && !string.IsNullOrEmpty(transactionID))
            {
                Instrumentation.Transaction("cvr.boundary.visible", transactionID).end();
                transactionID = string.Empty;
            }

            //if boundary.getvisible doesn't work, could test points
            //OVRBoundary.BoundaryTestResult result = OVRManager.boundary.TestPoint(Vector3.zero, OVRBoundary.BoundaryType.PlayArea);
        }
#endif

        public static string GetDescription()
        {
            return "Sends transaction when SteamVR Chaperone or Oculus Guardian becomes visible and becomes hidden";
        }
    }
}