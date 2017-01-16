using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// sends transactions when a tracked device (likely a controller, but could also be headset or lighthouse) loses visibility (isvalid) or is disconnected/loses power (disconnected)
/// </summary>

namespace CognitiveVR.Components
{
    public class OcclusionEvent : CognitiveVRAnalyticsComponent
    {


        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);

#if CVR_STEAMVR
            CognitiveVR_Manager.PoseUpdateEvent += CognitiveVR_Manager_PoseUpdateHandler;
#elif CVR_OCULUS
            OVRManager.TrackingAcquired += OVRManager_TrackingAcquired;
            OVRManager.TrackingLost += OVRManager_TrackingLost;
#endif
        }

#if CVR_OCULUS
        string rRouchGUID;
        string lTouchGUID;
        void Update()
        {
            if (!OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch) && string.IsNullOrEmpty(rRouchGUID))
            {
                rRouchGUID = System.Guid.NewGuid().ToString();
                Instrumentation.Transaction("cvr.tracking", rRouchGUID).setProperty("device","right controller").setProperty("visible",false).begin();
            }
            if (OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch) && !string.IsNullOrEmpty(rRouchGUID))
            {
                Instrumentation.Transaction("cvr.tracking", rRouchGUID).setProperty("device","right controller").setProperty("visible", true).end();
                rRouchGUID = string.Empty;
            }

            if (!OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch) && string.IsNullOrEmpty(lTouchGUID))
            {
                lTouchGUID = System.Guid.NewGuid().ToString();
                Instrumentation.Transaction("cvr.tracking", lTouchGUID).setProperty("device", "left controller").setProperty("visible", false).begin();
            }
            if (OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch) && !string.IsNullOrEmpty(lTouchGUID))
            {
                Instrumentation.Transaction("cvr.tracking", lTouchGUID).setProperty("device", "left controller").setProperty("visible", true).end();
                lTouchGUID = string.Empty;
            }
        }

        string hmdGUID;
        private void OVRManager_TrackingLost()
        {
            hmdGUID = System.Guid.NewGuid().ToString();
            Instrumentation.Transaction("cvr.tracking", hmdGUID).setProperty("device", "hmd").setProperty("visible", false).begin();
        }

        private void OVRManager_TrackingAcquired()
        {
            Instrumentation.Transaction("cvr.tracking", hmdGUID).setProperty("device", "hmd").setProperty("visible", true).end();
            hmdGUID = string.Empty;
        }
#endif

#if CVR_STEAMVR
        string chaperoneGUID;
        List<TrackedDevice> Devices = new List<TrackedDevice>();

        [System.Serializable]
        class TrackedDevice
        {
            public int deviceID;
            public string ValidTransID = string.Empty;
            public string ConnectedTransID = string.Empty;
        }

        private void CognitiveVR_Manager_PoseUpdateHandler(Valve.VR.TrackedDevicePose_t[] args)
        {
            //var poses = (Valve.VR.TrackedDevicePose_t[])args[0];
            for (int i = 0; i < 16; i++)
            {
                if (args.Length <= i) { break; }

                if (args[i].bDeviceIsConnected && args[i].bPoseIsValid)
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
                if (args[Devices[j].deviceID].bPoseIsValid && Devices[j].ValidTransID != string.Empty)
                {
                    Instrumentation.Transaction("cvr.tracking", Devices[j].ValidTransID).setProperty("device", GetViveDeviceName(Devices[j].deviceID)).setProperty("visible", true).end();
                    Devices[j].ValidTransID = string.Empty;
                }
                if (!args[Devices[j].deviceID].bPoseIsValid && Devices[j].ValidTransID == string.Empty)
                {
                    Devices[j].ValidTransID = System.Guid.NewGuid().ToString();
                    Instrumentation.Transaction("cvr.tracking", Devices[j].ValidTransID).setProperty("device", GetViveDeviceName(Devices[j].deviceID)).setProperty("visible", false).begin();
                }

                if (args[Devices[j].deviceID].bDeviceIsConnected && Devices[j].ConnectedTransID != string.Empty)
                {
                    Instrumentation.Transaction("cvr.tracking", Devices[j].ConnectedTransID).setProperty("device", GetViveDeviceName(Devices[j].deviceID)).setProperty("connected", true).end();
                    Devices[j].ConnectedTransID = string.Empty;
                }
                if (!args[Devices[j].deviceID].bDeviceIsConnected && Devices[j].ConnectedTransID == string.Empty)
                {
                    Devices[j].ConnectedTransID = System.Guid.NewGuid().ToString();
                    Instrumentation.Transaction("cvr.tracking", Devices[j].ConnectedTransID).setProperty("device", GetViveDeviceName(Devices[j].deviceID)).setProperty("connected", false).begin();
                }
            }
        }

        string GetViveDeviceName(int deviceID)
        {
            if (deviceID == 0)
            {
                return "hmd";
            }
            CognitiveVR_Manager.ControllerInfo cont = CognitiveVR_Manager.GetControllerInfo(deviceID);

            if (cont != null) { return cont.isRight ? "right controller" : "left controller"; }

            return "unknown id " + deviceID;
        }
#endif

        public static string GetDescription()
        {
            return "Sends transactions when a tracked device (likely a controller, but could also be headset or lighthouse) loses visibility (visible) or is disconnected/loses power (connected)";
        }

        void OnDestroy()
        {
#if CVR_STEAMVR
            CognitiveVR_Manager.PoseUpdateEvent -= CognitiveVR_Manager_PoseUpdateHandler;
#endif
#if CVR_OCULUS
            OVRManager.TrackingAcquired -= OVRManager_TrackingAcquired;
            OVRManager.TrackingLost -= OVRManager_TrackingLost;
#endif
        }
    }
}