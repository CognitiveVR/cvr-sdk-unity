﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// samples height of a player's HMD. average is assumed to be roughly player's eye height
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/HMD Height")]
    public class HMDHeight : AnalyticsComponentBase
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

        private const float SAMPLE_INTERVAL = 5;
        float[] heights;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();

            heights = new float[SampleCount];

            StartCoroutine(Tick());
        }

        IEnumerator Tick()
        {
            yield return new WaitForSeconds(StartDelay);

            float hmdAccumHeight = 0;
            YieldInstruction wait = new WaitForSeconds(Interval);

            //median
            for (int i = 0; i < SampleCount; i++)
            {
                yield return wait;

                hmdAccumHeight += GameplayReferences.HMD.localPosition.y;
                heights[i] = GameplayReferences.HMD.localPosition.y;
                if (i % (SampleCount/SAMPLE_INTERVAL) == 0)
                {
                    RecordAndSendMedian(heights, i);
                }
            }
        }

        private void RecordAndSendMedian(float[] heights, int lastIndex)
        {
            float medianHeight = Median(heights, lastIndex);
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsSpatialDataAllowed)
#endif
            {
                Cognitive3D_Manager.SetParticipantProperty("height", medianHeight * 100 + ForeheadHeight * 100);

            }
        }

        private float Median(float[] items, int lastIndex)
        {
            float[] tempArray = new float[lastIndex + 1];
            for (int j = 0; j < lastIndex; j++)
            {
                tempArray[j] = items[j];
            }
            var i = (int)Mathf.Ceil((float)(lastIndex) / 2);
            if (i >= 0)
            {
                System.Array.Sort(tempArray);
                return tempArray[i];
            }
            return 0;
        }

        public override string GetDescription()
        {
#if C3D_PICOVR
            return "Samples the height of a player's HMD. Average is assumed to be player's eye height\nPvr_UnitySDKManager.TrackingOrigin MUST be set as FloorLevel!";
#else
            return "Samples the height of a player's HMD. Average is assumed to be player's eye height";
#endif
        }

        public override bool GetWarning()
        {
#if C3D_PICOVR
            var pvrManager = GameplayReferences.Pvr_UnitySDKManager;
            if (pvrManager != null)
            {
                if (pvrManager.TrackingOrigin == Pvr_UnitySDKAPI.TrackingOrigin.EyeLevel)
                {
                    return true;
                }
            }
#endif
            return false;
        }
    }
}