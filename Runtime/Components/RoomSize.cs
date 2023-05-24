using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds room size from SteamVR chaperone to device info
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Room Size")]
    public class RoomSize : AnalyticsComponentBase
    {
        
        //counts up the deltatime to determine when the interval ends
        private float currentTime;
        Vector3 lastRoomSize = new Vector3();
#if C3D_OCULUS
        private readonly float BoundaryTrackingInterval = 1;
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
            boundaryPointsArray = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
#endif

#if C3D_STEAMVR2
            Valve.VR.SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsHidden).AddListener(OnChaperoneChanged);
            Valve.VR.SteamVR_Events.System(Valve.VR.EVREventType.VREvent_Compositor_ChaperoneBoundsShown).AddListener(OnChaperoneChanged);

            if (Valve.VR.OpenVR.Chaperone.AreBoundsVisible())
            {
                new CustomEvent("c3d.user.exited.boundary").Send();
            }
#endif
            CalculateAndRecordRoomsize(false);
        }

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
                    CalculateAndRecordRoomsize(true);
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

        // Online reference
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

        private void CalculateAndRecordRoomsize(bool recordRoomSizeChangeAsEvent)
        {
            Vector3 roomsize = new Vector3();
            if (GameplayReferences.GetRoomSize(ref roomsize))
            {
#if XRPF
                if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsSpatialDataAllowed)
#endif
                {
                    Cognitive3D_Manager.SetSessionProperty("c3d.roomsizeMeters", roomsize.x * roomsize.z);
                    Cognitive3D_Manager.SetSessionProperty("c3d.roomsizeDescriptionMeters", string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} x {1:0.0}", roomsize.x, roomsize.z));
                    SensorRecorder.RecordDataPoint("RoomSize", roomsize.x * roomsize.z);
                    if (recordRoomSizeChangeAsEvent)
                    {
                        new CustomEvent("c3d.User changed boundary").SetProperties(new Dictionary<string, object>
                        {
                            {  "Previous Room Size" , lastRoomSize.x * lastRoomSize.z },
                            {   "New Room Size" , roomsize.x * roomsize.z }
                        }).Send();
                    }
                    lastRoomSize = roomsize;
                }
            }
            else
            {
                Cognitive3D_Manager.SetSessionProperty("c3d.roomsizeDescriptionMeters", "Invalid");
            }
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

        public override bool GetWarning()
        {
            return !GameplayReferences.SDKSupportsRoomSize;
        }

        public override string GetDescription()
        {
            if (GameplayReferences.SDKSupportsRoomSize)
            {
                return "Calculates properties and handles events related to player boundary";
            }
            else
            {
                return "Current platform does not support this component";
            }
        }
    }
}