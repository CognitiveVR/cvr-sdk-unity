using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if CVR_STEAMVR || CVR_STEAMVR2
using Valve.VR;
#endif

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
        public float StartDelay = 5; //this is an additional start delay after cognitivevr_manager has initialized
        public float Interval = 1;

#if CVR_STEAMVR
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);

            StartCoroutine(Tick());
        }
#endif

#if CVR_STEAMVR2

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            StartCoroutine(Tick());
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
                StartCoroutine(Tick());
                CognitiveVR_Manager.UpdateEvent -= CognitiveVR_Manager_OnUpdate;
            }
        }
#endif

        IEnumerator Tick()
        {
            //TODO wait for input
            yield return new WaitForSeconds(StartDelay);

            int samples = 0;
            float maxSqrDistance = 0;

            while (samples < SampleCount)
            {
                yield return new WaitForSeconds(Interval);

                var left = CognitiveVR_Manager.GetControllerInfo(false);
                if (left != null && left.transform != null && left.connected && left.visible)
                {
                    maxSqrDistance = Mathf.Max(maxSqrDistance, Vector3.SqrMagnitude(left.transform.position - CognitiveVR_Manager.HMD.position));
                }

                var right = CognitiveVR_Manager.GetControllerInfo(true);
                if (right != null && right.transform != null && right.connected && right.visible)
                {
                    maxSqrDistance = Mathf.Max(maxSqrDistance, Vector3.SqrMagnitude(right.transform.position - CognitiveVR_Manager.HMD.position));
                }

                samples++;
            }

            if (maxSqrDistance > 0)
            {
                //send arm length
                float distance = Mathf.Sqrt(maxSqrDistance);
                Core.UpdateSessionState(new Dictionary<string, object> { { "cvr.armlength", distance } });
            }
        }

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
#if CVR_OCULUS
            CognitiveVR_Manager.UpdateEvent -= CognitiveVR_Manager_OnUpdate;
#endif
        }


    }
}