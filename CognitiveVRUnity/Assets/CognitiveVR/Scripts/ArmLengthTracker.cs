using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// samples distances the the HMD to the player's arm. max is assumed to be roughly player arm length
/// this only starts tracking when the player has pressed the Steam Controller Trigger
/// </summary>

namespace CognitiveVR
{
    public class ArmLengthTracker : CognitiveVRAnalyticsComponent
    {
        float maxSqrDistance;
        int sampleCount = 50;
        int samples = 0;

#if CVR_STEAMVR
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);

            CognitiveVR_Manager.OnUpdate += CognitiveVR_Manager_OnUpdate;
        }

        public Valve.VR.VRControllerState_t controllerState;
        private void CognitiveVR_Manager_OnUpdate()
        {
            var system = Valve.VR.OpenVR.System;
            if (system != null && system.GetControllerState(0, ref controllerState))
            {
                ulong trigger = controllerState.ulButtonPressed & (1UL << ((int)Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger));
                if (trigger > 0L)
                {
                    CognitiveVR_Manager.OnTick += CognitiveVR_Manager_OnTick;
                    CognitiveVR_Manager.OnUpdate -= CognitiveVR_Manager_OnUpdate;
                }
            }
        }
#endif

    private void CognitiveVR_Manager_OnTick()
        {
            if (samples < sampleCount)
            {
                maxSqrDistance = Mathf.Max(Vector3.SqrMagnitude(CognitiveVR_Manager.GetController(0).position - CognitiveVR_Manager.HMD.position));
                samples++;
                if (samples >= sampleCount)
                {
                    Util.logDebug("arm length " + maxSqrDistance);
                    Instrumentation.updateUserState(new Dictionary<string, object> { { "armlength", Mathf.Sqrt(maxSqrDistance) } });
                    CognitiveVR_Manager.OnTick -= CognitiveVR_Manager_OnTick;
                }
            }
        }

        public static string GetDescription()
        {
            return "Samples distances from the HMD to the player's controller. Max is assumed to be roughly player arm length. This only starts tracking when the player has pressed the Steam Controller Trigger";
        }
    }
}