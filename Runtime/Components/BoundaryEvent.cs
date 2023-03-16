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
        private float BoundaryTrackingInterval = 1;
        //counts up the deltatime to determine when the interval ends
        private float currentTime;
#if C3D_OCULUS
        Vector3[] boundaryPointsArray;
        List<float> xCoordinates;
        List<float> zCoordinates;
        float minX;
        float maxX;
        float minZ;
        float maxZ;
        Transform trackingSpace;
        Vector3 hmdPosition;
        bool exited = false;
#endif
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
#if C3D_OCULUS
            trackingSpace = GameObject.FindObjectOfType<OVRCameraRig>().trackingSpace;
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
            boundaryPointsArray = new Vector3[4];
            if (OVRManager.boundary != null)
            {
                if (boundaryPointsArray != OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea))
                {
                    boundaryPointsArray = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
                    RecalculateBounds();
                }
            }

            // Unity uses y-up coordinate system - the boundary "up" doesn't matters
            if ((hmdPosition.x < minX) || (hmdPosition.x > maxX)
                || (hmdPosition.z < minZ) || (hmdPosition.z > maxZ))
            {
                if (!exited)
                {
                    new CustomEvent("c3d.user.exited.boundary").Send();
                    exited = true;
                }
            }
            else
            {
                exited = false;
            }
            currentTime = 0;
#endif
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

        private void RecalculateBounds()
        {
            xCoordinates = new List<float>();
            zCoordinates = new List<float>();

            foreach (Vector3 point in boundaryPointsArray)
            {
                Vector3 transformedPoint = trackingSpace.TransformPoint(point);

                xCoordinates.Add(transformedPoint.x);
                zCoordinates.Add(transformedPoint.z);
            }

            xCoordinates.Sort();
            zCoordinates.Sort();

            if (xCoordinates.Count > 0)
            {
                minX = xCoordinates[0];
                maxX = xCoordinates[xCoordinates.Count - 1];
            }
            if (zCoordinates.Count > 0)
            {
                minZ = zCoordinates[0];
                maxZ = zCoordinates[zCoordinates.Count - 1];
            }
            hmdPosition = GameplayReferences.HMD.position;
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