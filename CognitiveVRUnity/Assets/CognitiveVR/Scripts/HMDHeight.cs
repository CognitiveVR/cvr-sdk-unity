using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// samples height of a player's HMD. average is assumed to be roughly player's eye height
/// </summary>

namespace CognitiveVR.Components
{
    [AddComponentMenu("Cognitive3D/Components/HMD Height")]
    public class HMDHeight : CognitiveVRAnalyticsComponent
    {
        [ClampSetting(5, 100)]
        [Tooltip("Number of samples taken. The median is assumed to be HMD height")]
        public int SampleCount = 50;

        [ClampSetting(0)]
        [Tooltip("number of seconds before starting to sample HMD height")]
        public float StartDelay = 10;

        [ClampSetting(1)]
        public float Interval = 1;

        [ClampSetting(0, 20)]
        [Tooltip("Distance from HMD Eye height to user's full height")]
        public float ForeheadHeight = 0.11f; //meters

        float[] heights;

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            base.CognitiveVR_Init(initError);

            heights = new float[SampleCount];

            StartCoroutine(Tick());
        }

        IEnumerator Tick()
        {
            yield return new WaitForSeconds(StartDelay);


            float hmdAccumHeight = 0;
            YieldInstruction wait = new WaitForSeconds(Interval);

#if CVR_PICONEO2EYE
            var sdkmanager = FindObjectOfType<Pvr_UnitySDKManager>();
            if (sdkmanager == null || sdkmanager.TrackingOrigin != Pvr_UnitySDKAPI.TrackingOrigin.FloorLevel)
            {
                CognitiveVR.Util.logError("Pvr_UnitySDKManager.TrackingOrigin MUST be set to FloorLevel to track HMD height correctly!");
                yield break;
            }
#endif

            //median
            for (int i = 0; i < SampleCount; i++)
            {
                yield return wait;
#if CVR_PICONEO2EYE
                heights[i] = Pvr_UnitySDKEyeManager.Instance.transform.localPosition.y;
                hmdAccumHeight += heights[i];
#else
                hmdAccumHeight += GameplayReferences.HMD.localPosition.y;
                heights[i] = GameplayReferences.HMD.localPosition.y;
#endif
            }

            float medianHeight = Median(heights);
            Core.SetParticipantProperty("height", medianHeight * 100 + ForeheadHeight * 100);
        }

        private float Median(float[] items)
        {
            var i = (int)Mathf.Ceil((float)(items.Length - 1) / 2);
            if (i >= 0)
            {
                System.Array.Sort(items);
                return items[i];
            }
            return 0;
        }

        public override string GetDescription()
        {
#if CVR_PICONEO2EYE
            return "Samples the height of a player's HMD. Average is assumed to be player's eye height\nPvr_UnitySDKManager.TrackingOrigin MUST be set as FloorLevel!";
#else
            return "Samples the height of a player's HMD. Average is assumed to be player's eye height";
#endif
        }

        public override bool GetWarning()
        {
#if CVR_OCULUS || CVR_STEAMVR || CVR_STEAMVR2 || CVR_NEURABLE || CVR_VARJO || CVR_TOBIIVR || CVR_PUPIL
            return false;
#else
            return true;
#endif
        }
    }
}