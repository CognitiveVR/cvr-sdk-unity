using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// sends transactions when a tracked device (likely a controller, but could also be headset or lighthouse) loses visibility (isvalid) or is disconnected/loses power (disconnected)
/// </summary>

namespace CognitiveVR
{
    public class OcclusionTracker : CognitiveVRAnalyticsComponent
    {
        string chaperoneGUID;

        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);

#if CVR_STEAMVR
            CognitiveVR_Manager.OnPoseUpdate += CognitiveVR_Manager_PoseUpdateHandler;
#elif CVR_OCULUS
            OVRManager.TrackingAcquired += OVRManager_TrackingAcquired;
            OVRManager.TrackingLost += OVRManager_TrackingLost;
#endif
        }

#if CVR_OCULUS
        string hmdGUID;
        private void OVRManager_TrackingLost()
        {
            Instrumentation.Transaction("Tracking", hmdGUID).setProperty("Device", "HMD").setProperty("visible", false).end();
            hmdGUID = string.Empty;
        }

        private void OVRManager_TrackingAcquired()
        {
            hmdGUID = System.Guid.NewGuid().ToString();
            Instrumentation.Transaction("Tracking", hmdGUID).setProperty("Device", "HMD").setProperty("visible", true).begin();
        }
#endif

#if CVR_STEAMVR
        List<TrackedDevice> Devices = new List<TrackedDevice>();

        [System.Serializable]
        class TrackedDevice
        {
            public int deviceID;
            public string ValidTransID = string.Empty;
            public string ConnectedTransID = string.Empty;

        }

        private void CognitiveVR_Manager_PoseUpdateHandler(params object[] args)
        {
            var poses = (Valve.VR.TrackedDevicePose_t[])args[0];
            for (int i = 0; i < 16; i++)
            {
                if (poses.Length <= i) { break; }

                if (poses[i].bDeviceIsConnected && poses[i].bPoseIsValid)
                {
                    bool foundMatchingDevice = false;
                    for (int j = 0; j < Devices.Count; j++)
                    {
                        if (Devices[j].deviceID == i) { foundMatchingDevice = true; break; }
                    }
                    if (!foundMatchingDevice)
                        Devices.Add(new TrackedDevice() { deviceID = i });
                }
            }

            for (int j = 0; j < Devices.Count; j++)
            {
                if (!poses[Devices[j].deviceID].bPoseIsValid)
                {
                    if (Devices[j].ValidTransID == string.Empty)
                    {
                        Devices[j].ValidTransID = System.Guid.NewGuid().ToString();
                        Instrumentation.Transaction("Tracking", Devices[j].ValidTransID).setProperty("Device", Devices[j]).setProperty("visible", false).begin();
                    }
                }
                else if (Devices[j].ValidTransID != string.Empty)
                {
                    Instrumentation.Transaction("Tracking", Devices[j].ValidTransID).setProperty("Device", Devices[j]).setProperty("visible", true).end();
                    Devices[j].ValidTransID = string.Empty;
                }

                if (!poses[Devices[j].deviceID].bDeviceIsConnected)
                {
                    if (Devices[j].ValidTransID == string.Empty)
                    {
                        Devices[j].ConnectedTransID = System.Guid.NewGuid().ToString();
                        Instrumentation.Transaction("Tracking", Devices[j].ConnectedTransID).setProperty("Device", Devices[j]).setProperty("connected", false).begin();
                    }
                }
                else if (Devices[j].ConnectedTransID != string.Empty)
                {
                    Instrumentation.Transaction("Tracking", Devices[j].ConnectedTransID).setProperty("Device", Devices[j]).setProperty("connected", true).end();
                    Devices[j].ConnectedTransID = string.Empty;
                }
            }
        }
#endif

        public static string GetDescription()
        {
            return "Sends transactions when a tracked device (likely a controller, but could also be headset or lighthouse) loses visibility (visible) or is disconnected/loses power (connected)\nOn Oculus, this will also happen if the HMD moves out of the tracking frustrum";
        }
    }
}