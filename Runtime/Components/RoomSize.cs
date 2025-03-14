using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

#if C3D_VIVEWAVE
    using Wave;
    using Wave.Native;
    using Wave.Essence;
    using Wave.Essence.Events;
#endif

/// <summary>
/// Adds room size from VR boundary to the session properties
/// Records room size as a sensor
/// Sends event on boundary exit, redraw, and recenter
/// </summary>
namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Room Size")]
    public class RoomSize : AnalyticsComponentBase
    {
        /// <summary>
        /// The previous list of coordinates (local to tracking space) describing the boundary <br/>
        /// Used for comparison to determine if the boundary changed
        /// </summary>
        Vector3[] previousBoundaryPoints = new Vector3[0];

        /// <summary>
        /// The last recorded roomsize; used for comparison and roomsize change
        /// </summary>
        Vector3 previousRoomSize = new Vector3();

        /// <summary>
        /// The current roomsize 
        /// </summary>
        Vector3 roomSize = new Vector3();
        
        /// <summary>
        /// Set to true if the player is outside the guardian
        /// </summary>
        bool isHMDOutsideBoundary;

        readonly float BoundaryTrackingInterval = 1;

        //counts up the deltatime to determine when the interval ends
        float currentTime;

#if C3D_VIVEWAVE
        bool didViveArenaChange;
#endif

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            previousBoundaryPoints = BoundaryUtil.GetCurrentBoundaryPoints();

            CalculateAndRecordRoomsize(false, false);
            GetRoomSize(ref previousRoomSize);
            WriteRoomSizeAsSessionProperty(previousRoomSize);

#if C3D_VIVEWAVE
            SystemEvent.Listen(WVR_EventType.WVR_EventType_ArenaChanged, ArenaChanged);
#endif
        }

        /// <summary>
        /// Called 1 time per second; 1Hz
        /// </summary>
        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()  
            if (isActiveAndEnabled)
            {
                currentTime += deltaTime;
                if (currentTime > BoundaryTrackingInterval)
                {
                    currentTime = 0;
#if C3D_VIVEWAVE
                    // reset these variables every BoundaryTrackingInterval
                    didViveArenaChange = false;

                    if (Interop.WVR_IsOverArenaRange())
                    {
                        SendExitEvent();
                    }
                    else
                    {
                        isHMDOutsideBoundary = false;
                    }
#else
                    var currentBoundaryPoints = BoundaryUtil.GetCurrentBoundaryPoints();

                    if (currentBoundaryPoints != null)
                    {
                        if (BoundaryUtil.HasBoundaryChanged(previousBoundaryPoints, currentBoundaryPoints))
                        {
                            
                            previousBoundaryPoints = currentBoundaryPoints;
                            CalculateAndRecordRoomsize(true, true);
                        }
                    }
                    SendEventIfUserExitsBoundary();
#endif
                }
            }
            else
            {
                Debug.LogWarning("Roomsize component is disabled. Please enable in inspector.");
            }
        }

        /// <summary>
        /// Sends a custom event when the user's HMD exits the boundary
        /// </summary>
        void SendEventIfUserExitsBoundary()
        {
            Transform trackingSpace = Cognitive3D_Manager.Instance.trackingSpace;
            if (trackingSpace)
            {
                if (previousBoundaryPoints != null && previousBoundaryPoints.Length != 0) // we want to avoid "fake exit" events if boundary points is empty array; this happens sometimes when you pause
                {
                    if (!BoundaryUtil.IsPointInPolygon4(previousBoundaryPoints, trackingSpace.transform.InverseTransformPoint(GameplayReferences.HMD.position)))
                    {
                        SendExitEvent();
                    }
                    else
                    {
                        isHMDOutsideBoundary = false;
                    }
                }
            }
            else
            {
                Debug.Log("Tracking Space not found");
            }
        }

        /// <summary>
        /// Writes roomsize as session property
        /// </summary>
        /// <param name="roomsize">A Vector3 representing the roomsize to write</param> 
        private void WriteRoomSizeAsSessionProperty(Vector3 roomsize)
        {
            Cognitive3D_Manager.SetSessionProperty("c3d.roomsizeMeters", roomsize.x * roomsize.z);
            Cognitive3D_Manager.SetSessionProperty("c3d.roomsizeDescriptionMeters", string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} x {1:0.0}", roomsize.x, roomsize.z));
        }

        /// <summary>
        /// Sends a custom event indicating a recenter
        /// </summary>
        private void SendRecenterEvent()
        {
            new CustomEvent("c3d.User recentered")
                .SetProperty("HMD position", GameplayReferences.HMD.position)
                .Send();
        }

        /// <summary>
        /// Sends a custom event indicating change in boundary
        /// </summary>
        /// <param name="roomSizeRef">A Vector3 representing new roomsize</param>
        private void SendBoundaryChangeEvent(Vector3 roomSizeRef)
        {
            // Chain SetProperty() instead of one SetProperties() to avoid creating dictionary and garbage
            new CustomEvent("c3d.User changed boundary")
                .SetProperty("Previous Room Size", previousRoomSize.x * previousRoomSize.z)
                .SetProperty("New Room Size", roomSizeRef.x * roomSizeRef.z)
                .Send();
        }

        /// <summary>
        /// Sends a custom event indicating user stepping out of boundary
        /// </summary>
        void SendExitEvent()
        {
            if (!isHMDOutsideBoundary)
            {
                new CustomEvent("c3d.user.exited.boundary").Send();
                isHMDOutsideBoundary = true;
            }
        }

        /// <summary>
        /// Called at session beginning and when boundary changes.
        /// Sets the new roomsize as a session property and if the bool param is true, records the boundary change as a custom event
        /// </summary>
        /// <param name="recordRoomSizeChangeAsEvent">Flag to enable recording a custom event</param>
        /// <param name="recordRecenterAsEvent">Flag to enable recording recenter</param>
        private void CalculateAndRecordRoomsize(bool recordRoomSizeChangeAsEvent, bool recordRecenterAsEvent)
        {
            if (GetRoomSize(ref roomSize))
            {
#if XRPF
                if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsSpatialDataAllowed)
#endif
                {
                    float currentArea = roomSize.x * roomSize.z;
                    float lastArea = previousRoomSize.x * previousRoomSize.z;

                    // We have determined that a recenter causes change in boundary points without changing the roomsize
                    if (Mathf.Approximately(currentArea, lastArea))
                    {
                        if (recordRecenterAsEvent)
                        {
                            SendRecenterEvent();
                        }
                    }
                    else
                    {
                        WriteRoomSizeAsSessionProperty(roomSize);
                        SensorRecorder.RecordDataPoint("RoomSize", roomSize.x * roomSize.z);
                        if (recordRoomSizeChangeAsEvent)
                        {
                            SendBoundaryChangeEvent(roomSize);
                        }
                        previousRoomSize = roomSize;
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
            if (Valve.VR.OpenVR.Chaperone != null && Valve.VR.OpenVR.Chaperone.GetPlayAreaSize(ref roomX, ref roomY))
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
#elif C3D_VIVEWAVE
            // We consider width to go from left-to-right hence width is x-component
            roomSize = new Vector3(Interop.WVR_GetArena().area.rectangle.width, 0f, Interop.WVR_GetArena().area.rectangle.length);
            return true;
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
            Vector3[] returnArray = new Vector3[steamArray.Length];
            for (int i = 0; i < steamArray.Length; i++)
            {
                returnArray[i] = SteamHMDVector3tToVector(steamArray[i].vCorners0);
            }
            return returnArray;
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

#if C3D_VIVEWAVE
        Vector3 lastRoomSize = new Vector3();
        
        /// <summary>
        /// Vive Wave Specific: The function to execute when user changes their boundary
        /// </summary>
        /// <param name="arenaChangeEvent">The event that triggered this</param>
        void ArenaChanged(WVR_Event_t arenaChangeEvent)
        {
            if (!didViveArenaChange)
            {
                Vector3 roomsizeVive = new Vector3();
                GetRoomSize(ref roomsizeVive);
                if (Mathf.Approximately(roomsizeVive.x * roomsizeVive.z, lastRoomSize.x * lastRoomSize.z))
                {
                    // If arena changes and size remains same, it is recenter
                    // For some reason, the recenter event isn't triggering 
                    SendRecenterEvent();
                }
                else
                {
                    lastRoomSize = roomsizeVive;
                    SendBoundaryChangeEvent(roomsizeVive);
                }
            }
            didViveArenaChange = true;
        }
#endif

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;

#if C3D_VIVEWAVE
            SystemEvent.Remove(WVR_EventType.WVR_EventType_ArenaChanged, ArenaChanged);
#endif
        }

        private void OnDestroy()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;

#if C3D_VIVEWAVE
            SystemEvent.Remove(WVR_EventType.WVR_EventType_ArenaChanged, ArenaChanged);
#endif
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
