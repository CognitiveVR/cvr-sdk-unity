using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// samples height of a player's HMD. average is assumed to be roughly player's eye height
/// </summary>

namespace CognitiveVR.Components
{
    public class HMDHeight: CognitiveVRAnalyticsComponent
    {
        [DisplaySetting(5,100)]
        [Tooltip("Number of samples taken. The average is assumed to be HMD height")]
        public int SampleCount = 50;
        public float Interval = 1;



        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);

            StartCoroutine(Tick());
        }

        IEnumerator Tick()
        {
            int samples = 0;
            float hmdAccumHeight = 0;
            YieldInstruction wait = new WaitForSeconds(Interval);

            while (samples < SampleCount)
            {
                yield return wait;
                hmdAccumHeight += CognitiveVR_Manager.HMD.localPosition.y;
                samples++;
            }

            float averageHeight = hmdAccumHeight / samples;
            Core.UpdateSessionState(new Dictionary<string, object> { { "cvr.height", averageHeight } });
        }

        public static string GetDescription()
        {
            return "Samples the height of a player's HMD. Average is assumed to be player's eye height";
        }

        public static bool GetWarning()
        {
#if (!CVR_OCULUS && !CVR_STEAMVR) || UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }
    }
}