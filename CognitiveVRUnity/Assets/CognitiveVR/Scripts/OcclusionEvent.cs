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
                new CustomEvent("cvr.tracking").SetProperty("device","right controller").SetProperty("visible",false).Send();
            }
            if (OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch) && !string.IsNullOrEmpty(rRouchGUID))
            {
                new CustomEvent("cvr.tracking").SetProperty("device","right controller").SetProperty("visible", true).Send();
                rRouchGUID = string.Empty;
            }

            if (!OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch) && string.IsNullOrEmpty(lTouchGUID))
            {
                lTouchGUID = Util.GetUniqueId();
                new CustomEvent("cvr.tracking").SetProperty("device", "left controller").SetProperty("visible", false).Send();
            }
            if (OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch) && !string.IsNullOrEmpty(lTouchGUID))
            {
                new CustomEvent("cvr.tracking").SetProperty("device", "left controller").SetProperty("visible", true).Send();
                lTouchGUID = string.Empty;
            }
        }

        string hmdGUID;
        private void OVRManager_TrackingLost()
        {
            hmdGUID = Util.GetUniqueId();
            new CustomEvent("cvr.tracking").SetProperty("device", "hmd").SetProperty("visible", false).Send();
        }

        private void OVRManager_TrackingAcquired()
        {
            new CustomEvent("cvr.tracking").SetProperty("device", "hmd").SetProperty("visible", true).Send();
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
                    new CustomEvent("cvr.tracking").SetProperty("device", GetViveDeviceName(Devices[j].deviceID)).SetProperty("visible", true).Send();
                    Devices[j].ValidTransID = string.Empty;
                }
                if (!args[Devices[j].deviceID].bPoseIsValid && Devices[j].ValidTransID == string.Empty)
                {
                    Devices[j].ValidTransID = Util.GetUniqueId();
                    new CustomEvent("cvr.tracking").SetProperty("device", GetViveDeviceName(Devices[j].deviceID)).SetProperty("visible", false).Send();
                }

                if (args[Devices[j].deviceID].bDeviceIsConnected && Devices[j].ConnectedTransID != string.Empty)
                {
                    new CustomEvent("cvr.tracking").SetProperty("device", GetViveDeviceName(Devices[j].deviceID)).SetProperty("connected", true).Send();
                    Devices[j].ConnectedTransID = string.Empty;
                }
                if (!args[Devices[j].deviceID].bDeviceIsConnected && Devices[j].ConnectedTransID == string.Empty)
                {
                    Devices[j].ConnectedTransID = Util.GetUniqueId();
                    new CustomEvent("cvr.tracking").SetProperty("device", GetViveDeviceName(Devices[j].deviceID)).SetProperty("connected", false).Send();
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