using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using System.Linq;

namespace Cognitive3D.Components
{
    public class BoundaryUtil : MonoBehaviour
    {
        /// <summary>
        /// Compares two lists of points to determine if the boundary changed
        /// </summary>
        /// <param name="currentBoundary">The newly retrieved set of boundary points</param>
        /// <param name="previousBoundary">The cached set of boundary points</param>
        /// <returns>True if boundary changed, false otherwise</returns>
        internal static bool HasBoundaryChanged(Vector3[] previousBoundary, Vector3[] currentBoundary)
        {
            if ((previousBoundary == null && currentBoundary != null) || (previousBoundary != null && currentBoundary == null)) { return true; }
            if (previousBoundary == null && currentBoundary == null) { return false; }

            // OCULUS SPECIFIC SPECIAL CASE HANDLING 
            // Going far beyond boundary sometimes causes a pause
            // which causes GetBoundaryPoints() to return empty array and hence fires "fake recenter" events
            // Better to have false negative than a false positive
            if ((previousBoundary.Length > 0) && (currentBoundary.Length == 0)) { return false; }


            if (previousBoundary.Length != currentBoundary.Length) { return true; }

            int minLength = Mathf.Min(previousBoundary.Length, currentBoundary.Length);
            for (int i = 0; i < minLength; i++)
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
        /// Retrieves the coordinates of the corners of a quadrilateral representing the user defined boundary
        /// </summary>
        /// <returns>A List of Vector3 representing the corners of the user defined boundary</returns>
        internal static Vector3[] GetCurrentBoundaryPoints()
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
                var boundaryPoints = GetConvexHull(Unity.XR.PXR.PXR_Boundary.GetGeometry(Unity.XR.PXR.BoundaryType.OuterBoundary));
                boundaryPoints = GetLargestInscribedRectangle(boundaryPoints);
                return boundaryPoints;
            }
            else
            {
                return null;
            }
#elif C3D_VIVEWAVE
            float width = Wave.Native.Interop.WVR_GetArena().area.rectangle.width;
            float length = Wave.Native.Interop.WVR_GetArena().area.rectangle.length;

            // Half dimensions
            float halfWidth = width / 2f;
            float halfLength = length / 2f;

            // 4 corner points (clockwise or counter-clockwise)
            Vector3[] boundaryCorners = new Vector3[4];
            boundaryCorners[0] = new Vector3(-halfWidth, 0, -halfLength); // Bottom left
            boundaryCorners[1] = new Vector3(halfWidth, 0, -halfLength);  // Bottom right
            boundaryCorners[2] = new Vector3(halfWidth, 0, halfLength);   // Top right
            boundaryCorners[3] = new Vector3(-halfWidth, 0, halfLength);  // Top left
            return boundaryCorners;
#else
            // Using Unity's XRInputSubsystem as fallback
            List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
#if UNITY_6000_0_OR_NEWER
            SubsystemManager.GetSubsystems<XRInputSubsystem>(subsystems);
#else
            SubsystemManager.GetInstances<XRInputSubsystem>(subsystems);
#endif

            // Handling case of multiple subsystems to find the first one that retrieves boundary points
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
            Util.LogOnce("Unable to find boundary points using XRInputSubsystem", LogType.Warning);
            return null;
#endif
        }

#region SteamVR Specific Utils

#if C3D_STEAMVR2
        /// <summary>
        /// Converts Valve's HmdQuad_t array to a List of Vector3. 
        /// Used for the very specific use-case of boundary points.
        /// </summary>
        /// <param name="steamArray"> An array of HmdQuad_t structs</param>
        /// <returns> A list of 4 Vector3 elements </returns>
        private static Vector3[] ConvertSteamVRToUnityBounds(Valve.VR.HmdQuad_t[] steamArray)
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
        private static Vector3 SteamHMDVector3tToVector(Valve.VR.HmdVector3_t point)
        {
            Vector3 myPoint = new Vector3(point.v0, point.v1, point.v2);
            return myPoint;
        }
#endif

#endregion

        /// <summary>
        /// Determines if a point is within a polygon
        /// </summary>
        /// <param name="polygon">An array of Vector3 representing the corners of a polygon</param>
        /// <param name="testPoint">A Vector3 representing the point to test</param>
        /// <returns>True if point is in polygon, false otherwise</returns>
        internal static bool IsPointInPolygon4(Vector3[] polygon, Vector3 testPoint)
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
        /// Retrieves tracking space data
        /// </summary>
        /// <param name="customTransform"></param>
        /// <returns></returns>
        internal static bool TryGetTrackingSpaceTransform(out CustomTransform customTransform)
        {
            customTransform = null;
#if C3D_OCULUS
            OVRPose trackingSpacePose = OVRPlugin.GetTrackingTransformRelativePose(OVRPlugin.TrackingOrigin.FloorLevel).ToOVRPose();
            var trackingSpacePosition = GameplayReferences.HMD.transform.parent.TransformPoint(trackingSpacePose.position);
            var trackingSpaceRotation = GameplayReferences.HMD.transform.parent.rotation * trackingSpacePose.orientation;

            customTransform = new CustomTransform(trackingSpacePosition, trackingSpaceRotation);
#elif C3D_VIVEWAVE
            Wave.Essence.WaveRig waveRig = GameplayReferences.WaveRig;
            if (waveRig!= null)
            {
                customTransform = new CustomTransform(waveRig.transform.position, waveRig.transform.rotation);
            }   
#elif C3D_STEAMVR2
            var playerRig = GameplayReferences.PlayerRig;
            if (playerRig!= null)
            {
                customTransform = new CustomTransform(playerRig.transform.position, playerRig.transform.rotation);
            }
#endif
            if (customTransform == null)
            {
                customTransform = GetDefaultTrackingSpaceTransform();
            }

            return customTransform != null;
        }

        internal static CustomTransform GetDefaultTrackingSpaceTransform()
        {
#if COGNITIVE3D_INCLUDE_COREUTILITIES
            Unity.XR.CoreUtils.XROrigin xrRig = GameplayReferences.XRRig;
            if (xrRig!= null)
            {
                return new CustomTransform(xrRig.transform.position, xrRig.transform.rotation);
            }
#endif

#if COGNITIVE3D_INCLUDE_LEGACYINPUTHELPERS
            UnityEditor.XR.LegacyInputHelpers.CameraOffset cameraOffset = GameplayReferences.CameraOffset;
            if (cameraOffset != null)
            {
                return new CustomTransform(cameraOffset.transform.position, cameraOffset.transform.rotation);
            }
#endif
            if (GameplayReferences.RoomTrackingSpaceTransform != null)
            {
                return new CustomTransform(GameplayReferences.RoomTrackingSpaceTransform.position, GameplayReferences.RoomTrackingSpaceTransform.rotation);
            }

            return null;
        }  

        /// <summary>
        /// This function calculates the convex hull of a set of points using Andrew’s monotone chain algorithm. 
        /// It constructs the lower and upper hulls by iterating through the points. The function returns the convex hull 
        /// as an array of Vector3s with duplicates removed.
        /// </summary>
        private static Vector3[] GetConvexHull(Vector3[] points)
        {
            // Convex Hull using Andrew’s monotone chain algorithm
            if (points.Length < 3) return points;
            points = points.OrderBy(p => p.x).ThenBy(p => p.z).ToArray();

            Vector3[] hull = new Vector3[points.Length * 2]; // Allocate max possible size
            int hullSize = 0;

            // Lower hull
            foreach (var point in points)
            {
                while (hullSize >= 2 && Cross(hull[hullSize - 2], hull[hullSize - 1], point) <= 0)
                    hullSize--;
                hull[hullSize++] = point;
            }

            // Upper hull
            int lowerHullSize = hullSize;
            for (int i = points.Length - 2; i >= 0; i--)
            {
                var point = points[i];
                while (hullSize > lowerHullSize && Cross(hull[hullSize - 2], hull[hullSize - 1], point) <= 0)
                    hullSize--;
                hull[hullSize++] = point;
            }

            // Remove duplicate and return final array
            return hull.Take(hullSize - 1).ToArray();
        }

        /// <summary>
        /// Calculates the cross product of two vectors (a - o) and (b - o) in 2D space.
        /// This is used to determine the orientation of the points relative to each other (e.g., clockwise or counterclockwise).
        /// </summary>
        /// <param name="o">The origin point of the cross product.</param>
        /// <param name="a">The first point for the cross product calculation.</param>
        /// <param name="b">The second point for the cross product calculation.</param>
        private static float Cross(Vector3 o, Vector3 a, Vector3 b)
        {
            return (a.x - o.x) * (b.z - o.z) - (a.z - o.z) * (b.x - o.x);
        }

        /// <summary>
        /// Finds the largest rectangle that can be inscribed within the given convex hull.
        /// Iterates through all pairs of points in the convex hull to calculate the maximum area rectangle 
        /// and returns the four corners of that rectangle.
        /// </summary>
        /// <param name="convexHull">An array of points forming the convex hull.</param>
        private static Vector3[] GetLargestInscribedRectangle(Vector3[] convexHull)
        {
            float maxRectangleWidth = 0, maxRectangleHeight = 0;
            Vector3 rectangleCenter;
            Vector3 bottomLeftCorner = Vector3.zero, bottomRightCorner = Vector3.zero;
            Vector3 topLeftCorner = Vector3.zero, topRightCorner = Vector3.zero;

            foreach (var point1 in convexHull)
            {
                foreach (var point2 in convexHull)
                {
                    if (point1 == point2) continue;

                    float rectangleWidth = Mathf.Abs(point1.x - point2.x);
                    float rectangleHeight = Mathf.Abs(point1.z - point2.z);

                    // Check if the area of the rectangle formed by these two points is larger than the current max
                    if (rectangleWidth * rectangleHeight > maxRectangleWidth * maxRectangleHeight)
                    {
                        maxRectangleWidth = rectangleWidth;
                        maxRectangleHeight = rectangleHeight;
                        rectangleCenter = new Vector3((point1.x + point2.x) / 2, point1.y, (point1.z + point2.z) / 2);

                        // Calculate the corners of the rectangle
                        bottomLeftCorner = new Vector3(rectangleCenter.x - maxRectangleWidth / 2, rectangleCenter.y, rectangleCenter.z - maxRectangleHeight / 2);
                        bottomRightCorner = new Vector3(rectangleCenter.x + maxRectangleWidth / 2, rectangleCenter.y, rectangleCenter.z - maxRectangleHeight / 2);
                        topLeftCorner = new Vector3(rectangleCenter.x - maxRectangleWidth / 2, rectangleCenter.y, rectangleCenter.z + maxRectangleHeight / 2);
                        topRightCorner = new Vector3(rectangleCenter.x + maxRectangleWidth / 2, rectangleCenter.y, rectangleCenter.z + maxRectangleHeight / 2);
                    }
                }
            }

            return new[] { topLeftCorner, bottomLeftCorner, bottomRightCorner, topRightCorner };
        }

    }

    /// <summary>
    /// A custom class to mimic behaviour of UnityEngine.Transform
    /// We use this so we can construct a Transform from position and rotation
    /// </summary>
    internal class CustomTransform
    {
        /// <summary>
        /// Constructor for our custom transform
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        internal CustomTransform(Vector3 position, Quaternion rotation)
        {
            this.pos = position;
            this.rot = rotation;
        }

        internal Vector3 pos;
        internal Quaternion rot;
    }
}
