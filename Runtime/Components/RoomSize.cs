using System.Collections.Generic;
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
        Vector3[] previousBoundaryPoints = new Vector3[0];
        readonly float BoundaryTrackingInterval = 1;
        Vector3 lastRoomSize = new Vector3();
        bool isHMDOutsideBoundary;

        //counts up the deltatime to determine when the interval ends
        float currentTime;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            previousBoundaryPoints = GetCurrentBoundaryPoints();
            CalculateAndRecordRoomsize(false, false);
            Vector3 initialRoomsize = new Vector3();
            GetRoomSize(ref initialRoomsize);
            WriteRoomSizeAsSessionProperty(initialRoomsize);
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            currentTime += deltaTime;
            if (currentTime > BoundaryTrackingInterval)
            {
                currentTime = 0;
                var currentBoundaryPoints = GetCurrentBoundaryPoints();
                if (HasBoundaryChanged(previousBoundaryPoints, currentBoundaryPoints))
                {
                    previousBoundaryPoints = currentBoundaryPoints;
                    CalculateAndRecordRoomsize(true, true);
                }
                SendEventIfUserExitsBoundary();
            }
        }

        /// <summary>
        /// Compares two lists of points to determine if the boundary changed
        /// </summary>
        /// <param name="currentBoundary">The newly retrieved set of boundary points</param>
        /// <param name="previousBoundary">The cached set of boundary points</param>
        /// <returns>True if boundary changed, false otherwise</returns>
        private bool HasBoundaryChanged(Vector3[] previousBoundary, Vector3[] currentBoundary)
        {
            // this is for a very specific case where boundary exit sometimes causes an empty array from GetBoundaryPoints and hence "fake recenter" events
            // better to have false negative than a false positive
            if ((previousBoundary.Length > 0) && (currentBoundary.Length == 0)) {  return false; }

            if ((previousBoundary == null && currentBoundary != null) || (previousBoundary != null && currentBoundary == null)) { return true; }
            if (previousBoundary == null && currentBoundary == null) { return false; }
            if (previousBoundary.Length != currentBoundary.Length) { return true; }

            for (int i = 0; i < previousBoundary.Length; i++)
            {
                // Check whether x or z coordinate changed significantly
                // Ignore y because y is "up" and boundary is infinitely high
                // We only care about ground plane
                if (Mathf.Abs(previousBoundary[i].x - currentBoundary[i].x) >= 0.1f
                    || Mathf.Abs(previousBoundary[i].z - currentBoundary[i].z) >= 0.1f)
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
            Transform trackingSpace = null;
            if (Cognitive3D_Manager.Instance.TryGetTrackingSpace(out trackingSpace))
            {
                if ((previousBoundaryPoints.Length != 0) && (!IsPointInPolygon4(previousBoundaryPoints, trackingSpace.transform.InverseTransformPoint(GameplayReferences.HMD.position))))
                {
                    if (!isHMDOutsideBoundary)
                    {
                        new CustomEvent("c3d.user.exited.boundary").Send();
                        isHMDOutsideBoundary = true;
                    }
                }
                else
                {
                    isHMDOutsideBoundary = false;
                }
            }
            else
            {
                Debug.Log("Tracking Space not found");
            }
        }

        /// <summary>
        /// Retrieves the coordinates of the corners of a quadrilateral representing the user defined boundary
        /// </summary>
        /// <returns>A List of Vector3 representing the corners of the user defined boundary</returns>
        private Vector3[] GetCurrentBoundaryPoints()
        {
#if C3D_OCULUS
            if (OVRManager.boundary == null)
            {
                return null;
            }
            return OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);

#elif C3D_STEAMVR2
            // Valve.VR/OpenVR Array; we will convert it to list for ease of use
            Valve.VR.HmdQuad_t[] steamVRBoundaryPoints;
            Valve.VR.CVRChaperoneSetup setup = Valve.VR.OpenVR.ChaperoneSetup;
            if (setup == null)
            {
                return null;
            }
            setup.GetWorkingCollisionBoundsInfo(out steamVRBoundaryPoints);
            return ConvertSteamVRToUnityBounds(steamVRBoundaryPoints);
#elif C3D_PICOXR
            if (Unity.XR.PXR.PXR_Boundary.GetEnabled())
            {
                return Unity.XR.PXR.PXR_Boundary.GetGeometry(Unity.XR.PXR.BoundaryType.PlayArea);
            }
            else
            {
                return null;
            }
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
                List<Vector3> retrievedPoints = new List<Vector3>();
                if (subsystem.TryGetBoundaryPoints(retrievedPoints))
                {
                    return retrievedPoints.ToArray();
                }
            }
            // Unable to find boundary points - should we send an event?
            // Probably will return empty list; need to append with warning or somethings
            Debug.LogWarning("Unable to find boundary points using XRInputSubsystem");
            return null;
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
            if (polygon == null || polygon.Length < 3) { return false; }
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
        /// Writes roomsize as session property
        /// </summary>
        private void WriteRoomSizeAsSessionProperty(Vector3 roomsize)
        {
            Cognitive3D_Manager.SetSessionProperty("c3d.roomsizeMeters", roomsize.x * roomsize.z);
            Cognitive3D_Manager.SetSessionProperty("c3d.roomsizeDescriptionMeters", string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} x {1:0.0}", roomsize.x, roomsize.z));
        }

        //TODO pass in boundary points and record roomsize with that instead of using GameplayReferences method

        /// <summary>
        /// Called at session beginning and when boundary changes.
        /// Sets the new roomsize as a session property and if the bool param is true, records the boundary change as a custom event
        /// </summary>
        /// <param name="recordRoomSizeChangeAsEvent">Flag to enable recording a custom event</param>
        /// <param name="recordRecenterAsEvent">Flag to enable recording recenter</param>
        private void CalculateAndRecordRoomsize(bool recordRoomSizeChangeAsEvent, bool recordRecenterAsEvent)
        {
            Vector3 roomsize = new Vector3();
            if (GetRoomSize(ref roomsize))
            {
#if XRPF
                if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsSpatialDataAllowed)
#endif
                {
                    float currentArea = roomsize.x * roomsize.z;
                    float lastArea = lastRoomSize.x * lastRoomSize.z;

                    // We have determined that a recenter causes change in boundary points without chaning the roomsize
                    if (Mathf.Approximately(currentArea, lastArea))
                    {
                        if (recordRecenterAsEvent)
                        {
                            new CustomEvent("c3d.User recentered")
                            .SetProperty("HMD position", GameplayReferences.HMD.position)
                            .Send();
                        }
                    }
                    else
                    {
                        WriteRoomSizeAsSessionProperty(roomsize);
                        SensorRecorder.RecordDataPoint("RoomSize", roomsize.x * roomsize.z);
                        if (recordRoomSizeChangeAsEvent)
                        {
                            // Chain SetProperty() instead of one SetProperties() to avoid creating dictionary and garbage
                            new CustomEvent("c3d.User changed boundary")
                            .SetProperty("Previous Room Size", lastRoomSize.x * lastRoomSize.z)
                            .SetProperty("New Room Size", roomsize.x * roomsize.z)
                            .Send();
                        }
                        lastRoomSize = roomsize;
                    }
                }
            }
            else
            {
                Cognitive3D_Manager.SetSessionProperty("c3d.roomsizeDescriptionMeters", "Invalid");
            }
        }

        /// <summary>
        /// Calculates and writes the value of the roomsize as a Vector3
        /// </summary>
        /// <param name="roomSize"> The variable the roomsize value will be written to</param>
        /// <returns> True if roomsize available, false otherwise</returns>
        private bool GetRoomSize(ref Vector3 roomSize)
        {
#if C3D_STEAMVR2
            float roomX = 0;
            float roomY = 0;
            if (Valve.VR.OpenVR.Chaperone == null || !Valve.VR.OpenVR.Chaperone.GetPlayAreaSize(ref roomX, ref roomY))
            {
                roomSize = new Vector3(roomX, 0, roomY);
                return true;
            }
            else
            {
                return false;
            }
#elif C3D_OCULUS
            if (OVRManager.boundary == null) { return false; }
            if (OVRManager.boundary.GetConfigured())
            {
                roomSize = OVRManager.boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea);
                return true;
            }
            return false;
#elif C3D_PICOXR
            if (Unity.XR.PXR.PXR_Boundary.GetEnabled())
            {
                roomSize = Unity.XR.PXR.PXR_Boundary.GetDimensions(Unity.XR.PXR.BoundaryType.PlayArea);
                roomSize /= 1000;
                return true;
            }
            else
            {
                return false;
            }
#elif C3D_PICOVR
            if (Pvr_UnitySDKAPI.BoundarySystem.UPvr_BoundaryGetEnabled())
            {
                //api returns mm
                roomSize = Pvr_UnitySDKAPI.BoundarySystem.UPvr_BoundaryGetDimensions(Pvr_UnitySDKAPI.BoundarySystem.BoundaryType.PlayArea);
                roomSize /= 1000;
                return true;
            }
            else
            {
                return false;
            }
#else
            UnityEngine.XR.InputDevice inputDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (inputDevice.isValid)
            {
                UnityEngine.XR.InputDevice hMDDevice = inputDevice;
                UnityEngine.XR.XRInputSubsystem XRIS = hMDDevice.subsystem;
                if (XRIS == null)
                {
                    return false;
                }
                List<Vector3> boundaryPoints = new List<Vector3>();
                if (XRIS.TryGetBoundaryPoints(boundaryPoints))
                {
                    roomSize = GetArea(boundaryPoints.ToArray());
                    return true;
                }
            }
            return false;
#endif
        }

        //really simple function to a rect from a collection of points
        //IMPROVEMENT support non-rectangular boundaries
        //IMPROVEMENT support rotated rectangular boundaries
        static Vector3 GetArea(Vector3[] points)
        {
            float minX = 0;
            float maxX = 0;
            float minZ = 0;
            float maxZ = 0;
            foreach (var v in points)
            {
                if (v.x < minX)
                    minX = v.x;
                if (v.x > maxX)
                    maxX = v.x;
                if (v.z < minZ)
                    minZ = v.z;
                if (v.z > maxZ)
                    maxZ = v.z;
            }
            return new Vector3(maxX - minX, 0, maxZ - minZ);
        }

        #region SteamVR Specific Utils

#if C3D_STEAMVR2
        /// <summary>
        /// Converts Valve's HmdQuad_t array to a List of Vector3. 
        /// Used for the very specific use-case of boundary points.
        /// </summary>
        /// <param name="steamArray"> An array of HmdQuad_t structs</param>
        /// <returns> A list of 4 Vector3 elements </returns>
        private Vector3[] ConvertSteamVRToUnityBounds(Valve.VR.HmdQuad_t[] steamArray)
        {
            Vector3[] returnArray = new Vector3[steamArray.Length * 4];
            for (int i = 0; i < steamArray.Length; i+=4)
            {
                returnArray[i] = SteamHMDVector3tToVector(steamArray[i].vCorners0);
                returnArray[i+1] = SteamHMDVector3tToVector(steamArray[i].vCorners1);
                returnArray[i+2] = SteamHMDVector3tToVector(steamArray[i].vCorners2);
                returnArray[i+3] = SteamHMDVector3tToVector(steamArray[i].vCorners3);
            }
            return returnArray;

            //List<Vector3> steamList = new List<Vector3>();
            //for (int i = 0; i < steamArray.Length; i++)
            //{
            //    Valve.VR.HmdQuad_t currentQuad = steamArray[i];
            //    steamList.Add(SteamHMDVector3tToVector(currentQuad.vCorners0));
            //    steamList.Add(SteamHMDVector3tToVector(currentQuad.vCorners1));
            //    steamList.Add(SteamHMDVector3tToVector(currentQuad.vCorners2));
            //    steamList.Add(SteamHMDVector3tToVector(currentQuad.vCorners3));
            //}
            //return steamList;
        }
            
        /// <summary>
        /// Converts a Valve.VR HmdVector3_t struct to a Unity Vector3
        /// </summary>
        /// <param name="point">A struct of type Valve.VR.HmdVector3_t</param>
        /// <returns>A Vector3 representation of the Valve.VR point</returns>
        private Vector3 SteamHMDVector3tToVector(Valve.VR.HmdVector3_t point)
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