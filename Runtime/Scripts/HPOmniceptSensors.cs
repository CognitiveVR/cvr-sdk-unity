using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D.Components
{
    // Takes care of handling sensor data from the 
    // HP Omnicept HMD
    // Must have HPGlia Package imported into the project
    public class HPOmniceptSensors : AnalyticsComponentBase
    {
        double pupillometryUpdateTime = 0;    // to check how much time elpased since last data point update 
        int pupillometryUpdateIntervalMilliSeconds = 100; // update every 100ms

        private void RecordEyePupillometry(HP.Omnicept.Messaging.Messages.EyeTracking data)
        {
            double hmdTimestamp = (double)data.Timestamp.SystemTimeMicroSeconds / 1000000.0;
            float minimumConfidenceThreshold = 0.5f;
            float eyeOpenPupilDilationThreshold = 1.5f;
            string hpLeftEyeDiameterTag = "HP.Left Pupil Diameter";
            string hpRightEyeDiameterTag = "HP.Right Pupil Diameter";
            
            if (hmdTimestamp > pupillometryUpdateTime)
            {
                pupillometryUpdateTime = hmdTimestamp + (pupillometryUpdateIntervalMilliSeconds / 1000); 
                
                if (data.LeftEye.PupilDilationConfidence > minimumConfidenceThreshold && data.LeftEye.PupilDilation > eyeOpenPupilDilationThreshold)
                {
                    SensorRecorder.RecordDataPoint(hpLeftEyeDiameterTag, data.LeftEye.PupilDilation, hmdTimestamp);
                }
                                
                if (data.RightEye.PupilDilationConfidence > minimumConfidenceThreshold && data.RightEye.PupilDilation > eyeOpenPupilDilationThreshold)
                {
                    SensorRecorder.RecordDataPoint(hpRightEyeDiameterTag, data.RightEye.PupilDilation, hmdTimestamp);
                }
            }
        }


    }
}