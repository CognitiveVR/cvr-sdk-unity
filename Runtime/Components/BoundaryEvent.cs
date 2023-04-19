using UnityEngine;

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
        private readonly float BoundaryTrackingInterval = 1;
        //counts up the deltatime to determine when the interval ends
        private float currentTime;
#if C3D_OCULUS
        Vector3[] boundaryPointsArray;
        Transform trackingSpace;
        bool exited = false;
#endif
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
#if C3D_OCULUS
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            boundaryPointsArray = new Vector3[4];
            trackingSpace = TryGetTrackingSpace();
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

#if C3D_OCULUS
        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            currentTime += deltaTime;
            if (currentTime > BoundaryTrackingInterval)
            {
                CheckBoundary();
            }
        }

        void CheckBoundary()
        {
            if (OVRManager.boundary != null)
            {
                if (HasBoundaryChanged())
                {
                    boundaryPointsArray = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
                }
            }

            // Unity uses y-up coordinate system - the boundary "up" doesn't matters
            if (trackingSpace != null)
            {
                if (!IsPointInPolygon4(boundaryPointsArray, trackingSpace.InverseTransformPoint(GameplayReferences.HMD.position)))
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
            }
            else 
            {
                trackingSpace = TryGetTrackingSpace();
            }
        }

        private bool HasBoundaryChanged()
        {
            Vector3[] temporaryArray;
            temporaryArray = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
            for (int i = 0; i < boundaryPointsArray.Length; i++)
            {
                if (Vector3.SqrMagnitude(boundaryPointsArray[i] - temporaryArray[i]) >= 1)
                {
                    return true;
                }
            }
            return false;
        }

        private Transform TryGetTrackingSpace()
        {
            OVRCameraRig cameraRig = GameObject.FindObjectOfType<OVRCameraRig>();
            if (cameraRig != null)
            {
                return cameraRig.trackingSpace;
            }
            return null;
        }
#endif

        private static bool IsPointInPolygon4(Vector3[] polygon, Vector3 testPoint)
        {
            bool result = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (polygon[i].z < testPoint.z && polygon[j].z >= testPoint.z || polygon[j].z < testPoint.z && polygon[i].z >= testPoint.z)
                {
                    if (polygon[i].x + (testPoint.z - polygon[i].z) / (polygon[j].z - polygon[i].z) * (polygon[j].x - polygon[i].x) < testPoint.x)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

    private void Cognitive3D_Manager_OnPreSessionEnd()
        {
#if C3D_OCULUS

            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
#endif
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