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

        int samples = 0;
        float hmdAccumHeight;

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);

            CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_OnTick;
        }

        private void CognitiveVR_Manager_OnTick()
        {
            if (CognitiveVR_Manager.HMD == null) { return; }
            if (samples < SampleCount)
            {
                hmdAccumHeight += CognitiveVR_Manager.HMD.position.y;
                samples++;
                if (samples >= SampleCount)
                {
                    float averageHeight = hmdAccumHeight / samples;
                    Util.logDebug("head height " + averageHeight);
                    CognitiveVR_Manager.UpdateSessionState(new Dictionary<string, object> { { "cvr.height", averageHeight } });
                    CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_OnTick;
                }
            }
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

        void OnDestroy()
        {
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_OnTick;
        }
    }
}