using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if CVR_STEAMVR || CVR_STEAMVR2
using Valve.VR;
#endif

/// <summary>
/// samples distances the the HMD to the player's arm. max is assumed to be roughly player arm length
/// this only starts tracking when the player has pressed a button/trigger/grip
/// </summary>

namespace CognitiveVR.Components
{
    [AddComponentMenu("Cognitive3D/Components/Arm Length")]
    public class ArmLength : CognitiveVRAnalyticsComponent
    {
        [ClampSetting(5, 100)]
        [Tooltip("Number of samples taken. The max is assumed to be maximum arm length")]
        public int SampleCount = 50;
        [ClampSetting(0.1f)]
        public float Interval = 1;

        [ClampSetting(0, 50)]
        [Tooltip("Distance from HMD to average shoulder height")]
        public float EyeToShoulderHeight = 0.186f; //meters

        GameplayReferences.ControllerInfo tempInfo = null;

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            StartCoroutine(Tick());
        }

        IEnumerator Tick()
        {
            int samples = 0;
            float maxSqrDistance = 0;

            var wait = new WaitForSeconds(Interval);

            while (samples < SampleCount)
            {
                yield return wait;

                bool includedSample = false;

                //if left controller is active, record max distance
                if (GameplayReferences.GetControllerInfo(false, out tempInfo))
                {
                    if (tempInfo.connected && tempInfo.visible)
                    {
                        maxSqrDistance = Mathf.Max(maxSqrDistance, Vector3.SqrMagnitude(tempInfo.transform.position - (GameplayReferences.HMD.position - GameplayReferences.HMD.up * EyeToShoulderHeight)));
                        includedSample = true;
                    }
                }

                //if right controller is active, record max distance
                if (GameplayReferences.GetControllerInfo(true, out tempInfo))
                {
                    if (tempInfo.connected && tempInfo.visible)
                    {
                        maxSqrDistance = Mathf.Max(maxSqrDistance, Vector3.SqrMagnitude(tempInfo.transform.position - (GameplayReferences.HMD.position - GameplayReferences.HMD.up * EyeToShoulderHeight)));
                        includedSample = true;
                    }
                }

                if (includedSample)
                    samples++;
            }

            if (maxSqrDistance > 0)
            {
                //send arm length
                float distance = Mathf.Sqrt(maxSqrDistance);
                //dashboard expects centimeters
                Core.SetParticipantProperty("armlength", distance * 100);
            }
        }

        public override string GetDescription()
        {
            if (GameplayReferences.SDKSupportsControllers)
            {
                return "Samples distances from the HMD to the player's controller. Max is assumed to be roughly player arm length.";
            }
            return "Selected runtime does not support this component";
        }

        public override bool GetWarning()
        {
            if (GameplayReferences.SDKSupportsControllers)
                return false;
            return true;
        }
    }
}
