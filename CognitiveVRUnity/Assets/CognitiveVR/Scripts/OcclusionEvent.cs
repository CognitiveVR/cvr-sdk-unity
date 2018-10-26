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

#if CVR_OCULUS

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);

            OVRManager.TrackingAcquired += OVRManager_TrackingAcquired;
            OVRManager.TrackingLost += OVRManager_TrackingLost;
        }

        bool LeftControllerVisible = true;
        bool RightControllerVisible = true;

        void Update()
        {
            //TODO move this stuff into cognitivevr_manager and get states from there
            if (!OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch) && RightControllerVisible)
            {
                RightControllerVisible = false;
                new CustomEvent("cvr.tracking").SetProperty("device","right controller").SetProperty("visible",false).Send();
            }
            if (OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch) && !RightControllerVisible)
            {
                new CustomEvent("cvr.tracking").SetProperty("device","right controller").SetProperty("visible", true).Send();
                RightControllerVisible = true;
            }

            if (!OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch) && LeftControllerVisible)
            {
                LeftControllerVisible = false;
                new CustomEvent("cvr.tracking").SetProperty("device", "left controller").SetProperty("visible", false).Send();
            }
            if (OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch) && !LeftControllerVisible)
            {
                new CustomEvent("cvr.tracking").SetProperty("device", "left controller").SetProperty("visible", true).Send();
                LeftControllerVisible = true;
            }
        }

        private void OVRManager_TrackingLost()
        {
            new CustomEvent("cvr.tracking").SetProperty("device", "hmd").SetProperty("visible", false).Send();
        }

        private void OVRManager_TrackingAcquired()
        {
            new CustomEvent("cvr.tracking").SetProperty("device", "hmd").SetProperty("visible", true).Send();
        }

        void OnDestroy()
        {
            OVRManager.TrackingAcquired -= OVRManager_TrackingAcquired;
            OVRManager.TrackingLost -= OVRManager_TrackingLost;
        }
#endif

        //known bug - steamvr1.2 occlusion events will not be correctly reported if only 1 controller is enabled. need to test steamvr2
#if CVR_STEAMVR2 || CVR_STEAMVR
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);
            
            CognitiveVR_Manager.PoseUpdateEvent += CognitiveVR_Manager_PoseUpdateHandler; //1.2
            //CognitiveVR_Manager.PoseUpdateEvent += CognitiveVR_Manager_PoseUpdateEvent; //1.1
        }

        //steam 1.2
        private void CognitiveVR_Manager_PoseUpdateHandler(Valve.VR.TrackedDevicePose_t[] args)
        {
            OcclusionChanged();
        }
#endif
        bool leftWasVisible;
        bool leftWasConnected;

        bool rightWasVisible;
        bool rightWasConnected;

        void OcclusionChanged()
        {
            var left = CognitiveVR_Manager.GetControllerInfo(false);
            if (left != null && left.transform != null)
            {
                if (left.connected != leftWasConnected)
                {
                    //event
                    new CustomEvent("cvr.tracking").SetProperty("device", "left").SetProperty("connected", left.connected).Send();
                    leftWasConnected = left.connected;
                }
                if (left.visible != leftWasVisible)
                {
                    //event
                    new CustomEvent("cvr.tracking").SetProperty("device", "left").SetProperty("visible", left.visible).Send();
                    leftWasVisible = left.visible;
                }
            }

            var right = CognitiveVR_Manager.GetControllerInfo(true);
            if (right != null && right.transform != null)
            {
                if (right.connected != rightWasConnected)
                {
                    //event
                    new CustomEvent("cvr.tracking").SetProperty("device", "right").SetProperty("connected", right.connected).Send();
                    rightWasConnected = right.connected;
                }
                if (right.visible != rightWasVisible)
                {
                    //event
                    new CustomEvent("cvr.tracking").SetProperty("device", "right").SetProperty("visible", right.visible).Send();
                    rightWasVisible = right.visible;
                }
            }
        }

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
    }
}