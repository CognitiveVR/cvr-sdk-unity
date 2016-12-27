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
            base.CognitiveVR_Init(initError);

            CognitiveVR_Manager.OnTick += CognitiveVR_Manager_OnTick;
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
                    Instrumentation.updateUserState(new Dictionary<string, object> { { "height", averageHeight } });
                    CognitiveVR_Manager.OnTick -= CognitiveVR_Manager_OnTick;
                }
            }
        }

        public static string GetDescription()
        {
            return "Samples the height of a player's HMD. Average is assumed to be player's eye height";
        }

        void OnDestroy()
        {
            CognitiveVR_Manager.OnTick -= CognitiveVR_Manager_OnTick;
        }
    }
}