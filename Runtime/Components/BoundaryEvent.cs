using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
#if C3D_STEAMVR || C3D_STEAMVR2
using Valve.VR;
#endif

/// <summary>
/// Send a Custom Event when SteamVR Chaperone is visible
/// </summary>

//TODO add picovr sdk Pvr_UnitySDKAPI.BoundarySystem.UPvr_BoundaryGetVisible();
//TODO investigate openxr support for boundary visibility events

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Boundary Event")]
    public class BoundaryEvent : AnalyticsComponentBase
    {

        [ClampSetting(1f, 5f)]
        [Tooltip("Number of seconds used to average to determine framerate. Lower means more smaller samples and more detail")]
        public float BoundaryTrackingInterval = 1;
        //counts up the deltatime to determine when the interval ends
        private float currentTime;
        //the number of frames in the interval
        private int intervalFrameCount;
#if C3D_OCULUS
        Vector3[] boundaryPointsArray;
        List<float> xCoordinates;
        List<float> yCoordinates;
        List<float> zCoordinates;
        float minX;
        float maxX;
        float minY;
        float maxY;
        float minZ;
        float maxZ;
        Transform trackingSpace;
#endif        
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsSpatialDataAllowed)
#endif            
            {
                Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
                Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            }
#if C3D_OCULUS
            Transform trackingSpace = GameObject.Find("TrackingSpace").transform;
#endif
            //Cognitive3D_Manager.PoseEvent += Cognitive3D_Manager_PoseEventHandler;

#if C3D_STEAMVR2
            Valve.VR.SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsHidden).AddListener(OnChaperoneChanged);
            Valve.VR.SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsShown).AddListener(OnChaperoneChanged);

            if (Valve.VR.OpenVR.Chaperone.AreBoundsVisible())
            {
                new CustomEvent("cvr.boundary").Send();
            }
#endif
        }

#if C3D_STEAMVR2
        private void OnChaperoneChanged(Valve.VR.VREvent_t arg0)
        {

            if (Valve.VR.OpenVR.Chaperone.AreBoundsVisible())
            {
                new CustomEvent("cvr.boundary").SetProperty("visible", true).Send();
            }
            else
            {
                new CustomEvent("cvr.boundary").SetProperty("visible", false).Send();
            }
        }
#endif

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
#if C3D_OCULUS
            intervalFrameCount++;
            currentTime += deltaTime;
            if (currentTime > BoundaryTrackingInterval)
            {
                CheckBoundary();
            }
#endif
        }

        void CheckBoundary()
        {
#if C3D_OCULUS
            boundaryPointsArray = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
            intervalFrameCount = 0;
            currentTime = 0;

            xCoordinates = new List<float>();
            yCoordinates = new List<float>();
            zCoordinates = new List<float>();

            foreach (Vector3 point in boundaryPointsArray)
            {
                Vector3 transformedPoint = trackingSpace.TransformPoint(point);
                xCoordinates.Add(transformedPoint.x);
                yCoordinates.Add(transformedPoint.y);
                zCoordinates.Add(transformedPoint.z);
            }

            xCoordinates.Sort();
            yCoordinates.Sort();
            zCoordinates.Sort();

            minX = xCoordinates[0];
            maxX = xCoordinates[xCoordinates.Count - 1];
            minY = yCoordinates[0];
            maxY = yCoordinates[yCoordinates.Count - 1];
            minZ = zCoordinates[0];
            maxZ = zCoordinates[zCoordinates.Count - 1];

            Vector3 hmdPosition = GameplayReferences.HMD.position;

            if (hmdPosition.x < minX || hmdPosition.x > maxX
                || hmdPosition.y < minY || hmdPosition.y > maxY
                || hmdPosition.z < minZ || hmdPosition.z > maxZ
                )
            {
                new CustomEvent("c3d.user.exited.boundary").Send();
            }
#endif
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

        void OnDestroy()
        {
            Cognitive3D_Manager_OnPreSessionEnd();
#if C3D_STEAMVR2
            Valve.VR.SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsHidden).RemoveListener(OnChaperoneChanged);
            Valve.VR.SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsShown).RemoveListener(OnChaperoneChanged);
#endif
        }

        public override string GetDescription()
        {
#if C3D_STEAMVR2 || C3D_OCULUS
            return "Sends an event when Boundary becomes visible and becomes hidden";
#else
            return "Current platform does not support this component";
#endif
        }

        public override bool GetWarning()
        {
#if C3D_STEAMVR2 || C3D_OCULUS
            return false;
#else
            return true;
#endif
        }
    }
}