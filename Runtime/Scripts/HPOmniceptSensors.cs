using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if C3D_OMNICEPT
using HP.Omnicept.Unity;
#endif

namespace Cognitive3D.Components
{
    // Takes care of handling sensor data from the 
    // HP Omnicept HMD
    // Must have HPGlia Package imported into the project
    [AddComponentMenu("Cognitive3D/Components/HP Omnicept Sensors")]
    public class HPOmniceptSensors : AnalyticsComponentBase
    {
#if C3D_OMNICEPT
        
        private double microSecondsToSeconds = 1000000;
        private double milliSecondsToSeconds = 1000;
        /*
        * Called on first frame update, sets up properties and event listeners
        */
        protected override void OnSessionBegin()
        {        
            if (GameplayReferences.GliaBehaviour != null)
            {
                GameplayReferences.GliaBehaviour.OnEyeTracking.AddListener(RecordEyePupillometry);
                GameplayReferences.GliaBehaviour.OnHeartRate.AddListener(RecordHeartRate);
                GameplayReferences.GliaBehaviour.OnCognitiveLoad.AddListener(RecordCognitiveLoad);
                GameplayReferences.GliaBehaviour.OnHeartRateVariability.AddListener(RecordHeartRateVariability);
            }
        }


        double pupillometryUpdateTime = 0;    // to check how much time elpased since last data point update 
        int pupillometryUpdateIntervalMilliSeconds = 100; // update every 100ms

        private void RecordEyePupillometry(HP.Omnicept.Messaging.Messages.EyeTracking data)
        {
            double hmdTimestamp = (double)data.Timestamp.SystemTimeMicroSeconds / microSecondsToSeconds;
            const float minimumConfidenceThreshold = 0.5f;
            const float eyeOpenPupilDilationThreshold = 1.5f;
            const string hpLeftEyeDiameterTag = "HP.Left Pupil Diameter";
            const string hpRightEyeDiameterTag = "HP.Right Pupil Diameter";
            if (hmdTimestamp > pupillometryUpdateTime)
            {
                pupillometryUpdateTime = hmdTimestamp + (pupillometryUpdateIntervalMilliSeconds / milliSecondsToSeconds);
                if (data.LeftEye.PupilDilationConfidence > minimumConfidenceThreshold && data.LeftEye.PupilDilation > eyeOpenPupilDilationThreshold)
                {
#if XRPF
                    if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsBioDataAllowed)
#endif
                    {
                        SensorRecorder.RecordDataPoint(hpLeftEyeDiameterTag, data.LeftEye.PupilDilation, hmdTimestamp);
                    }
                }
                if (data.RightEye.PupilDilationConfidence > minimumConfidenceThreshold && data.RightEye.PupilDilation > eyeOpenPupilDilationThreshold)
                {
#if XRPF
                    if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsBioDataAllowed)
#endif
                    {
                        SensorRecorder.RecordDataPoint(hpRightEyeDiameterTag, data.RightEye.PupilDilation, hmdTimestamp);
                    }
                }
            }
        }

        // Updates every 5 seconds by HP
        private void RecordHeartRate(HP.Omnicept.Messaging.Messages.HeartRate data)
        {
            const string hpHeartRateTag = "HP.HeartRate";
            double hmdTimestamp = (double)data.Timestamp.SystemTimeMicroSeconds / microSecondsToSeconds;
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsBioDataAllowed)
#endif
            {
                SensorRecorder.RecordDataPoint(hpHeartRateTag, data.Rate, hmdTimestamp);
            }
        }

        // Updates every 6 seconds by HP
        private void RecordHeartRateVariability(HP.Omnicept.Messaging.Messages.HeartRateVariability data)
        {
            const string hpHeartRateVariabilityTag = "HP.HeartRate.Variability";
            double hmdTimestamp = (double)data.Timestamp.SystemTimeMicroSeconds / microSecondsToSeconds;
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsBioDataAllowed)
#endif
            {
                SensorRecorder.RecordDataPoint(hpHeartRateVariabilityTag, data.Sdnn, hmdTimestamp);
            }
        }

        // Updates every 1 second by HP
        private void RecordCognitiveLoad(HP.Omnicept.Messaging.Messages.CognitiveLoad data)
        {
            const string hpCognitiveLoadTag = "HP.CognitiveLoad";
            const string hpCognitiveLoadConfidenceTag = "HP.CognitiveLoad.Confidence";
            double hmdTimestamp = (double)data.Timestamp.SystemTimeMicroSeconds / microSecondsToSeconds;
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsBioDataAllowed)
#endif
            {
                SensorRecorder.RecordDataPoint(hpCognitiveLoadTag, data.CognitiveLoadValue, hmdTimestamp);
                SensorRecorder.RecordDataPoint(hpCognitiveLoadConfidenceTag, data.StandardDeviation, hmdTimestamp);
            }
         }
        public override string GetDescription()
        {
            return "Records Heart Rate, Heart Rate Variability, Pupillometry and Cognitive Load on an interval";
        }
#else
        public override string GetDescription()
        {
            return "Requires HP Omnicept Runtime to be selected in Scene Setup Window";
        }

        public override bool GetWarning()
        {
            return true;
        }
#endif
    }
}