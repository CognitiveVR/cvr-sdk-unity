using UnityEngine;
using System.Collections;
using UnityEngine.XR;
#if C3D_STEAMVR || C3D_STEAMVR2
using Valve.VR;
#endif

/// <summary>
/// samples distances the the HMD to the player's arm. max is assumed to be roughly player arm length
/// </summary>

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Arm Length")]
    public class ArmLength : AnalyticsComponentBase
    {
        private readonly int SampleCount = 50;
        private readonly float Interval = 1;
        private const float SAMPLE_INTERVAL = 10;
        private readonly float EyeToShoulderHeight = 0.186f; //meters
        private readonly float MIN_ACCEPTABLE_ARMLENGTH = 0.01f;
        private readonly float MAX_ACCEPTABLE_ARMLENGTH = 1.25f; // longest arm span in guinness record is approx 250cm
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

                if (GameplayReferences.IsInputDeviceValid(XRNode.LeftHand) && GameplayReferences.IsControllerTracking(XRNode.LeftHand))
                {
                    if (GameplayReferences.GetControllerTransform(false, out tempInfo))
                    {
                        var currentDistance = Vector3.SqrMagnitude(tempInfo.transform.position - (GameplayReferences.HMD.position - GameplayReferences.HMD.up * EyeToShoulderHeight));
                        if (currentDistance >= MIN_ACCEPTABLE_ARMLENGTH && currentDistance <= MAX_ACCEPTABLE_ARMLENGTH)
                        {
                            maxSqrDistance = Mathf.Max(maxSqrDistance, currentDistance);
                            includedSample = true;
                        }
                    }
                }

                if (GameplayReferences.IsInputDeviceValid(XRNode.RightHand) && GameplayReferences.IsControllerTracking(XRNode.RightHand))
                {
                    if (GameplayReferences.GetControllerTransform(true, out tempInfo))
                    {
                        var currentDistance = Vector3.SqrMagnitude(tempInfo.transform.position - (GameplayReferences.HMD.position - GameplayReferences.HMD.up * EyeToShoulderHeight));
                        if (currentDistance >= MIN_ACCEPTABLE_ARMLENGTH && currentDistance <= MAX_ACCEPTABLE_ARMLENGTH)
                        {
                            maxSqrDistance = Mathf.Max(maxSqrDistance, currentDistance);
                            includedSample = true;
                        }
                    }
                }

                if (includedSample)
                {
                    samples++;
                    if (Mathf.Approximately(samples % SAMPLE_INTERVAL, 0.0f))
                    {
                        SendMaxDistance(maxSqrDistance);
                    }
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
