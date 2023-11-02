using OVR.OpenVR;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Adds room size from SteamVR chaperone to device info
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Room Size")]
    public class RoomSize : AnalyticsComponentBase
    {

        public GameObject post;
        List <Vector3> boundaryPoints = new List<Vector3>();
        

        //counts up the deltatime to determine when the interval ends
        private float currentTime;
        Vector3 lastRoomSize = new Vector3();
#if C3D_OCULUS
        private readonly float BoundaryTrackingInterval = 1;
        Vector3[] boundaryPointsArray;
        Transform trackingSpace;
        bool exited = false;
#endif

        private List<Vector3> GetBoundaryPoints()
        {

#if C3D_OCULUS
        if (OVRManager.boundary == null)
        {
            return null;
        }
        boundaryPoints = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea).ToList<Vector3>();
        return boundaryPoints;
#elif C3D_STEAMVR2
        // Valve.VR/OpenVR Array; we will convert it to list for ease of use. Array of size 1 because we are representing 1 rectangle
        Valve.VR.HmdQuad_t[] steamVRBoundaryPoints = new Valve.VR.HmdQuad_t[1]; 
        
        Valve.VR.CVRChaperoneSetup setup = Valve.VR.OpenVR.ChaperoneSetup;
        setup.GetWorkingCollisionBoundsInfo(out steamVRBoundaryPoints);
        boundaryPoints = GetValveArrayAsList(steamVRBoundaryPoints);
        return boundaryPoints;
#else
        // Using Unity's XRInputSubsystem as fallback
        List <XRInputSubsystem> subsystems = new List <XRInputSubsystem>();
        SubsystemManager.GetInstances<XRInputSubsystem>(subsystems);

        // Handling case of multiple subsystems to find the first one that "works"
        foreach (XRInputSubsystem subsystem in subsystems)
        {
            if (!subsystem.running)
            {
                continue;
            }
            if (subsystem.TryGetBoundaryPoints(boundaryPoints))
            {
                return boundaryPoints;
            }
        }
        // Unable to find boundary points - should we send an event?
#endif
            return boundaryPoints;
        }

#if C3D_STEAMVR2
        /// <summary>
        /// Converts Valve's HmdQuad_t array to a List of Vector3. 
        /// Used for the very specific use-case of boundary points.
        /// </summary>
        /// <param name="steamArray"> An array of HmdQuad_t structs</param>
        /// <returns> A list of 4 Vector3 elements </returns>
        private List<Vector3> GetValveArrayAsList(Valve.VR.HmdQuad_t[] steamArray)
        {
            List<Vector3> steamList = new List<Vector3>();
            Valve.VR.HmdQuad_t currentQuad = steamArray[0];
            steamList.Add(SteamQuadtToVector(currentQuad.vCorners0));
            steamList.Add(SteamQuadtToVector(currentQuad.vCorners1));
            steamList.Add(SteamQuadtToVector(currentQuad.vCorners2));
            steamList.Add(SteamQuadtToVector(currentQuad.vCorners3));
            return steamList;
        }

        /// <summary>
        /// Converts a Valve.VR HmdVector3_t struct to a Unity Vector3
        /// </summary>
        /// <param name="point">A struct of type Valve.VR.HmdVector3_t</param>
        /// <returns>A Vector3 representation of the Valve.VR point</returns>
        private Vector3 SteamQuadtToVector(Valve.VR.HmdVector3_t point)
        {
            Vector3 myPoint = new Vector3(point.v0, point.v1, point.v2);
            return myPoint;
        }
#endif


        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
#if C3D_OCULUS
            if (OVRManager.boundary == null)
            {
                return;
            }
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