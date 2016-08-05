using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when SteamVR Chaperone is visible
/// </summary>

namespace CognitiveVR
{
    public class ChaperoneTracker : CognitiveVRAnalyticsComponent
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
                Instrumentation.Transaction("chaperone", chaperoneGUID).begin();
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
                    Instrumentation.Transaction("chaperone", chaperoneGUID).begin();
                    Util.logDebug("chaperone visible");
                }
                else
                {
                    Instrumentation.Transaction("chaperone", chaperoneGUID).end();
                }
            }
        }
#endif

        public static string GetDescription()
        {
            return "Sends transaction when SteamVR Chaperone is becomes visible and is becomes hidden";
        }
    }
}