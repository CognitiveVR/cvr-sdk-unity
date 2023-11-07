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
        List <Vector3> boundaryPoints = new List<Vector3>();
        float BoundaryTrackingInterval = 1;
        Vector3 lastRoomSize = new Vector3();
        bool exited;

        //counts up the deltatime to determine when the interval ends
        float currentTime;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            boundaryPoints = GetBoundaryPoints();
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            CalculateAndRecordRoomsize(false);
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {

            currentTime += deltaTime;
            if (currentTime > BoundaryTrackingInterval)
            {
                if (HasBoundaryChanged())
                {
                    boundaryPoints = GetBoundaryPoints();
                    CalculateAndRecordRoomsize(true);
                }
                SendEventIfUserExitsBoundary();
                currentTime = 0;
            }
        }

        /// <summary>
        /// Determines if user changed their boundary
        /// </summary>
        /// <returns>True if boundary changed, false otherwise</returns>
        private bool HasBoundaryChanged()
        {
            List<Vector3> temporaryList = GetBoundaryPoints();
            for (int i = 0; i < boundaryPoints.Count; i++)
            {
                if (Vector3.SqrMagnitude(boundaryPoints[i] - temporaryList[i]) >= 0.1)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Sends a custom event when the user's HMD exits the boundary
        /// </summary>
        void SendEventIfUserExitsBoundary()
        {
            if (Cognitive3D_Manager.Instance.trackingSpace != null)
            {
                if (!IsPointInPolygon4(boundaryPoints.ToArray(), Cognitive3D_Manager.Instance.trackingSpace.transform.InverseTransformPoint(GameplayReferences.HMD.position)))
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
            }
            else
            {
                // Tracking space unavailable: Maybe send an event or property?
            }
        }

        /// <summary>
        /// Retrieves the coordinates of the corners of a quadrilateral representing the user defined boundary
        /// </summary>
        /// <returns>A List of Vector3 representing the corners of the user defined boundary</returns>
        private List<Vector3> GetBoundaryPoints()
        {

#if C3D_OCULUS
        if (OVRManager.boundary == null)
        {
            return null;
        }
        boundaryPoints = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea).ToList();
        return boundaryPoints;

#elif C3D_STEAMVR2
        // Valve.VR/OpenVR Array; we will convert it to list for ease of use
        Valve.VR.HmdQuad_t[] steamVRBoundaryPoints;
        Valve.VR.CVRChaperoneSetup setup = Valve.VR.OpenVR.ChaperoneSetup;
        setup.GetWorkingCollisionBoundsInfo(out steamVRBoundaryPoints);
        boundaryPoints = GetValveArrayAsList(steamVRBoundaryPoints);
        //  Debug.Log(steamVRBoundaryPoints.Length);
        // LogTheArray(steamVRBoundaryPoints);
        return boundaryPoints;

#else
            // Using Unity's XRInputSubsystem as fallback
            List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
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
            // Probably will return empty list; need to append with warning or somethings
            Debug.LogWarning("Unable to find boundary points using XRInputSubsystem");
            return boundaryPoints;
#endif
        }

        /// <summary>
        /// Determines if a point is within a polygon
        /// </summary>
        /// <param name="polygon">An array of Vector3 representing the corners of a polygon</param>
        /// <param name="testPoint">A Vector3 representing the point to test</param>
        /// <returns>True if point is in polygon, false otherwise</returns>
        private static bool IsPointInPolygon4(Vector3[] polygon, Vector3 testPoint)
        {
            bool result = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                // Only using x and z coordinates because Unity is "y-up" and boundary is infinitely high
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

        /// <summary>
        /// Called at session beginning and when boundary changes.
        /// Sets the new roomsize as a session property and if the bool param is true, records the boundary change as a custom event
        /// </summary>
        /// <param name="recordRoomSizeChangeAsEvent">Flag to enable recording a custom event</param>
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

#region SteamVR Specific Utils

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
                for (int i = 0; i < steamArray.Length; i++)
                {
                    Valve.VR.HmdQuad_t currentQuad = steamArray[i];
                    steamList.Add(SteamQuadtToVector(currentQuad.vCorners0));
                    steamList.Add(SteamQuadtToVector(currentQuad.vCorners1));
                    steamList.Add(SteamQuadtToVector(currentQuad.vCorners2));
                    steamList.Add(SteamQuadtToVector(currentQuad.vCorners3));
                }
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

#endregion

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

#region Inspector Utils
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
#endregion

    }
}