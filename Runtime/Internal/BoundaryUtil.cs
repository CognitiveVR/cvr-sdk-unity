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
#else
            // Using Unity's XRInputSubsystem as fallback
            List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
#if UNITY_6000_0_OR_NEWER
            SubsystemManager.GetSubsystems<XRInputSubsystem>(subsystems);
#else
            SubsystemManager.GetInstances<XRInputSubsystem>(subsystems);
#endif

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
            Util.LogOnce("Unable to find boundary points using XRInputSubsystem", LogType.Warning);
            return null;
#endif
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
            Vector3 rectangleCenter = Vector3.zero;
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

            return new Vector3[] { topLeftCorner, bottomLeftCorner, bottomRightCorner, topRightCorner };
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
