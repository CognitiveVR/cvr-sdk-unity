using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// sends transactions when a tracked device (likely a controller, but could also be headset or lighthouse) loses visibility (isvalid) or is disconnected/loses power (disconnected)
/// </summary>

namespace CognitiveVR.Components
{
    [AddComponentMenu("Cognitive3D/Components/Occlusion Event")]
    public class OcclusionEvent : CognitiveVRAnalyticsComponent
    {

#if CVR_OCULUS

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
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

#if CVR_PICONEO2EYE

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            base.CognitiveVR_Init(initError);

            Pvr_ControllerManager.PvrControllerStateChangedEvent += Pvr_ControllerManager_PvrControllerStateChangedEvent;
            Pvr_UnitySDKSensor.Enter3DofModelEvent += TrackingLost;
            Pvr_UnitySDKSensor.Exit3DofModelEvent += TrackingAcquired;
        }

        private void TrackingAcquired()
        {
            new CustomEvent("cvr.tracking").SetProperty("device", "hmd").SetProperty("visible", true).Send();
        }

        private void TrackingLost()
        {
            new CustomEvent("cvr.tracking").SetProperty("device", "hmd").SetProperty("visible", false).Send();
        }

        //Neo controller，"int a,int b"，a(0:controller0,1：controller1)，b(0:Disconnect，1：Connect)  
        private void Pvr_ControllerManager_PvrControllerStateChangedEvent(string data)
        {
            //Debug.Log("PicoControllerManager State Change:   " + data);
            //OcclusionChanged();
        }

#endif

        //known bug - steamvr1.2 occlusion events will not be correctly reported if only 1 controller is enabled. need to test steamvr2
#if CVR_STEAMVR2 || CVR_STEAMVR
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
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


        GameplayReferences.ControllerInfo tempInfo;
        void OcclusionChanged()
        {
            if (GameplayReferences.GetControllerInfo(false,out tempInfo))
            {
                if (tempInfo.connected != leftWasConnected)
                {
                    //event
                    new CustomEvent("cvr.tracking").SetProperty("device", "left").SetProperty("connected", tempInfo.connected).Send();
                    leftWasConnected = tempInfo.connected;
                }
                if (tempInfo.visible != leftWasVisible)
                {
                    //event
                    new CustomEvent("cvr.tracking").SetProperty("device", "left").SetProperty("visible", tempInfo.visible).Send();
                    leftWasVisible = tempInfo.visible;
                }
            }
            if (GameplayReferences.GetControllerInfo(true, out tempInfo))
            {
                if (tempInfo.connected != rightWasConnected)
                {
                    //event
                    new CustomEvent("cvr.tracking").SetProperty("device", "right").SetProperty("connected", tempInfo.connected).Send();
                    rightWasConnected = tempInfo.connected;
                }
                if (tempInfo.visible != rightWasVisible)
                {
                    //event
                    new CustomEvent("cvr.tracking").SetProperty("device", "right").SetProperty("visible", tempInfo.visible).Send();
                    rightWasVisible = tempInfo.visible;
                }
            }
        }

        public override bool GetWarning()
        {
#if CVR_STEAMVR || CVR_STEAMVR2 || CVR_OCULUS || CVR_PICONEO2EYE
            return false;
#else
            return true;
#endif
        }

        public override string GetDescription()
        {
            return "Sends transactions when a tracked device (likely a controller, but could also be headset or lighthouse) loses visibility (visible) or is disconnected/loses power (connected)";
        }
    }
}