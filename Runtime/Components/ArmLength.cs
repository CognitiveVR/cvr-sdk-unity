using UnityEngine;
using System.Collections;
#if C3D_STEAMVR || C3D_STEAMVR2
using Valve.VR;
#endif

/// <summary>
/// samples distances the the HMD to the player's arm. max is assumed to be roughly player arm length
/// this only starts tracking when the player has pressed a button/trigger/grip
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Arm Length")]
    public class ArmLength : AnalyticsComponentBase
    {
        private readonly int SampleCount = 50;
        private readonly float Interval = 1;
        private const float SAMPLE_INTERVAL = 10;
        private readonly float EyeToShoulderHeight = 0.186f; //meters
        Transform tempInfo = null;

        protected override void OnSessionBegin()
        {
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

                if (GameplayReferences.IsInputDeviceValid(UnityEngine.XR.XRNode.LeftHand))
                {
                    if (GameplayReferences.GetControllerTransform(false, out tempInfo))
                    {
                        maxSqrDistance = Mathf.Max(maxSqrDistance, Vector3.SqrMagnitude(tempInfo.transform.position - (GameplayReferences.HMD.position - GameplayReferences.HMD.up * EyeToShoulderHeight)));
                        includedSample = true;
                    }
                }

                if (GameplayReferences.IsInputDeviceValid(UnityEngine.XR.XRNode.RightHand))
                {
                    if (GameplayReferences.GetControllerTransform(true, out tempInfo))
                    {
                        maxSqrDistance = Mathf.Max(maxSqrDistance, Vector3.SqrMagnitude(tempInfo.transform.position - (GameplayReferences.HMD.position - GameplayReferences.HMD.up * EyeToShoulderHeight)));
                        includedSample = true;
                    }
                }

                if (includedSample)
                {
                    samples++;
                }

                if (Mathf.Approximately(samples % SAMPLE_INTERVAL, 0.0f))
                {
                    SendMaxDistance(maxSqrDistance);
                }
            }
        }

        private void SendMaxDistance(float maxDistance)
        {
            if (maxDistance > 0)
            {
                //send arm length
                float distance = Mathf.Sqrt(maxDistance);
#if XRPF
                if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsSpatialDataAllowed)
#endif
                {
                    //dashboard expects centimeters
                    Cognitive3D_Manager.SetParticipantProperty("armlength", distance * 100);
                }
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
