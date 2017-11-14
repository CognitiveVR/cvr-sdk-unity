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
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);

#if CVR_STEAMVR
            CognitiveVR_Manager.PoseUpdateEvent += CognitiveVR_Manager_PoseUpdateHandler; //1.2
            //CognitiveVR_Manager.PoseUpdateEvent += CognitiveVR_Manager_PoseUpdateEvent; //1.1
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
                rRouchGUID = Util.GetUniqueId();
                Instrumentation.Transaction("cvr.tracking", rRouchGUID).setProperty("device","right controller").setProperty("visible",false).begin();
            }
            if (OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch) && !string.IsNullOrEmpty(rRouchGUID))
            {
                Instrumentation.Transaction("cvr.tracking", rRouchGUID).setProperty("device","right controller").setProperty("visible", true).end();
                rRouchGUID = string.Empty;
            }

            if (!OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch) && string.IsNullOrEmpty(lTouchGUID))
            {
                lTouchGUID = Util.GetUniqueId();
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
            hmdGUID = Util.GetUniqueId();
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

        //steam 1.1
        private void CognitiveVR_Manager_PoseUpdateEvent(params object[] args)
        {
            CognitiveVR_Manager_PoseUpdateHandler((Valve.VR.TrackedDevicePose_t[])args[0]);
        }

        //steam 1.2
        private void CognitiveVR_Manager_PoseUpdateHandler(Valve.VR.TrackedDevicePose_t[] args)
        {
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
                    Devices[j].ValidTransID = Util.GetUniqueId();
                    Instrumentation.Transaction("cvr.tracking", Devices[j].ValidTransID).setProperty("device", GetViveDeviceName(Devices[j].deviceID)).setProperty("visible", false).begin();
                }

                if (args[Devices[j].deviceID].bDeviceIsConnected && Devices[j].ConnectedTransID != string.Empty)
                {
                    Instrumentation.Transaction("cvr.tracking", Devices[j].ConnectedTransID).setProperty("device", GetViveDeviceName(Devices[j].deviceID)).setProperty("connected", true).end();
                    Devices[j].ConnectedTransID = string.Empty;
                }
                if (!args[Devices[j].deviceID].bDeviceIsConnected && Devices[j].ConnectedTransID == string.Empty)
                {
                    Devices[j].ConnectedTransID = Util.GetUniqueId();
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

        public static bool GetWarning()
        {
#if (!CVR_OCULUS && !CVR_STEAMVR) || UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }

        public static string GetDescription()
        {
            return "Sends transactions when a tracked device (likely a controller, but could also be headset or lighthouse) loses visibility (visible) or is disconnected/loses power (connected)";
        }

        void OnDestroy()
        {
#if CVR_STEAMVR
            CognitiveVR_Manager.PoseUpdateEvent -= CognitiveVR_Manager_PoseUpdateHandler; //1.2
            //CognitiveVR_Manager.PoseUpdateEvent -= CognitiveVR_Manager_PoseUpdateEvent; //1.1
#endif
#if CVR_OCULUS
            OVRManager.TrackingAcquired -= OVRManager_TrackingAcquired;
            OVRManager.TrackingLost -= OVRManager_TrackingLost;
#endif
        }
    }
}