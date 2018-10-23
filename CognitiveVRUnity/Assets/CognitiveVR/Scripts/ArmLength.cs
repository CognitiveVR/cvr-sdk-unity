using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// samples distances the the HMD to the player's arm. max is assumed to be roughly player arm length
/// this only starts tracking when the player has pressed the Steam Controller Trigger
/// </summary>

namespace CognitiveVR.Components
{
    public class ArmLength : CognitiveVRAnalyticsComponent
    {
        [DisplaySetting(5,100)]
        [Tooltip("Number of samples taken. The max is assumed to be maximum arm length")]
        public int SampleCount = 50;

#if CVR_STEAMVR || CVR_OCULUS
        float maxSqrDistance;
        int samples = 0;
#endif
#if CVR_STEAMVR
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);

            CognitiveVR_Manager.UpdateEvent += CognitiveVR_Manager_OnUpdate;
        }

        public Valve.VR.VRControllerState_t controllerState;
        uint stateSize=0;
        private void CognitiveVR_Manager_OnUpdate()
        {
            var system = Valve.VR.OpenVR.System;
            if (system != null && system.GetControllerState(0, ref controllerState, stateSize )) //1.2. for steam 1.1, remove statesize variable
            {
                ulong trigger = controllerState.ulButtonPressed & (1UL << ((int)Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger));
                if (trigger > 0L)
                {
                    CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_OnTick;
                    CognitiveVR_Manager.UpdateEvent -= CognitiveVR_Manager_OnUpdate;
                }
            }
        }

        private void CognitiveVR_Manager_OnTick()
        {
            if (CognitiveVR_Manager.GetController(0) == null){return;}

            if (samples < SampleCount)
            {
                maxSqrDistance = Mathf.Max(Vector3.SqrMagnitude(CognitiveVR_Manager.GetController(0).position - CognitiveVR_Manager.HMD.position));

                samples++;
                if (samples >= SampleCount)
                {
                    float distance = Mathf.Sqrt(maxSqrDistance);
                    Util.logDebug("arm length " + distance);
                    Core.UpdateSessionState(new Dictionary<string, object> { { "cvr.armlength", distance } });
                    CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_OnTick;
                }
            }
        }
#endif

#if CVR_OCULUS
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);
            CognitiveVR_Manager.UpdateEvent += CognitiveVR_Manager_OnUpdate;
        }

        private void CognitiveVR_Manager_OnUpdate()
        {
            if (OVRInput.GetDown(OVRInput.Button.Any))
            {
                CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_OnTick;
                CognitiveVR_Manager.UpdateEvent -= CognitiveVR_Manager_OnUpdate;
            }
        }

        private void CognitiveVR_Manager_OnTick()
        {
            if (samples < SampleCount)
            {
                maxSqrDistance = Mathf.Max(maxSqrDistance, OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch).sqrMagnitude, OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch).sqrMagnitude);

                samples++;
                if (samples >= SampleCount)
                {
                    float distance = Mathf.Sqrt(maxSqrDistance);
                    Util.logDebug("arm length " + distance);
                    Core.UpdateSessionState(new Dictionary<string, object> { { "cvr.armlength", distance } });
                    CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_OnTick;
                }
            }
        }
#endif
        public static string GetDescription()
        {
            return "Samples distances from the HMD to the player's controller. Max is assumed to be roughly player arm length. This only starts tracking when the player has pressed the Steam Controller Trigger\nRequires SteamVR or Oculus Touch controllers";
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
#if CVR_STEAMVR || CVR_OCULUS
            CognitiveVR_Manager.UpdateEvent -= CognitiveVR_Manager_OnUpdate;
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_OnTick;
#endif
        }


    }
}