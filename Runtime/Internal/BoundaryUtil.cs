using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

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

            // OCULUS SPECIFIC HACK 
            // Going far beyond boundary sometimes causes a pause
            // which causes GetBoundaryPoints() to return empty array and hence fires "fake recenter" events
            // Better to have false negative than a false positive
            if ((previousBoundary.Length > 0) && (currentBoundary.Length == 0)) { return false; }


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
                return Unity.XR.PXR.PXR_Boundary.GetGeometry(Unity.XR.PXR.BoundaryType.PlayArea);
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
